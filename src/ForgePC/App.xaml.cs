using System.Windows;
using System.IO;
using System.Security.Principal;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace ForgePC;
public partial class App : Application
{
 private Mutex? instance;
 protected override void OnStartup(StartupEventArgs e)
 {
  if (e.Args.Contains("--smoke-test") || e.Args.Contains("--render-preview"))
  {
   try
   {
    var window = new MainWindow(); window.ReadOnlyCheck();
    if (e.Args.Contains("--render-preview"))
    {
     var destination = e.Args.Last();
     var surface = (FrameworkElement)window.Content;
     surface.Measure(new Size(1180, 815)); surface.Arrange(new Rect(0, 0, 1180, 815)); surface.UpdateLayout();
     var bitmap = new RenderTargetBitmap(1180, 815, 96, 96, PixelFormats.Pbgra32);
     bitmap.Render(surface);
     var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
     using var output = File.Create(destination); encoder.Save(output);
    }
    Shutdown(0);
   }
   catch { Shutdown(1); }
   return;
  }
  instance = new Mutex(true, @"Local\EZoptimizer-" + WindowsIdentity.GetCurrent().User?.Value, out var created);
  if (!created) { MessageBox.Show("EZoptimizer is already running.", "EZoptimizer"); Shutdown(); return; }
  base.OnStartup(e);
 }
 protected override void OnExit(ExitEventArgs e) { instance?.Dispose(); base.OnExit(e); }
}
