using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
namespace ForgePC;
public sealed class WindowsSettings : ISettings
{
 [DllImport("user32.dll", EntryPoint="SystemParametersInfoW", SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]
 private static extern bool GetParameter(uint action,uint parameter,out int value,uint flags);
 [DllImport("user32.dll", EntryPoint="SystemParametersInfoW", SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]
 private static extern bool SetParameter(uint action,uint parameter,IntPtr value,uint flags);
 private static (uint Get,uint Set) Flags(string key) => key switch
 { "animations" => (0x1042,0x1043), "menus" => (0x1002,0x1003), _ => throw new ArgumentException("Unknown setting.") };
 public string Read(string key)
 {
  if (key == "power") return GuidFrom(RunPower("/getactivescheme"));
  if (!GetParameter(Flags(key).Get,0,out var v,0)) throw new Win32Exception(Marshal.GetLastWin32Error());
  return v == 0 ? "Off" : "On";
 }
 public void Write(string key,string value)
 {
  Catalog.ValidateTarget(key,value);
  if (key == "power")
  {
   if (!Plans().Any(p => p.Id == value)) throw new InvalidOperationException("This power plan is no longer installed.");
   RunPower("/setactive",value); return;
  }
  if (!SetParameter(Flags(key).Set,0,value == "On" ? new IntPtr(1) : IntPtr.Zero,3)) throw new Win32Exception(Marshal.GetLastWin32Error());
 }
 public List<PowerPlan> Plans() => RunPower("/list").Split('\n').Where(l => Regex.IsMatch(l,"[a-fA-F0-9]{8}-[a-fA-F0-9-]{27}"))
  .Select(l => new PowerPlan(GuidFrom(l),Regex.Match(l,@"\((.*)\)").Groups[1].Value)).ToList();
 private static string GuidFrom(string value) => Guid.Parse(Regex.Match(value,"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}").Value).ToString();
 private static string RunPower(params string[] args)
 {
  var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"powercfg.exe"))
  { UseShellExecute=false, RedirectStandardOutput=true, RedirectStandardError=true, CreateNoWindow=true };
  foreach (var arg in args) start.ArgumentList.Add(arg);
  using var p = Process.Start(start) ?? throw new IOException("Windows power configuration is unavailable.");
  var output = p.StandardOutput.ReadToEndAsync(); var error = p.StandardError.ReadToEndAsync();
  if (!p.WaitForExit(8000)) { p.Kill(); throw new TimeoutException("Power configuration timed out."); }
  Task.WaitAll(output,error);
  if (p.ExitCode != 0) throw new IOException("Windows refused power configuration. Review policy and privileges in Windows.");
  return output.Result;
 }
}
