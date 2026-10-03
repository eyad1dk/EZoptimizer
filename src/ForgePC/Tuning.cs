using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace ForgePC;

public record Change(string Key, string Label, string Before, string After);
public record PowerPlan(string Id, string Name) { public override string ToString() => Name; }
public sealed class Journal
{
 public string Id { get; set; } = Guid.NewGuid().ToString("N");
 public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
 public string Profile { get; set; } = "";
 public string Status { get; set; } = "pending";
 public List<Change> Changes { get; set; } = [];
 public List<string> Restored { get; set; } = [];
}
public interface ISettings { string Read(string key); void Write(string key, string value); }

public sealed class TuningEngine(ISettings settings, string directory)
{
 private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
 public List<Journal> History()
 {
  Directory.CreateDirectory(directory);
  return Directory.GetFiles(directory, "*.json").Select(p => JsonSerializer.Deserialize<Journal>(File.ReadAllText(p)) ?? throw new InvalidDataException("Invalid history file."))
   .OrderByDescending(x => x.CreatedUtc).ToList();
 }
 private void Save(Journal journal)
 {
  Directory.CreateDirectory(directory);
  var path = Path.Combine(directory, journal.Id + ".json");
  var temporary = path + ".tmp";
  using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
  { JsonSerializer.Serialize(stream, journal, Json); stream.Flush(true); }
  File.Move(temporary, path, true);
 }
 public Journal Apply(string profile, List<Change> changes)
 {
  if (changes.Count == 0) throw new InvalidOperationException("There are no changes to apply.");
  if (History().Any(j => j.Status != "restored")) throw new InvalidOperationException("Undo the existing session before applying another profile.");
  foreach (var change in changes)
   if (settings.Read(change.Key) != change.Before) throw new InvalidOperationException("Settings changed since preview. Refresh the preview first.");
  var journal = new Journal { Profile = profile };
  Save(journal);
  try
  {
   foreach (var change in changes)
   {
    // Persist intent before touching Windows, so an interrupted write can be recovered.
    journal.Changes.Add(change); Save(journal);
    settings.Write(change.Key, change.After);
    if (settings.Read(change.Key) != change.After) throw new IOException("Windows did not accept " + change.Label + ".");
   }
   journal.Status = "applied"; Save(journal); return journal;
  }
  catch (Exception error)
  {
   journal.Status = "interrupted"; Save(journal);
   throw new InvalidOperationException("Apply stopped: " + error.Message + " Use Undo session to restore the saved settings.", error);
  }
 }
 public List<string> Restore(Journal journal)
 {
  var issues = new List<string>();
  foreach (var change in journal.Changes.AsEnumerable().Reverse())
  {
   if (journal.Restored.Contains(change.Key)) continue;
   try
   {
    var current = settings.Read(change.Key);
    if (current != change.Before && current != change.After) { issues.Add(change.Label + " was changed outside Forge. Restore it manually to " + change.Before + "."); continue; }
    if (current != change.Before) settings.Write(change.Key, change.Before);
    if (settings.Read(change.Key) != change.Before) throw new IOException("Restore verification failed.");
    journal.Restored.Add(change.Key); Save(journal);
   }
   catch (Exception e) { issues.Add(change.Label + ": " + e.Message); }
  }
  journal.Status = issues.Count == 0 ? "restored" : "restore incomplete"; Save(journal);
  return issues;
 }
}

public sealed class WindowsSettings : ISettings
{
 [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
 [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetParameter(uint action, uint parameter, out int value, uint flags);
 [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
 [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetParameter(uint action, uint parameter, IntPtr value, uint flags);
 private static (uint Get, uint Set) Flags(string key) => key switch
 { "animations" => (0x1042, 0x1043), "menus" => (0x1002, 0x1003), _ => throw new ArgumentException("Unknown setting.") };
 public string Read(string key)
 {
  if (key == "power") return GuidFrom(RunPower("/getactivescheme"));
  if (!GetParameter(Flags(key).Get, 0, out var value, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
  return value == 0 ? "Off" : "On";
 }
 public void Write(string key, string value)
 {
  if (key == "power")
  {
   var id = Guid.Parse(value).ToString();
   if (!Plans().Any(p => p.Id == id)) throw new InvalidOperationException("That power plan is unavailable on this PC.");
   RunPower("/setactive", id); return;
  }
  if (value is not ("On" or "Off")) throw new ArgumentException("Invalid setting value.");
  if (!SetParameter(Flags(key).Set, 0, value == "On" ? new IntPtr(1) : IntPtr.Zero, 3)) throw new Win32Exception(Marshal.GetLastWin32Error());
 }
 public List<PowerPlan> Plans() => RunPower("/list").Split('\n').Where(line => Regex.IsMatch(line, "[a-fA-F0-9]{8}-[a-fA-F0-9-]{27}"))
  .Select(line => new PowerPlan(GuidFrom(line), Regex.Match(line, @"\((.*)\)").Groups[1].Value)).ToList();
 private static string GuidFrom(string value) => Guid.Parse(Regex.Match(value, "[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}").Value).ToString();
 private static string RunPower(params string[] arguments)
 {
  var info = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "powercfg.exe"))
  { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
  foreach (var argument in arguments) info.ArgumentList.Add(argument);
  using var process = Process.Start(info) ?? throw new IOException("Cannot start Windows power configuration.");
  var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
  if (!process.WaitForExit(10000)) { process.Kill(); throw new TimeoutException("Windows power configuration timed out."); }
  Task.WaitAll(output, error);
  if (process.ExitCode != 0) throw new IOException("Windows rejected the power plan change. " + error.Result.Trim());
  return output.Result;
 }
}
