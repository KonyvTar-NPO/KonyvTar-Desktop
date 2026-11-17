using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KönyvTár.Notifications;

public partial class ToastHost : UserControl
{
    public ToastHost()
    {
        InitializeComponent();
        ToastItems.ItemsSource = ToastService.Instance.Messages;
    }

    private void CloseToast(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ToastMessage toast })
            ToastService.Instance.Close(toast);
    }
}
