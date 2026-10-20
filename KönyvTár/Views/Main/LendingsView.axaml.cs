using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace KönyvTár.Views.Main;

public partial class LendingsView : UserControl
{
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
}
