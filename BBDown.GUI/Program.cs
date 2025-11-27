using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using BBDown.GUI.Views;

namespace BBDown.GUI;

internal static class Program
{
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithMacOptions(new MacPlatformOptions
            {
                UpdateDefaultApplicationMenu = menu =>
                {
                    // The first item is the "About" menu on macOS
                    var about = (NativeMenuItem)menu.Items[0];
                    about.Header = "关于 BBDown GUI";
                    about.Command = ReactiveCommand.Create(() => { new AboutWindow().Show(); });
                }
            })
            .LogToTrace()
            .UseReactiveUI();
}