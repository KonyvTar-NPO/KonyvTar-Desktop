using Avalonia.Controls;
using Avalonia.Interactivity;
using KönyvTár.Notifications;

namespace KönyvTár.Views.Auth;

public partial class PasswordLoginPanel : UserControl
{
    public event System.Action? LoginRequested;
    public event System.Action? CodeLoginRequested;
    public event System.Action? ForgotPasswordRequested;

    public string? Email => EmailTextBox.Text;

    public string? Password => PasswordTextBox.Text;

    public bool StaySignedIn => StaySignedInCheckBox.IsChecked == true;

    public PasswordLoginPanel()
    {
        InitializeComponent();
        LoginButton.Click += OnLoginClick;
        ForgotPasswordButton.Click += (_, _) => ForgotPasswordRequested?.Invoke();
    }

    public void ShowStatus(string message) => StatusText.Text = message;

    public void SetBusy(bool isBusy)
    {
        LoginButton.IsEnabled = !isBusy;
        EmailTextBox.IsEnabled = !isBusy;
        PasswordTextBox.IsEnabled = !isBusy;
        ForgotPasswordButton.IsEnabled = !isBusy;
    }

    private void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || string.IsNullOrWhiteSpace(PasswordTextBox.Text))
        {
            const string message = "Add meg e-mail címed és jelszavad.";
            ShowStatus(message);
            ToastService.Instance.ShowWarning(message);
            return;
        }

        LoginRequested?.Invoke();
    }
}
