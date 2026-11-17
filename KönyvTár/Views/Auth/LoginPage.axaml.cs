using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KönyvTár.Views.Auth;

public partial class LoginPage : UserControl
{
    private PasswordLoginPanel _passwordPage = null!;
    private CodeLoginPanel _codePage = null!;
    
    public event System.Action? LoginRequested;
    public event System.Action? RegisterRequested;
    public event System.Action? CodeLoginRequested;
    public event System.Action? ForgotPasswordRequested;

    public string? Email => _passwordPage.Email;

    public string? Password => _passwordPage.Password;

    public bool StaySignedIn => _passwordPage.StaySignedIn;

    public LoginPage()
    {
        InitializeComponent();

        _passwordPage = new PasswordLoginPanel();
        _codePage = new CodeLoginPanel();

        LoginMethodToggle.IsCheckedChanged += ChangeLoginPage;
        _passwordPage.LoginRequested += () => LoginRequested?.Invoke();
        _passwordPage.CodeLoginRequested += () => LoginMethodToggle.IsChecked = true;
        _passwordPage.ForgotPasswordRequested += () => ForgotPasswordRequested?.Invoke();
        RegisterButton.Click += (_, _) => RegisterRequested?.Invoke();

        ShowPasswordLoginPage();
    }
    
    private void ShowPasswordLoginPage()
    {
        _passwordPage.ShowStatus(string.Empty);
        _passwordPage.EmailTextBox.Text = string.Empty;
        _passwordPage.PasswordTextBox.Text = string.Empty;
        ShowPage(_passwordPage, "KönyvTár — Belépés jelszóval");
    }
    private void ShowCodeLoginPage()
    {
        _codePage.EmailTextBox.Text = string.Empty;
        _codePage.CodeTextBox.Text = string.Empty;
        ShowPage(_codePage, "KönyvTár — Belépés kóddal");
    }

    public void ShowStatus(string message) => _passwordPage.ShowStatus(message);

    public void SetBusy(bool isBusy)
    {
        _passwordPage.SetBusy(isBusy);
        RegisterButton.IsEnabled = !isBusy;
        LoginMethodToggle.IsEnabled = !isBusy;
    }

    private void ChangeLoginPage(object? sender, RoutedEventArgs e)
    {
        if (LoginMethodToggle.IsChecked == true)
        {
            ShowCodeLoginPage();
        }
        else
        {
            ShowPasswordLoginPage();
        }
    }
    
    
    
    private void ShowPage(Control page, string title)
    {
        LoginHost.Content = page;
    }
}
