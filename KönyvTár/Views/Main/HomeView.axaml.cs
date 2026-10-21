using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace KönyvTár.Views.Main;

public partial class HomeView : UserControl
{
    public event EventHandler? LibraryRequested;
    public event EventHandler? LendingsRequested;

    public HomeView()
    {
        InitializeComponent();
    }

    private void LibraryWidget_Click(object? sender, RoutedEventArgs e) => LibraryRequested?.Invoke(this, EventArgs.Empty);

    private void LendingsWidget_Click(object? sender, RoutedEventArgs e) => LendingsRequested?.Invoke(this, EventArgs.Empty);
}
