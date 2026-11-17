using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace KönyvTár.Views.Main;

public partial class HomeView : UserControl
{
    private const double CompactWidthThreshold = 900;
    private bool? _isCompact;

    public event EventHandler? LibraryRequested;
    public event EventHandler? LendingsRequested;

    public HomeView()
    {
        InitializeComponent();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyResponsiveLayout(e.NewSize.Width < CompactWidthThreshold);
    }

    private void ApplyResponsiveLayout(bool compact)
    {
        if (_isCompact == compact)
            return;

        _isCompact = compact;
        PageContent.Margin = compact ? new Thickness(16, 20) : new Thickness(24);
        PageTitle.FontSize = compact ? 24 : 28;

        if (compact)
        {
            WidgetGrid.ColumnDefinitions.Clear();
            WidgetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            WidgetGrid.RowDefinitions.Clear();
            WidgetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            WidgetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(LibraryWidget, 0);
            Grid.SetRow(LibraryWidget, 0);
            Grid.SetColumn(LendingsWidget, 0);
            Grid.SetRow(LendingsWidget, 1);
            return;
        }

        WidgetGrid.ColumnDefinitions.Clear();
        WidgetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        WidgetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        WidgetGrid.RowDefinitions.Clear();
        WidgetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid.SetColumn(LibraryWidget, 0);
        Grid.SetRow(LibraryWidget, 0);
        Grid.SetColumn(LendingsWidget, 1);
        Grid.SetRow(LendingsWidget, 0);
    }

    private void LibraryWidget_Click(object? sender, RoutedEventArgs e) => LibraryRequested?.Invoke(this, EventArgs.Empty);

    private void LendingsWidget_Click(object? sender, RoutedEventArgs e) => LendingsRequested?.Invoke(this, EventArgs.Empty);
}
