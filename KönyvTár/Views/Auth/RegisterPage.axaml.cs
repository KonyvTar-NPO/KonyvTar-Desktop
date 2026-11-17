using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KönyvTár.Views.Auth;

public partial class RegisterPage : UserControl
{
    public event System.Action? BackRequested;

    public RegisterPage()
    {
        InitializeComponent();
        BackButton.Click += (_, _) => BackRequested?.Invoke();
        RegisterButton.Click += OnRegisterClick;
        AccountTypeToggle.IsCheckedChanged += OnAccountTypeChanged;
    }

    private void OnAccountTypeChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        LibrarySection.IsVisible = AccountTypeToggle.IsChecked == true;
    }

    private void OnRegisterClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text)
            || string.IsNullOrWhiteSpace(LastNameTextBox.Text)
            || string.IsNullOrWhiteSpace(EmailTextBox.Text)
            || string.IsNullOrWhiteSpace(PasswordTextBox.Text)
            || string.IsNullOrWhiteSpace(ConfirmPasswordTextBox.Text))
        {
            StatusText.Text = "Tölts ki minden mezőt.";
            return;
        }

        if (PasswordTextBox.Text != ConfirmPasswordTextBox.Text)
        {
            StatusText.Text = "A két jelszó nem egyezik.";
            return;
        }

        StatusText.Text = "A fiók létrehozása jelenleg nem érhető el.";
    }
}
