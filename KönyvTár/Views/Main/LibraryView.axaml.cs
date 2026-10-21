using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.ComponentModel;
using KönyvTár.ViewModels;
using KönyvTár.Services;
using KönyvTár.Models;

namespace KönyvTár.Views.Main;

public partial class LibraryView : UserControl
{
    private MainWindowViewModel? _viewModel;
    private bool _syncingDatePickers;

    public LibraryView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ToDatePicker.PropertyChanged += OnDatePickerPropertyChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            SyncDatePickersFromViewModel();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.FromDate)
            or nameof(MainWindowViewModel.ToDate))
            SyncDatePickersFromViewModel();
    }

    private void OnDatePickerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_syncingDatePickers || _viewModel is null
            || e.Property != DatePicker.SelectedDateProperty)
            return;

        if (_viewModel.SelectedLendingDuration != "Egyedi")
            return;

        var end = ToDatePicker.SelectedDate is { } toDate
            ? new DateTimeOffset(toDate.Date)
            : (DateTimeOffset?)null;
        var start = DateTimeOffset.Now.Date;

        _syncingDatePickers = true;
        try
        {
            _viewModel.FromDate = start;
            _viewModel.ToDate = end is { } endDate && endDate < start
                ? null
                : end;
        }
        finally
        {
            _syncingDatePickers = false;
        }

        SyncDatePickersFromViewModel();
    }

    private void SyncDatePickersFromViewModel()
    {
        if (_syncingDatePickers || _viewModel is null)
            return;

        _syncingDatePickers = true;
        try
        {
            if (_viewModel.SelectedLendingDuration == "Egyedi")
                _viewModel.FromDate = DateTimeOffset.Now.Date;

            ToDatePicker.SelectedDate = _viewModel.ToDate?.Date;
        }
        finally
        {
            _syncingDatePickers = false;
        }
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

    private async void EditBookData(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedBook is not { } book)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null)
            return;

        var editedBook = await new EditBookWindow(book, _viewModel.Books).ShowDialog<Book?>(owner);
        if (editedBook is not null)
            _viewModel.UpdateBook(book, editedBook);
    }

    private async void ReturnSelectedLending(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.ReturnSelectedBookLendingAsync();
    }

    private async void EditSelectedLending(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.SelectedBookLending is not { } lending)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null)
            return;

        var editedLending = await new EditLendingWindow(lending, _viewModel.Users).ShowDialog<LendingRecord?>(owner);
        if (editedLending is not null)
            await _viewModel.UpdateLendingAsync(editedLending);
    }

    private async void DeleteSelectedLending(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.DeleteSelectedBookLendingAsync();
    }
}
