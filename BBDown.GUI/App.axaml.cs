using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Avalonia.Controls;
using BBDown.GUI.Views;
using System.Runtime.InteropServices;
using ReactiveUI;

namespace BBDown.GUI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var menu = new NativeMenu();
            var about = new NativeMenuItem("关于 BBDown GUI");
            about.Command = ReactiveCommand.Create(() =>
            {
                new AboutWindow().Show();
            });
            menu.Add(about);
            if (Application.Current != null)
            {
                NativeMenu.SetMenu(Application.Current, menu);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}