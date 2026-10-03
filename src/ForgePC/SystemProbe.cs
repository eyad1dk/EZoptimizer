using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
namespace ForgePC;
public record SystemSample(double Cpu, ulong TotalMemory, ulong FreeMemory, long DiskTotal, long DiskFree, string CpuName, bool OnBattery);
public static class SystemProbe
{
 [StructLayout(LayoutKind.Sequential)] private struct Memory
 { public uint Length, Load; public ulong Total, Available, TotalPage, AvailablePage, TotalVirtual, AvailableVirtual, Extended; }
 [StructLayout(LayoutKind.Sequential)] private struct Battery
 { public byte Ac, Flag, Percent, Reserved; public uint Life, FullLife; }
 [DllImport("kernel32.dll")] private static extern bool GlobalMemoryStatusEx(ref Memory memory);
 [DllImport("kernel32.dll")] private static extern bool GetSystemTimes(out long idle, out long kernel, out long user);
 [DllImport("kernel32.dll")] private static extern bool GetSystemPowerStatus(out Battery battery);
 private static long previousIdle, previousTotal;
 public static SystemSample Read()
 {
  var memory = new Memory { Length = (uint)Marshal.SizeOf<Memory>() };
  if (!GlobalMemoryStatusEx(ref memory)) throw new IOException("Cannot read memory usage.");
  double cpu = 0;
  if (GetSystemTimes(out var idle, out var kernel, out var user))
  {
   var total = kernel + user; var delta = total - previousTotal;
   if (previousTotal != 0 && delta > 0) cpu = Math.Clamp(100d * (delta - (idle - previousIdle)) / delta, 0, 100);
   previousIdle = idle; previousTotal = total;
  }
  var disk = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!);
  using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
  GetSystemPowerStatus(out var battery);
  return new(cpu, memory.Total, memory.Available, disk.TotalSize, disk.AvailableFreeSpace, key?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "CPU", battery.Ac == 0);
 }
 public static List<string> Processes()
 {
  var result = new List<(string Name, long Memory)>();
  foreach (var process in Process.GetProcesses())
  {
   using (process) { try { result.Add((process.ProcessName, process.WorkingSet64)); } catch { } }
  }
  return result.OrderByDescending(p => p.Memory).Take(12).Select(p => $"{p.Name,-32} {p.Memory / 1048576d,8:0} MB").ToList();
 }
 public static List<string> Startup()
 {
  var entries = new List<string>();
  foreach (var (root, scope) in new[] { (Registry.CurrentUser, "User"), (Registry.LocalMachine, "PC") })
  {
   using var key = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
   if (key != null) entries.AddRange(key.GetValueNames().Select(name => $"{scope}  /  {name}"));
  }
  foreach (var folder in new[] { Environment.SpecialFolder.Startup, Environment.SpecialFolder.CommonStartup })
  {
   var path = Environment.GetFolderPath(folder);
   if (Directory.Exists(path)) entries.AddRange(Directory.GetFiles(path).Select(p => "Folder  /  " + Path.GetFileNameWithoutExtension(p)));
  }
  return entries.Order().ToList();
 }
}
