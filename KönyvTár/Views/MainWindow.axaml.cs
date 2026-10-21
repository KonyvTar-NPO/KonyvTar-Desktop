using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Material.Icons;
using KönyvTár.ViewModels;
using KönyvTár.Views.Main;

namespace KönyvTár.Views;

public partial class MainWindow : Window
{
    private HomeView _homeView = new HomeView();
    private LibraryView _libraryView = new LibraryView();
    private LendingsView _lendingsView = new LendingsView();
    private SettingsView _settingsView = new SettingsView();
    private ProfileView _profileView =  new ProfileView();
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        _homeView.DataContext = DataContext;
        _libraryView.DataContext = DataContext;
        _lendingsView.DataContext = DataContext;
        _homeView.LibraryRequested += (_, _) => MainContent.Content = _libraryView;
        _homeView.LendingsRequested += (_, _) => MainContent.Content = _lendingsView;
        MainContent.Content = _homeView;
        ThemeButton.Click += OnThemeButtonClick;
        SettingsButton.Click += OnSettingsButtonClick;
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
}
