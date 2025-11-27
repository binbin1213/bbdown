using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Avalonia.Controls;
using BBDown.GUI.Views;
using System.Runtime.InteropServices;
using ReactiveUI;

using Avalonia.Input;

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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var mainMenu = new NativeMenu();

                var appMenuItem = new NativeMenuItem("BBDown GUI");
                var appMenu = new NativeMenu();

                var aboutItem = new NativeMenuItem("关于 BBDown GUI");
                aboutItem.Command = ReactiveCommand.Create(() => { new AboutWindow().Show(); });
                appMenu.Add(aboutItem);

                appMenu.Add(new NativeMenuItemSeparator());

                var quitItem = new NativeMenuItem("退出 BBDown GUI");
                quitItem.Gesture = new KeyGesture(Key.Q, KeyModifiers.Meta);
                quitItem.Command = ReactiveCommand.Create(() => { desktop.Shutdown(); });
                appMenu.Add(quitItem);

                appMenuItem.Menu = appMenu;
                mainMenu.Add(appMenuItem);

                NativeMenu.SetMenu(this, mainMenu);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}