using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using KönyvTár.Views;

namespace KönyvTár;

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

        base.OnFrameworkInitializationCompleted();
    }
}

public static class ThemeManager
{
    public static bool IsDark => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    public static void Toggle()
    {
        if (Application.Current is not { } app)
            return;

        app.RequestedThemeVariant = IsDark ? ThemeVariant.Light : ThemeVariant.Dark;
    }
}
