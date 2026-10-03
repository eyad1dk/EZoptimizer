using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Automation.Peers;
using System.Windows.Threading;
using Microsoft.Win32;
namespace ForgePC;
public sealed class PageVisibilityConverter : IValueConverter
{
 public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>Equals(value?.ToString(),parameter?.ToString())?Visibility.Visible:Visibility.Collapsed;
 public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
}
public sealed class NavigationTemplateSelector : DataTemplateSelector
{
 public override DataTemplate? SelectTemplate(object item,DependencyObject container)=>item is string page?((FrameworkElement)container).TryFindResource("Page-"+page) as DataTemplate:null;
}
public sealed class DesktopInteraction : IUserInteraction
{
 public Task<string?> ReadProfile(){var picker=new OpenFileDialog {Filter="EZoptimizer profile (*.json)|*.json",CheckFileExists=true};if(picker.ShowDialog()!=true)return Task.FromResult<string?>(null);if(new FileInfo(picker.FileName).Length>65536)throw new InvalidDataException("Profile exceeds 64 KB.");return File.ReadAllTextAsync(picker.FileName)!;}
 public async Task WriteText(string text,string name){var picker=new SaveFileDialog {FileName=name,Filter="JSON (*.json)|*.json",AddExtension=true};if(picker.ShowDialog()==true)await File.WriteAllTextAsync(picker.FileName,text);}
 public bool Confirm(string text,string title)
 {
  var dialog=new Window{Title=title,Width=620,Height=460,MinWidth=360,MinHeight=240,Owner=Application.Current.MainWindow,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=(Brush)Application.Current.FindResource("Canvas"),Foreground=(Brush)Application.Current.FindResource("Ink")};
  var dock=new DockPanel{Margin=new Thickness(24)};var buttons=new WrapPanel();DockPanel.SetDock(buttons,Dock.Bottom);var yes=new Button{Content="Continue",IsDefault=true};var no=new Button{Content="Cancel",IsCancel=true};yes.Click+=(_,_)=>dialog.DialogResult=true;buttons.Children.Add(yes);buttons.Children.Add(no);dock.Children.Add(buttons);dock.Children.Add(new ScrollViewer{Content=new TextBlock{Text=text},VerticalScrollBarVisibility=ScrollBarVisibility.Auto});dialog.Content=dock;return dialog.ShowDialog()==true;
 }
 public void ApplyTheme(string theme)
 {
  var high=SystemParameters.HighContrast||theme=="High contrast";
  bool light=theme=="Light" || theme=="Windows" && (int?)(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","AppsUseLightTheme",0))==1;
  var colors=high?new[]{"#000000","#000000","#202020","#FFFFFF","#FFFFFF","#FFFFFF","#FFFF00","#000000"}:light?new[]{"#F4F6FA","#FFFFFF","#E3EAF3","#76879C","#142034","#45566D","#096850","#FFFFFF"}:new[]{"#0B1220","#151F30","#23334A","#40516C","#F1F5FC","#B9C7DA","#66E4C0","#062B22"};
  var keys=new[]{"Canvas","Surface","Raised","Line","Ink","Muted","Accent","OnAccent"};
  for(int i=0;i<keys.Length;i++)Application.Current.Resources[keys[i]]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i]));
  if(SystemParameters.HighContrast){Application.Current.Resources["Canvas"]=SystemColors.WindowBrush;Application.Current.Resources["Surface"]=SystemColors.WindowBrush;Application.Current.Resources["Raised"]=SystemColors.ControlBrush;Application.Current.Resources["Ink"]=SystemColors.WindowTextBrush;Application.Current.Resources["Muted"]=SystemColors.WindowTextBrush;Application.Current.Resources["Accent"]=SystemColors.HighlightBrush;Application.Current.Resources["OnAccent"]=SystemColors.HighlightTextBrush;Application.Current.Resources["Line"]=SystemColors.WindowTextBrush;}
 }
}
public partial class MainWindow : Window
{
 public MainViewModel ViewModel {get;}
 private readonly DispatcherTimer timer=new();private bool closeReady;
 public MainWindow(string dataRoot,bool recoveryOnly=false)
 {
  InitializeComponent();ViewModel=new(dataRoot,new DesktopInteraction(),recoveryOnly);DataContext=ViewModel;
  timer.Interval=TimeSpan.FromSeconds(2);timer.Tick+=async (_,_)=>{timer.Interval=TimeSpan.FromSeconds(IsActive&&WindowState!=WindowState.Minimized?2:10);await ViewModel.TickAsync();};
  ViewModel.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(MainViewModel.Status))UIElementAutomationPeer.CreatePeerForElement(OperationStatus)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);};
  Closing+=ClosingWindow;Closed+=(_,_)=>{timer.Stop();SystemParameters.StaticPropertyChanged-=SystemChanged;SystemEvents.UserPreferenceChanged-=PreferenceChanged;ViewModel.Dispose();};SystemParameters.StaticPropertyChanged+=SystemChanged;SystemEvents.UserPreferenceChanged+=PreferenceChanged;
 }
 public async Task InitializeAsync(bool poll=true){await ViewModel.InitializeAsync();if(poll)timer.Start();}
 public void VerifyPageLayout()
 {
  IEnumerable<FrameworkElement> Elements(DependencyObject root){for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is FrameworkElement f)yield return f;foreach(var nested in Elements(child))yield return nested;}}
  var pages=Elements(Root).Where(e=>e.Tag?.ToString()?.StartsWith("Page-")==true && e.Visibility==Visibility.Visible).ToList();
  if(pages.Count!=1||pages[0].ActualWidth<=0||pages[0].Tag.ToString()!="Page-"+ViewModel.Page)throw new InvalidOperationException("Navigation did not produce exactly one measurable page.");
  if(System.Windows.Automation.AutomationProperties.GetName(OperationStatus)!="Operation status")throw new InvalidOperationException("Status automation name is missing.");
 }
 private void SystemChanged(object? sender,PropertyChangedEventArgs e){if(e.PropertyName==nameof(SystemParameters.HighContrast))new DesktopInteraction().ApplyTheme(ViewModel.Theme);}
 private void PreferenceChanged(object sender,UserPreferenceChangedEventArgs e){if(ViewModel.Theme=="Windows" && e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.VisualStyle)Dispatcher.InvokeAsync(()=>new DesktopInteraction().ApplyTheme(ViewModel.Theme));}
 private async void ClosingWindow(object? sender,CancelEventArgs e){if(closeReady)return;if(ViewModel.Busy){e.Cancel=true;ViewModel.Status="Wait for the reviewed operation and recovery to finish before closing.";return;}e.Cancel=true;await ViewModel.CloseSessionAsync();closeReady=true;Close();}
}
