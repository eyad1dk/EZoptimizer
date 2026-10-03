using ForgePC;
using System.Text.Json;
var root = Path.Combine(Path.GetTempPath(),"EZoptimizer-regression-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int passed = 0;
void Test(string title, Action body) { body(); passed++; Console.WriteLine("PASS " + title); }
void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
void Throws<T>(Action action) where T:Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
(FakeSettings Settings,TuningEngine Engine,JournalStore Store) Fixture()
{
 var settings = new FakeSettings(); var store = new JournalStore(Path.Combine(root,Guid.NewGuid().ToString("N")));
 return (settings,new TuningEngine(settings,store,new PermissiveCapabilities()),store);
}
List<Change> Changes() => [new("animations","Animations","On","Off"),new("menus","Menus","On","Off")];
try
{
 Test("originals are durable and survive a service restart",() => {
  var (s,e,store)=Fixture(); e.Apply("Gaming",Changes()); Assert(s.Values["menus"]=="Off");
  Assert(e.History().Single().Changes.Count==2); Assert(new TuningEngine(s,store,new PermissiveCapabilities()).Restore(store.Load().Single()).Count==0);
  Assert(s.Values["menus"]=="On"); Assert(e.History().Single().Status=="restored");
 });
 Test("stale preview causes zero writes",() => {
  var (s,e,_) = Fixture(); s.Values["menus"]="Off"; Throws<InvalidOperationException>(()=>e.Apply("Coding",Changes())); Assert(s.Writes==0);
 });
 Test("failed write automatically compensates earlier writes",() => {
  var (s,e,_) = Fixture(); s.FailKey="menus"; Throws<InvalidOperationException>(()=>e.Apply("Gaming",Changes()));
  Assert(s.Values["animations"]=="On"); Assert(e.History().Single().Status=="restored");
 });
 Test("interruption after a write leaves retryable recovery",() => {
  var (s,e,_) = Fixture(); s.WriteThenFail=true; Throws<InvalidOperationException>(()=>e.Apply("Coding",Changes()));
  s.WriteThenFail=false; Assert(e.Restore(e.History().Single()).Count==0); Assert(s.Values["animations"]=="On");
 });
 Test("undo preserves external changes and supports retry",() => {
  var (s,e,_) = Fixture(); e.Apply("Gaming",Changes()); s.Values["menus"]="External";
  Assert(e.Restore(e.History().Single()).Count==1); Assert(s.Values["menus"]=="External"); Assert(s.Values["animations"]=="On");
  s.Values["menus"]="Off"; Assert(e.Restore(e.History().Single()).Count==0);
 });
 Test("one active session and idempotent undo",() => {
  var (s,e,_) = Fixture(); var j=e.Apply("Gaming",Changes());
  Throws<InvalidOperationException>(()=>e.Apply("Coding",[new("animations","Animations","Off","On")]));
  e.Restore(j); var count=s.Writes; e.Restore(j); Assert(count==s.Writes); e.Apply("Coding",Changes()); Assert(e.History().Count==2);
 });
 Test("silent write rejection is verified and compensated",() => {
  var (s,e,_) = Fixture(); s.IgnoreWrites=true; Throws<InvalidOperationException>(()=>e.Apply("Gaming",Changes()));
  Assert(e.History().Single().Status=="restored");
 });
 Test("empty batch cannot create a session",() => { var (s,e,_) = Fixture(); Throws<InvalidOperationException>(()=>e.Apply("Gaming",[])); Assert(e.History().Count==0); });
 Test("legacy active journal migrates to typed versioned operations",() => {
  var json=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","legacy-active.json")); var j=JournalStore.Parse(json);
  Assert(j.SchemaVersion==2 && j.Operations[1].ValueType=="Guid" && j.IsActive);
  var (s,e,store)=Fixture(); s.Values["animations"]="Off"; s.Values["power"]="8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"; store.Save(j);
  Assert(e.Restore(store.Load().Single()).Count==0); Assert(s.Values["power"]=="381b4222-f694-41f0-9685-ff5bb260df2e");
 });
 Test("legacy partial restore preserves restored ownership",() => {
  var json=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","legacy-active.json")).Replace("\"Restored\": []","\"Restored\": [\"animations\"]");
  var j=JournalStore.Parse(json); Assert(j.Operations[0].State=="rolled back");
 });
 Test("journal path traversal and future schemas are rejected",() => {
  var j=new Journal { Id="../outside" }; Throws<InvalidDataException>(()=>JournalStore.Validate(j));
  j.Id=Guid.NewGuid().ToString("N"); j.SchemaVersion=99; Throws<InvalidDataException>(()=>JournalStore.Validate(j));
 });
 Test("malformed history is quarantined without hiding valid recovery",() => {
  var directory=Path.Combine(root,Guid.NewGuid().ToString("N")); var store=new JournalStore(directory); store.Save(new Journal { Status="restored" });
  File.WriteAllText(Path.Combine(directory,Guid.NewGuid().ToString("N")+".json"),"{broken");
  Assert(store.Load().Count==1); Assert(store.HasQuarantinedHistory); Assert(Directory.GetFiles(Path.Combine(directory,"quarantine")).Length==1);
  var s=new FakeSettings(); var e=new TuningEngine(s,store,new PermissiveCapabilities());
  Throws<InvalidOperationException>(()=>e.Apply("Gaming",Changes())); Assert(s.Writes==0);
 });
 Test("mismatched file ID is quarantined",() => {
  var directory=Path.Combine(root,Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
  File.WriteAllText(Path.Combine(directory,Guid.NewGuid().ToString("N")+".json"),JsonSerializer.Serialize(new Journal()));
  var store=new JournalStore(directory); Assert(store.Load().Count==0 && store.HasQuarantinedHistory);
 });
 Test("disk full before intent prevents mutation",() => {
  var (s,_,store)=Fixture(); var failing=new FailingStore(store){ FailAt=1 };
  Throws<IOException>(()=>new TuningEngine(s,failing,new PermissiveCapabilities()).Apply("Gaming",Changes())); Assert(s.Writes==0);
 });
 Test("permission failure before intent prevents mutation",() => {
  var (s,_,store)=Fixture(); var failing=new FailingStore(store){ Denied=true,FailAt=1 };
  Throws<UnauthorizedAccessException>(()=>new TuningEngine(s,failing,new PermissiveCapabilities()).Apply("Gaming",Changes())); Assert(s.Writes==0);
 });
 Test("interrupted save after mutation preserves recoverable originals",() => {
  var (s,_,store)=Fixture(); var failing=new FailingStore(store){ FailAt=3 };
  var e=new TuningEngine(s,failing,new PermissiveCapabilities()); Throws<InvalidOperationException>(()=>e.Apply("Gaming",Changes()));
  Assert(s.Values["animations"]=="On"); Assert(store.Load().Single().Changes[0].Before=="On");
 });
 Test("rollback denial retains unresolved record",() => {
  var (s,e,_) = Fixture(); e.Apply("Gaming",Changes()); s.FailKey="menus"; Assert(e.Restore(e.History().Single()).Count==1);
  Assert(e.History().Single().IsActive); s.FailKey=null; Assert(e.Restore(e.History().Single()).Count==0);
 });
 Test("mid-batch stale setting is never written",() => {
  var (s,e,_) = Fixture(); s.AfterWrite=()=>s.Values["menus"]="External";
  Throws<InvalidOperationException>(()=>e.Apply("Gaming",Changes())); Assert(s.Values["menus"]=="External"); Assert(s.Values["animations"]=="On");
 });
 Test("mid-batch policy change stops writes and records recovery denial",() => {
  var (s,_,store)=Fixture(); var policy=new FakeCapabilities(); s.AfterWrite=()=>policy.Denied=true;
  var e=new TuningEngine(s,store,policy); Throws<InvalidOperationException>(()=>e.Apply("Gaming",Changes()));
  Assert(s.Values["menus"]=="On" && store.Load().Single().IsActive); policy.Denied=false; Assert(e.Restore(store.Load().Single()).Count==0);
 });
 Test("cancellation at a safe boundary compensates completed work",() => {
  var (s,e,_) = Fixture(); using var cancel=new CancellationTokenSource(); s.AfterWrite=()=>cancel.Cancel();
  var result=e.ApplyAsync("Coding",Changes(),cancel.Token).GetAwaiter().GetResult();
  Assert(!result.Success && s.Values["animations"]=="On" && s.Values["menus"]=="On");
 });
 Test("unsupported capability blocks planning",() => {
  var s=new FakeSettings(); var p=new ExecutionPlanner(s,new FakeCapabilities { Denied=true });
  Throws<InvalidOperationException>(()=>p.Preview(new Dictionary<string,string>{{"animations","Off"}})); Assert(s.Writes==0);
 });
 Test("duplicate operations and unknown IDs cannot become commands",() => {
  var (s,e,_) = Fixture(); Throws<InvalidDataException>(()=>e.Apply("Gaming",[Changes()[0],Changes()[0]]));
  Throws<InvalidDataException>(()=>e.Apply("Gaming",[new("powershell","Unknown","On","Off")])); Assert(s.Writes==0);
 });
 Test("malformed profiles and unsupported versions are rejected",() => {
  Throws<InvalidDataException>(()=>ProfileStore.Parse("""{"SchemaVersion":9,"Name":"Bad","Parameters":[]}"""));
  Throws<InvalidDataException>(()=>ProfileStore.Serialize(new(1,"Bad",[new("dns-script",1,"anything")])));
  Throws<InvalidDataException>(()=>ProfileStore.Serialize(new(1,"Bad",[new("menus",99,"On")])));
  Throws<InvalidDataException>(()=>ProfileStore.Serialize(new(1,"Bad",[new("menus",1,"C:\\tool.exe")])));
 });
 Test("profiles exclude uninstall, DNS mutations and scripts",() => {
  foreach(var id in new[]{"uninstall","dns","script","service","registry"})
   Throws<InvalidDataException>(()=>ProfileStore.Serialize(new(1,"Blocked",[new(id,1,"On")])));
 });
 Test("custom profile and persistent queue preserve desired values only",() => {
  var p=new ProfileDocument(1,"My setup",[new("animations",1,"Off")]); var parsed=ProfileStore.Parse(ProfileStore.Serialize(p)); Assert(parsed.Parameters.Single().Value=="Off");
  var queue=new QueueStore(Path.Combine(root,"queue.json")); queue.Save(new Dictionary<string,string>{{"menus","Off"}}); Assert(queue.Load()["menus"]=="Off");
 });
 Test("update metadata rejects hostile hosts and malformed records",() => {
  var json="""[{"draft":false,"prerelease":true,"tag_name":"v9.0.0","html_url":"https://evil.example/eyad1dk/EZoptimizer/releases/tag/v9"}]""";
  Assert(UpdateService.Parse(json,true,new(0,2,0)).ReleasePage==null);
  Assert(UpdateService.Parse("{",true,new(0,2,0)).Status=="Invalid");
  Assert(!UpdateService.IsOfficialPage(new("http://github.com/eyad1dk/EZoptimizer/releases/tag/v3")));
 });
 Test("stable and preview update channels are separate",() => {
  var json="""[{"draft":false,"prerelease":true,"tag_name":"v0.3.0","html_url":"https://github.com/eyad1dk/EZoptimizer/releases/tag/v0.3.0"}]""";
  Assert(UpdateService.Parse(json,false,new(0,2,0)).Status=="No releases");
  Assert(UpdateService.Parse(json,true,new(0,2,0)).Status=="Available");
  Throws<NotSupportedException>(UpdateService.InstallDownloadedArtifact);
 });
 Test("missing measurements are never treated as performance gains",() => {
  var recommendations=RecommendationRules.Evaluate(null,null,null,null); Assert(recommendations.Single().Detected.Contains("available"));
  Assert(RecommendationRules.Evaluate(90,5,true,95).Count==4);
 });
 Test("atomic orphan file cannot replace a completed journal",() => {
  var directory=Path.Combine(root,Guid.NewGuid().ToString("N")); var store=new JournalStore(directory);
  var j=new Journal { Status="restored" }; store.Save(j); File.WriteAllText(Path.Combine(directory,"orphan.tmp"),"{broken");
  Assert(store.Load().Single().Id==j.Id);
 });
 Test("restore-point skipped and failure outcomes never claim success",()=>{
  Assert(RestorePointService.ResultFromCode(0).State=="Created");Assert(RestorePointService.ResultFromCode(2).State=="Skipped");Assert(RestorePointService.ResultFromCode(3).State=="Unavailable");Assert(RestorePointService.ResultFromCode(4).State=="Denied");Assert(RestorePointService.ResultFromCode(123).State=="Failed");
 });
 Test("network target rejects URLs, credentials and script input",()=>{
  foreach(var value in new[]{"https://example.com/path","user@example.com","powershell; evil","","C:\\test.exe"})Throws<InvalidDataException>(()=>NetworkDiagnostics.ValidateEndpoint(value));
  Assert(NetworkDiagnostics.ValidateEndpoint("example.com")=="example.com");Assert(NetworkDiagnostics.ValidateEndpoint("::1")=="::1");
 });
 Test("default report excludes user paths, identities, commands and original values",()=>{
  var journal=new Journal{Profile="PRIVATE-PROFILE",Changes=[new("power","secret","PRIVATE-ORIGINAL","PRIVATE-TARGET")]};
  var text=ReportService.Serialize(null,null,[journal]);foreach(var secret in new[]{"PRIVATE-PROFILE","PRIVATE-ORIGINAL","PRIVATE-TARGET","Username","Network","CommandLine"})Assert(!text.Contains(secret));Assert(text.Contains("SchemaVersion"));
 });
 Test("local error logs redact messages and retain correlation",()=>{
  var directory=Path.Combine(root,"logs");var id=new LocalLogger(directory).Error(new IOException("C:\\Users\\Private\\SECRET-KEY"));var text=File.ReadAllText(Path.Combine(directory,"events.jsonl"));Assert(text.Contains(id)&&!text.Contains("SECRET-KEY"));
 });
 Test("official update links reject alternate ports and credentials",()=>{
  Assert(!UpdateService.IsOfficialPage(new("https://github.com:444/eyad1dk/EZoptimizer/releases/tag/v9")));Assert(!UpdateService.IsOfficialPage(new("https://user@github.com/eyad1dk/EZoptimizer/releases/tag/v9")));
 });
 Test("offline and rate-limited checks leave manual recovery usable",()=>{
  var client=new System.Net.Http.HttpClient(new StubHandler(_=>throw new System.Net.Http.HttpRequestException()));Assert(new UpdateService(client).CheckAsync(false).GetAwaiter().GetResult().Status=="Offline");
  client=new(new StubHandler(_=>new(System.Net.HttpStatusCode.TooManyRequests)));Assert(new UpdateService(client).CheckAsync(false).GetAwaiter().GetResult().Status=="Rate limited");
 });
 Test("interrupted metadata cannot replace the application",()=>{
  var client=new System.Net.Http.HttpClient(new StubHandler(_=>throw new IOException("Interrupted stream")));Assert(new UpdateService(client).CheckAsync(false).GetAwaiter().GetResult().Status=="Interrupted");Throws<NotSupportedException>(UpdateService.InstallDownloadedArtifact);
 });
 Test("unsigned or bad signature cannot enter an installation path",()=>{
  // No installer exists until a trusted publisher signature and signed manifest are configured.
  Throws<NotSupportedException>(UpdateService.InstallDownloadedArtifact);
 });
 Test("rollback save failure keeps the returned journal consistent and retryable",()=>{
  var s=new FakeSettings();var inner=new JournalStore(Path.Combine(root,Guid.NewGuid().ToString("N")));var failing=new FailingStore(inner);var e=new TuningEngine(s,failing,new PermissiveCapabilities());var j=e.Apply("Coding",Changes());failing.FailAt=failing.Count+2;Assert(e.Restore(j).Count>0);JournalStore.Validate(j);Assert(inner.Load().Single().IsActive);Assert(e.Restore(j).Count==0);Assert(inner.Load().Single().Status=="restored");
 });
 Test("null custom-profile entries are rejected before planning",()=>Throws<InvalidDataException>(()=>ProfileStore.Parse("""{"SchemaVersion":1,"Name":"Bad","Parameters":[null]}""")));

 Test("all new preference values survive exact simulated undo",()=>{
  foreach(var spec in NativePreferences.Specifications){var (settings,engine,store)=Fixture();var before=spec.Numeric?spec.Minimum.ToString():"On";var after=spec.Numeric?spec.Maximum.ToString():"Off";settings.Values[spec.Id]=before;engine.Apply("Custom",[new(spec.Id,spec.Id,before,after)]);Assert(settings.Values[spec.Id]==after);Assert(engine.Restore(store.Load().Single()).Count==0);Assert(settings.Values[spec.Id]==before);}
 });
 Test("numeric values reject invalid, padded and out-of-range input",()=>{
  foreach(var spec in NativePreferences.Specifications.Where(p=>p.Numeric)){foreach(var value in new[]{"-1","+1","01"," 1","NaN","1.2",(spec.Maximum+1).ToString()})Throws<InvalidDataException>(()=>Catalog.ValidateTarget(spec.Id,value));}
 });
 Test("numeric undo preserves an external change",()=>{
  var (settings,engine,store)=Fixture();settings.Values["mouse-speed"]="10";engine.Apply("Custom",[new("mouse-speed","Pointer","10","12")]);settings.Values["mouse-speed"]="8";Assert(engine.Restore(store.Load().Single()).Count==1);Assert(settings.Values["mouse-speed"]=="8");
 });
 Test("numeric profile roundtrip is typed and lossless",()=>{
  var doc=new ProfileDocument(1,"Input",[new("menu-delay",1,"275"),new("keyboard-repeat",1,"20")]);var restored=ProfileStore.Parse(ProfileStore.Serialize(doc));Assert(restored.Parameters[0].Value=="275");Assert(Catalog.ValueType("menu-delay")=="Integer");
 });
 Test("native argument conventions remain operation-specific",()=>{
  Assert(NativePreferences.Get("mouse-speed").Parameter==NativeParameter.PointerValue);Assert(NativePreferences.Get("keyboard-repeat").Parameter==NativeParameter.UiValue);Assert(NativePreferences.Get("minimize-animation").Parameter==NativeParameter.AnimationStructure);
 });
 string Csv(string value)=>"FrameTimeMs\n"+string.Join("\n",Enumerable.Repeat(value,100));
 Test("frame summary computes constant frame times",()=>{var result=FrameBenchmark.Parse(Csv("10"));Assert(result.Frames==100&&result.AverageFps==100&&result.LowOnePercent==100&&result.P99==10);});
 Test("frame summary uses slowest one percent for lows",()=>{var result=FrameBenchmark.Parse("FrameTimeMs\n"+string.Join("\n",Enumerable.Repeat("10",99).Append("100")));Assert(result.LowOnePercent==10&&result.P99==10);});
 Test("frame import rejects invalid numeric data",()=>{foreach(var value in new[]{"NaN","Infinity","0","-1","60001","oops"})Throws<InvalidDataException>(()=>FrameBenchmark.Parse(Csv(value)));});
 Test("frame import rejects ambiguous headers and short runs",()=>{Throws<InvalidDataException>(()=>FrameBenchmark.Parse("FrameTimeMs,MsBetweenPresents\n10,10"));Throws<InvalidDataException>(()=>FrameBenchmark.Parse("FrameTimeMs\n10"));});
 Test("frame import rejects mixed processes",()=>{Throws<InvalidDataException>(()=>FrameBenchmark.Parse("ProcessID,MsBetweenPresents\n1,10\n2,10"));});
 Test("frame import accepts quoted fields and rejects malformed quotes",()=>{Assert(FrameBenchmark.Parse("\"FrameTimeMs\"\n"+string.Join("\n",Enumerable.Repeat("\"10\"",30))).Frames==30);Throws<InvalidDataException>(()=>FrameBenchmark.Parse("FrameTimeMs\n\"10"));});
 Test("workspace inspection leaves project files intact",()=>{var folder=Path.Combine(root,"workspace");Directory.CreateDirectory(Path.Combine(folder,"node_modules"));var file=Path.Combine(folder,"node_modules","sample.txt");File.WriteAllText(file,"keep me");var result=WorkspaceInspector.Inspect(folder);Assert(result.Contains("No files were changed")&&File.ReadAllText(file)=="keep me");});
 Console.WriteLine($"{passed} regression checks passed. No Windows settings were changed.");
}
finally { Directory.Delete(root,true); }
sealed class FakeSettings : ISettings
{
 public Dictionary<string,string> Values=new(){{"animations","On"},{"menus","On"}};
 public string? FailKey; public bool WriteThenFail,IgnoreWrites; public int Writes; public Action? AfterWrite;
 public string Read(string key)=>Values[key];
 public void Write(string key,string value) { Writes++; if(key==FailKey)throw new IOException("Simulated rejection"); if(!IgnoreWrites)Values[key]=value; AfterWrite?.Invoke(); if(WriteThenFail)throw new IOException("Interrupted"); }
}
sealed class StubHandler(Func<System.Net.Http.HttpRequestMessage,System.Net.Http.HttpResponseMessage> response) : System.Net.Http.HttpMessageHandler
{ protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request,CancellationToken token)=>Task.FromResult(response(request)); }
sealed class FakeCapabilities : ICapabilityService
{ public bool Denied; public Capability Check(string id,string value)=>new(!Denied,Denied?"Managed restriction":"Eligible"); }
sealed class FailingStore(IJournalStore inner) : IJournalStore
{
 public int FailAt,Count; public bool Denied;
 public IReadOnlyList<string> Warnings=>inner.Warnings; public bool HasQuarantinedHistory=>inner.HasQuarantinedHistory;
 public List<Journal> Load()=>inner.Load();
 public void Save(Journal j) { if(++Count==FailAt){ if(Denied)throw new UnauthorizedAccessException("Denied"); throw new IOException("Disk full"); } inner.Save(j); }
}
