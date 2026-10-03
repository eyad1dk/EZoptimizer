using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
namespace ForgePC;
public partial class MainWindow : Window
{
 private readonly WindowsSettings settings = new();
 private readonly TuningEngine engine;
 private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(2) };
 private List<Change> preview = [];
 private string profile = "Gaming";
 private bool busy;
 [DllImport("kernel32.dll", SetLastError = true)] private static extern uint SetThreadExecutionState(uint flags);
 public MainWindow()
 {
  InitializeComponent();
  engine = new(settings, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EZoptimizer", "history"));
  Loaded += async (_, _) => { await Guard(() => { PlanPicker.ItemsSource = settings.Plans(); SetProfile("Gaming"); LoadHistory(); }); UpdateMetrics(); timer.Start(); };
  timer.Tick += (_, _) => UpdateMetrics();
  Closed += (_, _) => { timer.Stop(); SetThreadExecutionState(0x80000000); };
  Closing += (_, e) => { if (busy) { e.Cancel = true; Status.Text = "Please wait for the current operation to finish."; } };
  PlanPicker.SelectionChanged += (_, _) => InvalidatePreview();
  Animations.Checked += (_, _) => InvalidatePreview(); Animations.Unchecked += (_, _) => InvalidatePreview();
  Menus.Checked += (_, _) => InvalidatePreview(); Menus.Unchecked += (_, _) => InvalidatePreview();
 }
 public void ReadOnlyCheck()
 {
  PlanPicker.ItemsSource = settings.Plans(); SetProfile("Gaming"); UpdateMetrics();
  ProcessList.Text = string.Join("\n", SystemProbe.Processes());
  StartupList.Text = string.Join("\n", SystemProbe.Startup());
  if (SystemProbe.Read().TotalMemory == 0) throw new IOException("Memory probe failed.");
  settings.Read("animations"); settings.Read("menus");
 }
 private Task Guard(Action action)
 {
  try { action(); } catch (Exception e) { Status.Text = e.Message; MessageBox.Show(this, e.Message, "EZoptimizer", MessageBoxButton.OK, MessageBoxImage.Information); }
  return Task.CompletedTask;
 }
 private void Show(string page)
 {
  OverviewPage.Visibility = page == "Overview" ? Visibility.Visible : Visibility.Collapsed;
  ProfilesPage.Visibility = page == "Profiles" ? Visibility.Visible : Visibility.Collapsed;
  InspectPage.Visibility = page == "Inspect" ? Visibility.Visible : Visibility.Collapsed;
  ToolsPage.Visibility = page == "Tools" ? Visibility.Visible : Visibility.Collapsed;
  HistoryPage.Visibility = page == "History" ? Visibility.Visible : Visibility.Collapsed;
  (PageTitle.Text, PageSubtitle.Text) = page switch
  {
   "Profiles" => ("A setup for every session.", "Choose the tradeoffs. Review the changes."),
   "Inspect" => ("See what's running.", "Find the apps that deserve a closer look."),
   "Tools" => ("Useful controls, one place.", "Direct access to Windows settings that matter."),
   "History" => ("Changes you can trace.", "Saved locally, before your settings are touched."),
   _ => ("Your PC, at a glance.", "Real readings. Clear choices. No magic numbers.")
  };
 }
 private async void Navigate(object sender, RoutedEventArgs e)
 {
  var page = ((Button)sender).Tag.ToString()!; Show(page);
  if (page == "Inspect") await RefreshInspection();
  if (page == "History") await Guard(LoadHistory);
 }
 private void InvalidatePreview() { preview.Clear(); ApplyButton.IsEnabled = false; PreviewText.Text = "Options changed. Preview again before applying."; }
 private void SetProfile(string name)
 {
  profile = name; ProfileName.Text = name;
  ProfileDescription.Text = name switch
  {
   "Gaming" => "Less desktop motion, with High performance when available. Check Game Mode and captures in Windows tools for your games.",
   "Coding" => "A responsive desktop with Balanced power. Use the awake option on Overview during long builds or downloads.",
   "Quiet" => "Power saver when available, with less desktop motion. Best when battery life and fan noise matter more than sustained performance.",
   _ => "Balanced power and familiar Windows animations for browsing, calls and everyday work."
  };
  Animations.IsChecked = Menus.IsChecked = name == "Everyday";
  var id = name switch { "Gaming" => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "Quiet" => "a1841308-3541-4fab-bc81-f71556f20b4a", _ => "381b4222-f694-41f0-9685-ff5bb260df2e" };
  var plans = (List<PowerPlan>?)PlanPicker.ItemsSource ?? [];
  var current = settings.Read("power");
  PlanPicker.SelectedItem = plans.FirstOrDefault(p => p.Id == id) ?? plans.FirstOrDefault(p => p.Id == current);
  InvalidatePreview();
 }
 private async void ChooseProfile(object sender, RoutedEventArgs e) => await Guard(() => { SetProfile(((Button)sender).Tag.ToString()!); Show("Profiles"); });
 private string Friendly(string key, string value) => key == "power" ? ((List<PowerPlan>?)PlanPicker.ItemsSource)?.FirstOrDefault(p => p.Id == value)?.Name ?? value : value;
 private async void PreviewChanges(object sender, RoutedEventArgs e) => await Guard(() =>
 {
  preview.Clear(); ApplyButton.IsEnabled = false;
  var desired = new List<(string Key, string Label, string Value)> { ("animations", "App animations", Animations.IsChecked == true ? "On" : "Off"), ("menus", "Menu animations", Menus.IsChecked == true ? "On" : "Off") };
  if (PlanPicker.SelectedItem is PowerPlan plan) desired.Add(("power", "Power plan", plan.Id));
  foreach (var item in desired) { var before = settings.Read(item.Key); if (before != item.Value) preview.Add(new(item.Key, item.Label, before, item.Value)); }
  PreviewText.Text = preview.Count == 0 ? "Already set. No changes needed." : string.Join("\n\n", preview.Select(c => $"{c.Label}: {Friendly(c.Key, c.Before)} → {Friendly(c.Key, c.After)}"));
  ApplyButton.IsEnabled = preview.Count > 0;
  Status.Text = "Preview ready. Nothing has been changed.";
 });
 private async void ApplyChanges(object sender, RoutedEventArgs e)
 {
  if (busy || preview.Count == 0) return;
  busy = true; ApplyButton.IsEnabled = false;
  try
  {
   var changes = preview.ToList(); var name = profile;
   Status.Text = "Saving your settings and applying the session…";
   await Task.Run(() => engine.Apply(name, changes));
   preview.Clear(); PreviewText.Text = "Session applied. Your previous settings are saved in History & undo.";
   Status.Text = "Session applied and verified."; LoadHistory();
  }
  catch (Exception error) { Status.Text = error.Message; MessageBox.Show(this, error.Message, "Could not complete session"); }
  finally { busy = false; }
 }
 private void LoadHistory()
 {
  var history = engine.History();
  HistoryText.Text = history.Count == 0 ? "No sessions yet." : string.Join("\n\n", history.Take(20).Select(j => $"{j.CreatedUtc.ToLocalTime():g}  ·  {j.Profile}  ·  {j.Status}\n" + string.Join("\n", j.Changes.Select(c => $"    {c.Label}: {Friendly(c.Key, c.Before)} → {Friendly(c.Key, c.After)}"))));
  if (history.Any(j => j.Status != "restored")) Status.Text = "A saved session is available. Use History & undo to restore it.";
 }
 private async void Undo(object sender, RoutedEventArgs e)
 {
  if (busy) return; busy = true;
  try
  {
   var journal = engine.History().FirstOrDefault(j => j.Status != "restored");
   if (journal == null) { Status.Text = "No active session to undo."; return; }
   var issues = await Task.Run(() => engine.Restore(journal));
   LoadHistory(); Status.Text = issues.Count == 0 ? "Previous settings restored and verified." : string.Join(" ", issues);
   if (issues.Count > 0) MessageBox.Show(this, string.Join("\n", issues), "Review these settings");
   InvalidatePreview();
  }
  catch (Exception error) { Status.Text = error.Message; }
  finally { busy = false; }
 }
 private void UpdateMetrics()
 {
  try
  {
   var sample = SystemProbe.Read();
   CpuValue.Text = $"{sample.Cpu:0}%"; CpuBar.Value = sample.Cpu; CpuDetail.Text = sample.CpuName;
   var used = 100d * (sample.TotalMemory - sample.FreeMemory) / sample.TotalMemory;
   RamValue.Text = $"{used:0}%"; RamBar.Value = used;
   RamDetail.Text = $"{(sample.TotalMemory - sample.FreeMemory) / 1073741824d:0.0} / {sample.TotalMemory / 1073741824d:0.0} GB in use";
   DiskValue.Text = $"{sample.DiskFree / 1073741824d:0} GB"; DiskBar.Value = 100d * (sample.DiskTotal - sample.DiskFree) / sample.DiskTotal;
   DiskDetail.Text = $"Free of {sample.DiskTotal / 1073741824d:0} GB";
   var advice = new List<string>();
   if (sample.OnBattery) advice.Add("Running on battery. Balanced or Quiet is a useful starting point.");
   if (used > 85) advice.Add("Memory is busy. Inspect your largest processes before closing apps you no longer need.");
   if (sample.DiskFree < sample.DiskTotal * .1) advice.Add("Your system drive has under 10% free space. Review Storage cleanup in Windows tools.");
   if (sample.Cpu > 85) advice.Add("CPU usage is high right now. Check Processes if this persists.");
   Advice.Text = advice.Count == 0 ? "No obvious pressure in this sample. Pick a profile for your workload, and review startup apps if sign-in feels slow." : string.Join("\n\n", advice);
  }
  catch { LiveIndicator.Text = "READING UNAVAILABLE"; }
 }
 private async Task RefreshInspection()
 {
  try { var data = await Task.Run(() => (SystemProbe.Processes(), SystemProbe.Startup())); ProcessList.Text = string.Join("\n", data.Item1); StartupList.Text = data.Item2.Count == 0 ? "No entries found in the inspected locations." : string.Join("\n", data.Item2); }
  catch (Exception error) { Status.Text = error.Message; }
 }
 private async void RefreshLists(object sender, RoutedEventArgs e) => await RefreshInspection();
 private async void OpenTool(object sender, RoutedEventArgs e) => await Guard(() => { Process.Start(new ProcessStartInfo(((Button)sender).Tag.ToString()!) { UseShellExecute = true }); });
 private async void ExportReport(object sender, RoutedEventArgs e) => await Guard(() =>
 {
  var dialog = new SaveFileDialog { FileName = "EZoptimizer-report.json", Filter = "JSON report|*.json" };
  if (dialog.ShowDialog(this) != true) return;
  var sample = SystemProbe.Read();
  var report = new { App = "EZoptimizer", Version = "0.1.0", GeneratedUtc = DateTime.UtcNow, OS = Environment.OSVersion.VersionString, sample.CpuName, LogicalProcessors = Environment.ProcessorCount, sample.TotalMemory, sample.FreeMemory, sample.DiskTotal, sample.DiskFree, sample.OnBattery, PowerPlan = Friendly("power", settings.Read("power")), Animations = settings.Read("animations"), Menus = settings.Read("menus") };
  File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
  Status.Text = "Report saved. Review it before sharing.";
 });
 private void ToggleAwake(object sender, RoutedEventArgs e)
 {
  if (SetThreadExecutionState(Awake.IsChecked == true ? 0x80000001 : 0x80000000) == 0) { Awake.IsChecked = false; Status.Text = "Windows could not accept the awake request."; }
  else Status.Text = Awake.IsChecked == true ? "Idle sleep prevented until you turn this off or close EZoptimizer." : "Normal sleep behavior restored.";
 }
}
