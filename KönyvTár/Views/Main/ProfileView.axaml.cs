using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Threading.Tasks;
using KönyvTár.Notifications;
using KönyvTár.Services.Api;
using KönyvTár.ViewModels;

namespace KönyvTár.Views.Main;

public partial class ProfileView : UserControl
{
    private ApiClient? _apiClient;

    public ProfileView()
    {
        InitializeComponent();
        EditProfileButton.Click += (_, _) => BeginEditing();
        ShowPasswordButton.Click += (_, _) => BeginPasswordChange();
        CancelProfileButton.Click += (_, _) => EndEditing();
        SaveProfileButton.Click += async (_, _) => await SaveProfileAsync();
        ChangePasswordButton.Click += async (_, _) => await ChangePasswordAsync();
        ClosePasswordButton.Click += (_, _) => EndPasswordChange();
    }

    public void SetApiClient(ApiClient apiClient)
    {
        _apiClient = apiClient;
        LoadProfileFields(apiClient.CurrentUser);
    }

    private void BeginEditing()
    {
        ProfileStatus.Text = string.Empty;
        LoadProfileFields(_apiClient?.CurrentUser);
        //ProfileActions.IsVisible = false;
        ProfileEditPanel.IsVisible = true;
        PasswordPanel.IsVisible = false;
    }

    private void EndEditing()
    {
        ProfileStatus.Text = string.Empty;
        ProfileEditPanel.IsVisible = false;
        //ProfileActions.IsVisible = true;
    }

    private void BeginPasswordChange()
    {
        PasswordStatus.Text = string.Empty;
        CurrentPasswordBox.Text = NewPasswordBox.Text = ConfirmPasswordBox.Text = string.Empty;
        //ProfileActions.IsVisible = false;
        ProfileEditPanel.IsVisible = false;
        PasswordPanel.IsVisible = true;

    }

    private void EndPasswordChange()
    {
        PasswordPanel.IsVisible = false;
        ProfileActions.IsVisible = true;
    }

    private void LoadProfileFields(ApiUser? user)
    {
        FirstNameBox.Text = user?.FirstName ?? string.Empty;
        LastNameBox.Text = user?.LastName ?? string.Empty;
        EmailBox.Text = user?.Email ?? string.Empty;
        PhoneBox.Text = user?.PhoneNumber ?? string.Empty;
    }

    private async Task SaveProfileAsync()
    {
        if (_apiClient is null)
            return;
        var firstName = FirstNameBox.Text?.Trim() ?? string.Empty;
        var lastName = LastNameBox.Text?.Trim() ?? string.Empty;
        var email = EmailBox.Text?.Trim() ?? string.Empty;
        if (firstName.Length == 0 || lastName.Length == 0 || email.Length == 0)
        {
            const string message = "A keresztnév, vezetéknév és e-mail cím megadása kötelező.";
            ProfileStatus.Text = message;
            ToastService.Instance.ShowWarning(message);
            return;
        }

        SaveProfileButton.IsEnabled = false;
        ProfileStatus.Text = string.Empty;
        try
        {
            var updatedUser = await _apiClient.UpdateProfileAsync(firstName, lastName, email, PhoneBox.Text?.Trim() ?? string.Empty);
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.ApplyCurrentUser(updatedUser);
            EndEditing();
            ProfileStatus.Foreground = Brushes.ForestGreen;
            ProfileStatus.Text = "Profil mentve.";
            ToastService.Instance.ShowSuccess("Profil mentve.");
        }
        catch (Exception exception)
        {
            ProfileStatus.Foreground = Brushes.Firebrick;
            ProfileStatus.Text = exception.Message;
            ToastService.Instance.ShowError(exception.Message);
        }
        finally
        {
            SaveProfileButton.IsEnabled = true;
        }
    }

    private async Task ChangePasswordAsync()
    {
        if (_apiClient is null)
            return;
        var current = CurrentPasswordBox.Text ?? string.Empty;
        var next = NewPasswordBox.Text ?? string.Empty;
        var confirmation = ConfirmPasswordBox.Text ?? string.Empty;
        if (current.Length == 0 || next.Length == 0 || confirmation.Length == 0)
        {
            const string message = "Töltsd ki mindhárom jelszómezőt.";
            PasswordStatus.Text = message;
            ToastService.Instance.ShowWarning(message);
            return;
        }
        if (next != confirmation)
        {
            const string message = "Az új jelszavak nem egyeznek.";
            PasswordStatus.Text = message;
            ToastService.Instance.ShowWarning(message);
            return;
        }

        ChangePasswordButton.IsEnabled = false;
        PasswordStatus.Text = string.Empty;
        try
        {
            await _apiClient.ChangePasswordAsync(current, next);
            CurrentPasswordBox.Text = NewPasswordBox.Text = ConfirmPasswordBox.Text = string.Empty;
            PasswordStatus.Foreground = Brushes.ForestGreen;
            PasswordStatus.Text = "Jelszó módosítva.";
            ToastService.Instance.ShowSuccess("Jelszó módosítva.");
        }
        catch (Exception exception)
        {
            PasswordStatus.Foreground = Brushes.Firebrick;
            PasswordStatus.Text = exception.Message;
            ToastService.Instance.ShowError(exception.Message);
        }
        finally
        {
            ChangePasswordButton.IsEnabled = true;
        }
    }
}
