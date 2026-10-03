using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
namespace ForgePC;

public sealed class Command(Func<object?,Task> action, Action<Exception> error, Func<bool>? allowed=null) : ICommand
{
 private bool running;
 public event EventHandler? CanExecuteChanged;
 public bool CanExecute(object? parameter)=>!running && (allowed?.Invoke()??true);
 public async void Execute(object? parameter){if(!CanExecute(parameter))return;running=true;Refresh();try{await action(parameter);}catch(Exception e){error(e);}finally{running=false;Refresh();}}
 public void Refresh()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);
}
public record ReviewRow(string Id,string Title,string Before,string After,string Contract);
public interface IUserInteraction
{
 Task<string?> ReadProfile(); Task WriteText(string text,string name);
 bool Confirm(string text,string title); void ApplyTheme(string theme);
}
public sealed class MainViewModel : Observable, IDisposable
{
 private readonly WindowsSettings settings=new(); private readonly WindowsSystemProbe probe=new();
 private readonly WindowsCapabilityService capabilities; private readonly TuningEngine engine;
 private readonly QueueStore queueStore; private readonly LocalLogger logger; private readonly IUserInteraction ui;
 private readonly CancellationTokenSource lifetime=new(); private CancellationTokenSource? batch;
 private readonly Dictionary<string,string> desired=[]; private List<Change> preview=[];
 private SystemReading? sample; private HardwareInventory? inventory; private bool sampling;
 private readonly bool recoveryOnly; private readonly List<double> cpuTrend=[],memoryTrend=[];
 private ProcessEntry? watchedGame; private string? gameJournal; private bool awake;
 [DllImport("kernel32.dll",SetLastError=true)] private static extern uint SetThreadExecutionState(uint flags);
 public ObservableCollection<OperationItem> Operations {get;}=new(Catalog.Operations.Select(d=>new OperationItem(d)));
 public ObservableCollection<PowerPlan> Plans {get;}=[];
 public ObservableCollection<ReviewRow> Review {get;}=[];
 public ObservableCollection<Journal> History {get;}=[];
 public ObservableCollection<Recommendation> Recommendations {get;}=[];
 public ObservableCollection<Guidance> Guides {get;}=[];
 public ObservableCollection<ProcessEntry> Processes {get;}=[];
 public ObservableCollection<InstalledApp> Apps {get;}=[];
 public ObservableCollection<string> Startup {get;}=[];
 public string[] Pages {get;}=["Overview","Recommendations","Optimizations","Profiles","Diagnostics","Recovery & History","Settings"];
 public string[] Profiles {get;}=["Gaming","Coding","Everyday","Quiet","Battery Saver"];
 public string[] Categories {get;}=["All","System","Gaming","Privacy","Debloat","Network","Maintenance"];
 public string[] Themes {get;}=["Dark","Light","Windows","High contrast"];
 public string[] Risks {get;}=["All risks","Low","Moderate"];
 public string[] ApplicabilityOptions {get;}=["All compatibility","Eligible","Blocked"];
 public string[] StateOptions {get;}=["All values","On","Off"];
 public string[] RestartOptions {get;}=["All restart states","No restart","Restart required"];
 public ObservableCollection<OperationItem> FilteredOperations {get;}=[];
 private string page="Overview",status="Loading local diagnostics…",search="",category="All",risk="All risks",applicability="All compatibility",state="All values",restart="All restart states",theme="Dark",profile="Gaming";
 private string cpu="—",memory="—",disk="—",stamp="Waiting for a sample",hardware="Reading hardware…",network="Not loaded",endpoint="",networkResult="Choose an endpoint before testing.",comparison="No comparison recorded.",workload="",restoreStatus="Not inspected",updateStatus="Manual checks only. Executable installation is disabled until publisher signing is configured.";
 private bool busy,reviewed,previewChannel;
 public string Page {get=>page;set{if(Pages.Contains(value))Set(ref page,value);}}
 public string Status {get=>status;set=>Set(ref status,value);}
 public string Search {get=>search;set{Set(ref search,value);Filter();}}
 public string Category {get=>category;set{if(Categories.Contains(value)){Set(ref category,value);Filter();}}}
 public string Risk {get=>risk;set{if(Risks.Contains(value)){Set(ref risk,value);Filter();}}}
 public string Applicability {get=>applicability;set{if(ApplicabilityOptions.Contains(value)){Set(ref applicability,value);Filter();}}}
 public string CurrentState {get=>state;set{if(StateOptions.Contains(value)){Set(ref state,value);Filter();}}}
 public string Restart {get=>restart;set{if(RestartOptions.Contains(value)){Set(ref restart,value);Filter();}}}
 public string Theme {get=>theme;set{if(Themes.Contains(value)){Set(ref theme,value);ui.ApplyTheme(value);}}}
 public string Profile {get=>profile;set{if(!string.IsNullOrWhiteSpace(value))Set(ref profile,value);}}
 public string Cpu {get=>cpu;set=>Set(ref cpu,value);}
 public string Memory {get=>memory;set=>Set(ref memory,value);}
 public string Disk {get=>disk;set=>Set(ref disk,value);}
 public string Stamp {get=>stamp;set=>Set(ref stamp,value);}
 public string Hardware {get=>hardware;set=>Set(ref hardware,value);}
 public string Network {get=>network;set=>Set(ref network,value);}
 public string Endpoint {get=>endpoint;set=>Set(ref endpoint,value);}
 public string NetworkResult {get=>networkResult;set=>Set(ref networkResult,value);}
 public string Workload {get=>workload;set=>Set(ref workload,value);}
 public string Comparison {get=>comparison;set=>Set(ref comparison,value);}
 public string RestoreStatus {get=>restoreStatus;set=>Set(ref restoreStatus,value);}
 public string UpdateStatus {get=>updateStatus;set=>Set(ref updateStatus,value);}
 public bool PreviewChannel {get=>previewChannel;set=>Set(ref previewChannel,value);}
 public bool Busy {get=>busy;private set{Set(ref busy,value);RefreshCommands();Raise(nameof(CanApply));}}
 public bool CanApply=>reviewed&&!Busy&&preview.Count>0;
 public bool KeepAwake {get=>awake;set{if(SetThreadExecutionState(value?0x80000001u:0x80000000u)==0){Status="Windows rejected the keep-awake request.";return;}Set(ref awake,value);Status=value?"Keep awake for this app session. Closing releases the request.":"Normal Windows sleep behavior resumed.";}}
 private bool queueExpanded; public bool QueueExpanded {get=>queueExpanded;set=>Set(ref queueExpanded,value);} public string QueueTitle=>$"Review queue · {desired.Count} staged · {(reviewed?"fresh preview":"preview required")}";
 public string RecoverySummary=>History.Any(j=>j.IsActive)?"An active session needs undo before another batch.":"Exact undo ready · one active session at a time";
 public string ActiveProfile=>History.FirstOrDefault(j=>j.IsActive)?.Profile??"No active profile";
 public string CpuPoints=>Points(cpuTrend); public string MemoryPoints=>Points(memoryTrend);
 private ProcessEntry? selectedGame; public ProcessEntry? SelectedGame {get=>selectedGame;set{Set(ref selectedGame,value);RefreshCommands();}}
 public Command Navigate {get;} public Command Stage {get;} public Command Preview {get;} public Command Apply {get;} public Command Cancel {get;} public Command Clear {get;} public Command StageProfile {get;} public Command Import {get;} public Command Export {get;} public Command Undo {get;} public Command RefreshDiagnostics {get;} public Command Open {get;} public Command CheckUpdate {get;} public Command CreateRestore {get;} public Command InspectRestore {get;} public Command ExportReport {get;} public Command MeasureNetwork {get;} public Command Compare {get;} public Command StartGame {get;}
 private readonly List<Command> commands=[];
 public MainViewModel(string dataRoot,IUserInteraction ui,bool recoveryOnly=false)
 {
  this.ui=ui;this.recoveryOnly=recoveryOnly; capabilities=new(settings);logger=new(Path.Combine(dataRoot,"logs"));
  engine=new(settings,new JournalStore(Path.Combine(dataRoot,"history")),capabilities,logger.Write);
  queueStore=new(Path.Combine(dataRoot,"review-queue.json"));
  Command Make(Func<object?,Task> action,Func<bool>? allowed=null){var c=new Command(action,Error,allowed);commands.Add(c);return c;}
  Navigate=Make(p=>{Page=p?.ToString()??"Overview";return Task.CompletedTask;});
  Stage=Make(p=>{if(p is OperationItem o){Catalog.ValidateTarget(o.Id,o.Target);desired[o.Id]=o.Target;PersistQueue();}return Task.CompletedTask;},()=>!Busy);
  Preview=Make(_=>PreviewQueue(),()=>!Busy&&desired.Count>0);
  Apply=Make(_=>ApplyQueue(false),()=>CanApply);Cancel=Make(_=>{batch?.Cancel();Status="Cancellation requested. The current operation will finish before compensation.";return Task.CompletedTask;},()=>Busy&&batch!=null);
  Clear=Make(_=>{desired.Clear();PersistQueue();return Task.CompletedTask;},()=>!Busy);
  StageProfile=Make(_=>LoadProfile(ProfileStore.BuiltIn(Profile)),()=>!Busy);
  Import=Make(async _=>{var content=await ui.ReadProfile();if(content!=null)await LoadProfile(ProfileStore.Parse(content));},()=>!Busy);
  Export=Make(_=>ui.WriteText(ProfileStore.Serialize(new(1,"Custom profile",desired.Select(p=>new ProfileParameter(p.Key,1,p.Value)).ToList())),"EZoptimizer-profile.json"),()=>!Busy);
  Undo=Make(async p=>{if(p is Journal j)await Restore(j);},()=>!Busy);
  RefreshDiagnostics=Make(_=>Diagnostics(),()=>!Busy);
  Open=Make(p=>{OpenAllowed(p?.ToString()??"");return Task.CompletedTask;});
  CheckUpdate=Make(async _=>{UpdateStatus="Checking official release metadata…";var result=await new UpdateService().CheckAsync(PreviewChannel,lifetime.Token);UpdateStatus=result.Message;},()=>!Busy);
  InspectRestore=Make(async _=>{var r=await new RestorePointService().InspectAsync(lifetime.Token);RestoreStatus=r.State+" · "+r.Message;},()=>!Busy);
  CreateRestore=Make(async _=>{if(!ui.Confirm("Create a supplementary Windows restore point? Windows will request administrator permission for this one operation. Frequency limits and policy are respected. Exact setting undo remains separate.","Review restore-point creation"))return;var r=await new RestorePointService().CreateReviewedAsync(lifetime.Token);RestoreStatus=r.State+" · "+r.Message;},()=>!Busy);
  ExportReport=Make(async _=>{var text=ReportService.Serialize(sample,inventory,History);if(ui.Confirm(text,"Review diagnostic export · local file only"))await ui.WriteText(text,"EZoptimizer-diagnostics.json");},()=>!Busy);
  MeasureNetwork=Make(async _=>{NetworkResult="Sending five bounded ICMP requests to your selected endpoint…";var r=await new NetworkDiagnostics().MeasureAsync(Endpoint,lifetime.Token);NetworkResult=$"{r.Received}/{r.Sent} replies · median {r.MedianMs?.ToString("0")??"unavailable"} ms\n{r.Explanation}";},()=>!Busy);
  Compare=Make(_=>CompareSamples(),()=>!Busy);
  StartGame=Make(_=>ApplyQueue(true),()=>CanApply&&SelectedGame!=null);
  if(recoveryOnly){Page="Recovery & History";Status="Recovery mode: dashboard probes are disabled.";Hardware="Not probed in recovery mode";}
  Filter();
 }
 public async Task InitializeAsync()
 {
  await ReloadHistory();if(recoveryOnly)return;
  try{foreach(var p in queueStore.Load())desired[p.Key]=p.Value;Invalidate();}catch(Exception e){Error(e);Status+=" The saved queue was not imported.";}
  await RefreshSettings();await TickAsync();
  try{inventory=await probe.InventoryAsync(lifetime.Token);Hardware=$"{inventory.OS}\n{inventory.Cpu} · {inventory.LogicalProcessors} logical processors · {inventory.Architecture}\nGraphics: {string.Join("; ",inventory.Graphics)}\nStorage: {string.Join("; ",inventory.Storage)}\n{inventory.PowerContext}";}catch(Exception e){Hardware="Inventory unavailable. Volatile readings remain separate.";Error(e);} if(Status=="Loading local diagnostics…")Status="Ready. Choose a profile or stage an individual setting. No changes have been applied.";
 }
 public async Task TickAsync()
 {
  if(recoveryOnly||sampling)return;sampling=true;
  try{sample=await probe.SampleAsync(lifetime.Token);Cpu=sample.Cpu is double c?$"{c:0}%":"Warming up";Memory=sample.MemoryPercent is double m?$"{m:0}%":"Unavailable";Disk=sample.DiskFree is long f?$"{f/1073741824d:0.0} GB":"Unavailable";Stamp=$"Sampled {sample.At.ToLocalTime():HH:mm:ss} · {sample.Status} · {sample.DurationMs} ms probe";
   Add(cpuTrend,sample.Cpu);Add(memoryTrend,sample.MemoryPercent);Raise(nameof(CpuPoints));Raise(nameof(MemoryPoints));
   Replace(Recommendations,RecommendationRules.Evaluate(sample.MemoryPercent,sample.DiskTotal>0?sample.DiskFree*100d/sample.DiskTotal:null,sample.OnBattery,sample.Cpu));
   if(gameJournal!=null && watchedGame!=null && !Busy){bool alive;try{using var p=Process.GetProcessById(watchedGame.Id);alive=p.StartTime.ToUniversalTime().Ticks==watchedGame.StartTicks&&!p.HasExited;}catch{alive=false;}if(!alive){var j=History.FirstOrDefault(j=>j.Id==gameJournal);gameJournal=null;if(j!=null)await Restore(j);Status="Selected game exited. "+Status;}}
  }catch(OperationCanceledException){}catch(Exception e){Stamp="Readings unavailable · last successful values may be stale";Error(e);}finally{sampling=false;}
 }
 private async Task RefreshSettings()
 {
  var snapshot=await Task.Run(()=>{var plans=settings.Plans();return(plans,items:Catalog.Operations.Select(d=>{try{var value=settings.Read(d.Id);return(d.Id,value,capabilities.Check(d.Id,value));}catch{return(d.Id,"Unavailable",new Capability(false,"Windows rejected the read or policy restricts this setting."));}}).ToArray());});
  Replace(Plans,snapshot.plans);
  foreach(var (id,value,cap) in snapshot.items){var o=Operations.Single(o=>o.Id==id);Replace(o.Choices,id=="power"?snapshot.plans.Select(p=>new ValueOption(p.Id,p.Name)):new[]{new ValueOption("On","On"),new ValueOption("Off","Off")});o.Current=Friendly(id,value);o.Target=desired.GetValueOrDefault(id,value);o.Eligible=cap.Eligible;o.Compatibility=cap.Reason;}
  Filter();
 }
 private Task LoadProfile(ProfileDocument document)
 {
  desired.Clear();var skipped=new List<string>();foreach(var p in document.Parameters){if(p.Id=="power"&&!Plans.Any(x=>x.Id==p.Value)){skipped.Add("Preferred power plan is not installed; current plan is retained.");continue;}desired[p.Id]=p.Value;var o=Operations.Single(o=>o.Id==p.Id);o.Target=p.Value;}
  Profile=document.Name;PersistQueue();Status=$"{document.Name} staged. Preview reevaluates this PC. "+string.Join(" ",skipped);return Task.CompletedTask;
 }
 private void PersistQueue(){Invalidate();queueStore.Save(desired);}
 private void Invalidate(){QueueExpanded=desired.Count>0;reviewed=false;preview.Clear();Replace(Review,desired.Select(p=>new ReviewRow(p.Key,Catalog.Get(p.Key).Title,"Preview required",Friendly(p.Key,p.Value),Catalog.Get(p.Key).Recovery)));Raise(nameof(CanApply));Raise(nameof(QueueTitle));RefreshCommands();}
 private async Task PreviewQueue()
 {
  Busy=true;Invalidate();try{var plan=await Task.Run(()=>new ExecutionPlanner(settings,capabilities).Preview(desired));preview=plan.Select(p=>new Change(p.Id,p.Title,p.Before,p.Target)).ToList();Replace(Review,plan.Select(p=>new ReviewRow(p.Id,p.Title,Friendly(p.Id,p.Before),Friendly(p.Id,p.Target),Catalog.Get(p.Id).Recovery)));reviewed=true;Status=preview.Count==0?"Already matches. No Windows changes are needed.":"Exact diff ready. Apply will revalidate every operation immediately before writing.";Raise(nameof(QueueTitle));}finally{Busy=false;}
 }
 private async Task ApplyQueue(bool game)
 {
  if(!CanApply)return;
  if(game){if(SelectedGame==null)return;using var p=Process.GetProcessById(SelectedGame.Id);if(p.StartTime.ToUniversalTime().Ticks!=SelectedGame.StartTicks||p.HasExited)throw new InvalidOperationException("Selected process has exited or changed identity.");watchedGame=SelectedGame;}
  batch=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);Busy=true;
  try{var progress=new Progress<OperationProgress>(p=>Status=$"{p.Completed}/{p.Total} · {Catalog.Get(p.OperationId).Title} · {p.State}");var result=await Task.Run(()=>engine.ApplyAsync(game?"Game session: "+Profile:Profile,preview,batch.Token,progress));Status=result.Message;if(game&&result.Success)gameJournal=result.Journal.Id;await ReloadHistory();Invalidate();await RefreshSettings();}finally{batch.Dispose();batch=null;Busy=false;}
 }
 private async Task Restore(Journal journal)
 {
  Busy=true;try{var progress=new Progress<OperationProgress>(p=>Status=$"Undo · {p.Completed}/{p.Total} · {p.State}");var result=await Task.Run(()=>engine.RestoreAsync(journal,progress));Status=result.Count==0?"Original values verified. Session restored.":"Undo incomplete: "+string.Join("; ",result);await ReloadHistory();Invalidate();if(!recoveryOnly)await RefreshSettings();}finally{Busy=false;}
 }
 private async Task ReloadHistory(){Replace(History,await Task.Run(()=>engine.History()));if(engine.Store.HasQuarantinedHistory)Status="History has quarantined or unreadable records. New batches are blocked; valid sessions can still be restored. Keep the history folder for support.";Raise(nameof(RecoverySummary));Raise(nameof(ActiveProfile));}
 private async Task Diagnostics(){Status="Reading accessible processes, startup entries and registered applications…";await Task.WhenAll(ReadProcesses(),ReadStartup(),ReadApps());Network=string.Join("\n\n",await Task.Run(probe.NetworkInventory));Status="Inventory refreshed. Protected processes and packaged/startup apps may be absent; Windows is the complete management workflow.";}
 private async Task ReadProcesses()=>Replace(Processes,await probe.ProcessesAsync(lifetime.Token));
 private async Task ReadStartup()=>Replace(Startup,await probe.StartupAsync(lifetime.Token));
 private async Task ReadApps()=>Replace(Apps,await probe.AppsAsync(lifetime.Token));
 private List<SystemReading>? baseline;
 private async Task CompareSamples()
 {
  if(string.IsNullOrWhiteSpace(Workload))throw new InvalidOperationException("Describe a repeatable workload before collecting a comparison.");
  Comparison="Collecting ten samples, two seconds apart. Keep your workload consistent…";var samples=new List<SystemReading>();
  for(int i=0;i<10;i++){await Task.Delay(2000,lifetime.Token);samples.Add(await probe.SampleAsync(lifetime.Token));}
  string Stats(List<SystemReading> s)=>$"CPU {s.Where(x=>x.Cpu!=null).Select(x=>x.Cpu!.Value).DefaultIfEmpty(double.NaN).Average():0.0}% · RAM {s.Where(x=>x.MemoryPercent!=null).Select(x=>x.MemoryPercent!.Value).DefaultIfEmpty(double.NaN).Average():0.0}%";
  if(baseline==null){baseline=samples;Comparison="Baseline saved in memory: "+Stats(samples)+". Repeat the same workload and collect again.";}
  else{Comparison=$"Baseline: {Stats(baseline)}\nComparison: {Stats(samples)}\n10 samples each · 2-second interval · context: {Workload}\nThese readings include system noise. Repeat at least three times. They do not measure FPS, frame time, input latency or build completion time.";baseline=null;}
 }
 private void Filter(){Replace(FilteredOperations,Operations.Where(o=>(category=="All"||o.Definition.Category==category)&&(risk=="All risks"||o.Definition.Risk.StartsWith(risk))&&(applicability=="All compatibility"||o.Eligible==(applicability=="Eligible"))&&(state=="All values"||o.Current==state)&&(restart!="Restart required")&&(o.Title+o.Purpose).Contains(search,StringComparison.OrdinalIgnoreCase)));Replace(Guides,GuidanceCatalog.Items.Where(g=>(category=="All"||g.Category==category)&&(g.Title+g.Description).Contains(search,StringComparison.OrdinalIgnoreCase)));}
 private string Friendly(string id,string value)=>id=="power"?Plans.FirstOrDefault(p=>p.Id==value)?.Name+" ["+value+"]":value;
 private static void Add(List<double> values,double? value){if(value==null)return;values.Add(value.Value);if(values.Count>30)values.RemoveAt(0);}
 private static string Points(List<double> v)=>string.Join(" ",v.Select((x,i)=>FormattableString.Invariant($"{i*200d/29:0.##},{40-x*.4:0.##}")));
 private static void Replace<T>(ObservableCollection<T> list,IEnumerable<T> values){list.Clear();foreach(var v in values)list.Add(v);}
 private void RefreshCommands(){foreach(var c in commands)c.Refresh();}
 private void Error(Exception e){var id=logger.Error(e);Status=$"The action could not finish. {e switch { InvalidDataException=>"The file or parameters are unsupported.",UnauthorizedAccessException=>"Windows denied access.",InvalidOperationException=>e.Message,OperationCanceledException=>"The request was cancelled.",_=>"Check availability and try again."}} Reference {id}.";}
 private void OpenAllowed(string value)
 {
  if(Pages.Contains(value)){Page=value;return;}
  if(value=="protection"||value=="restore"){Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,value=="protection"?"SystemPropertiesProtection.exe":"rstrui.exe")){UseShellExecute=true});return;}
  if(GuidanceCatalog.Items.Any(g=>g.Url==value)||Catalog.Operations.Any(o=>o.Documentation==value)||value=="https://github.com/eyad1dk/EZoptimizer/releases")Process.Start(new ProcessStartInfo(value){UseShellExecute=true});
  else throw new InvalidOperationException("This link is not in the supported catalog.");
 }
 public async Task CloseSessionAsync(){if(gameJournal!=null){var j=History.FirstOrDefault(j=>j.Id==gameJournal);gameJournal=null;if(j!=null)await Restore(j);}}
 private bool disposed; public void Dispose(){if(disposed)return;disposed=true;SetThreadExecutionState(0x80000000);lifetime.Cancel();lifetime.Dispose();}
}
