using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace KönyvTár.Notifications;

public static class ToastLayer
{
    private static readonly ConditionalWeakTable<Window, ToastHost> Hosts = new();

    public static void Attach(Window window)
    {
        if (Hosts.TryGetValue(window, out _))
            return;

        var content = window.Content as Control;
        if (content is not null)
            window.Content = null;

        var layer = new Grid();
        if (content is not null)
            layer.Children.Add(content);

        var host = new ToastHost();
        layer.Children.Add(host);
        window.Content = layer;

        Hosts.Add(window, host);
    }
}
