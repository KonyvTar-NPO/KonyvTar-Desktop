using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KönyvTár.Views.Auth;

public partial class ForgotEmailPage : UserControl
{
    public event System.Action? BackRequested;
    public event System.Action<string>? CodeRequested;

    public ForgotEmailPage()
    {
        InitializeComponent();
        BackButton.Click += (_, _) => BackRequested?.Invoke();
        SendCodeButton.Click += OnSendCodeClick;
    }

    private void OnSendCodeClick(object? sender, RoutedEventArgs e)
    {
        var email = EmailTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            StatusText.Text = "Add meg az e-mail címed.";
            return;
        }

        CodeRequested?.Invoke(email);
    }
}
