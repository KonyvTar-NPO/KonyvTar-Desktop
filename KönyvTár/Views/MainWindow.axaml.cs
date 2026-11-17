using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Material.Icons;
using KönyvTár.Services.Api;
using KönyvTár.ViewModels;
using KönyvTár.Views.Main;

namespace KönyvTár.Views;

public partial class MainWindow : Window
{
    private readonly ApiClient _apiClient;
    private readonly HomeView _homeView = new();
    private readonly LibraryView _libraryView = new();
    private readonly LendingsView _lendingsView = new();
    private readonly SettingsView _settingsView = new();
    private readonly ProfileView _profileView = new();

    public MainWindow()
        : this(new MainWindowViewModel(), new ApiClient())
    {
    }

    public MainWindow(MainWindowViewModel viewModel, ApiClient apiClient)
    {
        InitializeComponent();
        _apiClient = apiClient;
        DataContext = viewModel;

        _homeView.DataContext = viewModel;
        _libraryView.DataContext = viewModel;
        _lendingsView.DataContext = viewModel;
        _settingsView.DataContext = viewModel;
        _profileView.DataContext = viewModel;
        _profileView.SetApiClient(apiClient);

        _homeView.LibraryRequested += (_, _) => MainContent.Content = _libraryView;
        _homeView.LendingsRequested += (_, _) => MainContent.Content = _lendingsView;
        MainContent.Content = _homeView;

        ThemeButton.Click += OnThemeButtonClick;
        SettingsButton.Click += OnSettingsButtonClick;
        LogoutButton.Click += OnLogoutButtonClick;
        UpdateThemeButton();
    }

    private void OnThemeButtonClick(object? sender, RoutedEventArgs e)
    {
        ThemeManager.Toggle();
        UpdateThemeButton();
    }

    private void UpdateThemeButton() => ThemeIcon.Kind = ThemeManager.IsDark
        ? MaterialIconKind.MoonWaningCrescent
        : MaterialIconKind.WhiteBalanceSunny;

    private void OnSettingsButtonClick(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = _settingsView;
    }

    private void ProfileButton_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = _profileView;
    }

    private void LibraryButton_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = _libraryView;
    }

    private void LendingsButton_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = _lendingsView;
    }

    private void HomeButton_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = _homeView;
    }

    private async void OnLogoutButtonClick(object? sender, RoutedEventArgs e)
    {
        LogoutButton.IsEnabled = false;
        try
        {
            await _apiClient.LogoutAsync();
            await SessionStore.ClearAsync();
        }
        catch
        {
            // The local session is cleared by ApiClient even if the API is unavailable.
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var loginWindow = new LoginWindow();
        desktop.MainWindow = loginWindow;
        loginWindow.Show();
        Close();
    }
}
