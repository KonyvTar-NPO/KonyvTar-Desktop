using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using KönyvTár.Models;
using KönyvTár.ViewModels;

namespace KönyvTár.Views.Main;

public partial class LendingsView : UserControl
{
    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    public LendingsView()
    {
        InitializeComponent();
    }

    private void EmptyFilterTextBox(object? sender, RoutedEventArgs e)
    {
        try
        {
            FilterTextBox.Clear();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    private async void EditSelectedLending(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedLending is not { } lending)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null)
            return;

        var edited = await new EditLendingWindow(lending, ViewModel.Users).ShowDialog<LendingRecord?>(owner);
        if (edited is not null)
            await ViewModel.UpdateLendingAsync(edited);
    }

    private async void ReturnSelectedLending(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedLending is { } lending)
            await ViewModel.ReturnLendingAsync(lending);
    }

    private async void DeleteSelectedLending(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedLending is { } lending)
            await ViewModel.DeleteLendingAsync(lending);
    }
}
