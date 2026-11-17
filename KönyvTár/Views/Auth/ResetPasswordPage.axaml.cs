using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KönyvTár.Views.Auth;

public partial class ResetPasswordPage : UserControl
{
    public event System.Action? BackRequested;
    public event System.Action? Finished;

    public ResetPasswordPage()
    {
        InitializeComponent();
        BackButton.Click += (_, _) => BackRequested?.Invoke();
        SaveButton.Click += OnSaveClick;
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Finished?.Invoke();
    }
}
