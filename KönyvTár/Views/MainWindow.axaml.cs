using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Material.Icons;
using KönyvTár.Views.Main;

namespace KönyvTár.Views;

public partial class MainWindow : Window
{
    private LibraryEditView _libraryEditView = new LibraryEditView();
    private LendingsView _lendingsView = new LendingsView();
    private SettingsView _settingsView = new SettingsView();
    private ProfileView _profileView =  new ProfileView();
    
    public MainWindow()
    {
        InitializeComponent();
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
    
    private void LibraryEditButton_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = _libraryEditView;
    }
    
    private void LendingsButton_Click(object? sender, RoutedEventArgs e)
    {
        MainContent.Content = _lendingsView;
    }
}
