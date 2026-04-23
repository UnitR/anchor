#if WINDOWS
using Microsoft.UI.Xaml;

namespace Anchor.Desktop.WinUI;

public partial class App : MauiWinUIApplication
{
    public App() { InitializeComponent(); }
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
#endif
