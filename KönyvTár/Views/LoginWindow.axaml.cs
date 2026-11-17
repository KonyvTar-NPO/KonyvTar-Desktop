using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Material.Icons;
using KönyvTár.Notifications;
using KönyvTár.Services;
using KönyvTár.Services.Api;
using KönyvTár.ViewModels;
using KönyvTár.Views.Auth;
using System;
using System.Net;
using System.Threading.Tasks;

namespace KönyvTár.Views;

public partial class LoginWindow : Window
{
    private readonly ApiClient _apiClient = new();
    private LoginPage _loginPage = null!;
    private RegisterPage _registerPage = null!;
    //private CodeLoginPage _codeLoginPage = null!;
    private ForgotEmailPage _forgotEmailPage = null!;
    private ForgotCodePage _forgotCodePage = null!;
    private ResetPasswordPage _resetPasswordPage = null!;
    private bool _registerPageActive;

    public LoginWindow()
    {
        InitializeComponent();
        Opened += async (_, _) => await RestoreSavedSessionAsync();

        _loginPage = new LoginPage();
        _registerPage = new RegisterPage();
        //_codeLoginPage = new CodeLoginPage();
        _forgotEmailPage = new ForgotEmailPage();
        _forgotCodePage = new ForgotCodePage();
        _resetPasswordPage = new ResetPasswordPage();

        _loginPage.LoginRequested += OnLoginRequested;
        _loginPage.RegisterRequested += () => ShowPage(_registerPage, "KönyvTár — Regisztráció");
       
        _loginPage.ForgotPasswordRequested += () => ShowPage(_forgotEmailPage, "KönyvTár — Jelszó visszaállítása");
        _registerPage.BackRequested += ShowLoginPage;
        //_codeLoginPage.BackRequested += ShowLoginPage;
        _forgotEmailPage.BackRequested += ShowLoginPage;
        _forgotEmailPage.CodeRequested += ShowForgotCodePage;
        _forgotCodePage.BackRequested += () => ShowPage(_forgotEmailPage, "KönyvTár — Jelszó visszaállítása");
        _forgotCodePage.ContinueRequested += () => ShowPage(_resetPasswordPage, "KönyvTár — Új jelszó");
        _resetPasswordPage.BackRequested += () => ShowPage(_forgotCodePage, "KönyvTár — E-mail ellenőrzése");
        _resetPasswordPage.Finished += OnPasswordResetFinished;

        //ThemeButton.Click += OnThemeButtonClick;
        //UpdateThemeButton();
        ShowLoginPage();
    }

    private async Task RestoreSavedSessionAsync()
    {
        var token = await SessionStore.ReadAsync();
        if (token is null)
            return;

        try
        {
            var user = await _apiClient.RestoreSessionAsync(token);
            if (user.IsStaff)
            {
                OpenMainWindow();
                return;
            }
        }
        catch
        {
            // Expired or revoked token: remove it and show login screen.
        }

        await _apiClient.LogoutAsync();
        await SessionStore.ClearAsync();
    }

    private void ShowLoginPage()
    {
        
        ShowPage(_loginPage, "KönyvTár — Belépés");
    }

    private void ShowForgotCodePage(string email)
    {
        _forgotCodePage.SetEmail(email);
        ShowPage(_forgotCodePage, "KönyvTár — E-mail ellenőrzése");
    }

    private void ShowPage(Control page, string title)
    {
        PageHost.Content = page;
        Title = title;
    }

    private void OnPasswordResetFinished()
    {
        ShowLoginPage();
        _loginPage.ShowStatus("A jelszó-visszaállítás jelenleg csak bemutató.");
    }

    //private void OnThemeButtonClick(object? sender, RoutedEventArgs e)
    //{
    //    ThemeManager.Toggle();
    //    UpdateThemeButton();
    //}

    //private void UpdateThemeButton() => ThemeIcon.Kind = ThemeManager.IsDark
    //    ? MaterialIconKind.MoonWaningCrescent
    //    : MaterialIconKind.WhiteBalanceSunny;
    
    private async void OnLoginRequested()
    {
        var email = _loginPage.Email?.Trim();
        var password = _loginPage.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            const string message = "Add meg az e-mail címed és a jelszavad.";
            _loginPage.ShowStatus(message);
            ToastService.Instance.ShowWarning(message);
            return;
        }

        _loginPage.ShowStatus(string.Empty);
        _loginPage.SetBusy(true);

        try
        {
            var user = await _apiClient.LoginAsync(email, password);

            if (!user.IsStaff)
            {
                await _apiClient.LogoutAsync();
                const string message = "Ez a felület könyvtárosok és rendszergazdák számára érhető el.";
                _loginPage.ShowStatus(message);
                ToastService.Instance.ShowWarning(message);
                return;
            }

            if (_loginPage.StaySignedIn)
                await SessionStore.SaveAsync(_apiClient.SessionToken!);
            else
                await SessionStore.ClearAsync();

            OpenMainWindow();
            ToastService.Instance.ShowSuccess($"Sikeres bejelentkezés: {user.FullName}.");
        }
        catch (ApiException exception)
        {
            var message = exception.StatusCode == HttpStatusCode.UnprocessableEntity
                ? "Hibás e-mail cím vagy jelszó."
                : exception.Message;
            _loginPage.ShowStatus(message);
            ToastService.Instance.ShowError(message);
        }
        catch (Exception)
        {
            const string message = "A belépés sikertelen. Próbáld újra később.";
            _loginPage.ShowStatus(message);
            ToastService.Instance.ShowError(message);
        }
        finally
        {
            _loginPage.SetBusy(false);
        }
    }

    private void OpenMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var viewModel = new MainWindowViewModel(
            new ApiBookService(_apiClient),
            new ApiUserService(_apiClient),
            new ApiLendingService(_apiClient),
            _apiClient.CurrentUser);

        var mainWindow = new MainWindow(viewModel, _apiClient);

        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        Close();
    }
}
