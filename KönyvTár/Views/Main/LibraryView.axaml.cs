using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using KönyvTár.ViewModels;
using KönyvTár.Services;

namespace KönyvTár.Views.Main;

public partial class LibraryView : UserControl
{
    private MainWindowViewModel? _viewModel;
    private bool _syncingCalendar;

    public LibraryView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        LendingCalendar.SelectedDates.CollectionChanged += OnCalendarSelectionChanged;
        LendingCalendar.AddHandler(
            InputElement.PointerWheelChangedEvent,
            LendingCalendar_PointerWheelChanged,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            SyncCalendarFromViewModel();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.FromDate)
            or nameof(MainWindowViewModel.ToDate))
            SyncCalendarFromViewModel();
    }

    private void OnCalendarSelectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_syncingCalendar || _viewModel is null)
            return;

        if (_viewModel.SelectedLendingDuration != "Egyedi")
            return;

        var dates = LendingCalendar.SelectedDates.OrderBy(date => date).ToList();
        var today = DateTimeOffset.Now.Date;
        _syncingCalendar = true;
        try
        {
            _viewModel.FromDate = today;
            _viewModel.ToDate = dates.Count > 0 && dates[^1] >= today
                ? new DateTimeOffset(dates[^1])
                : null;
        }
        finally
        {
            _syncingCalendar = false;
        }

        SyncCalendarFromViewModel();
    }

    private void SyncCalendarFromViewModel()
    {
        if (_syncingCalendar || _viewModel is null)
            return;

        _syncingCalendar = true;
        try
        {
            LendingCalendar.SelectedDates.Clear();

            if (_viewModel.FromDate is not { } fromDate)
                return;

            LendingCalendar.DisplayDate = fromDate.Date;
            if (_viewModel.ToDate is { } toDate && toDate >= fromDate)
                LendingCalendar.SelectedDates.AddRange(fromDate.Date, toDate.Date);
            else
                LendingCalendar.SelectedDates.Add(fromDate.Date);
        }
        finally
        {
            _syncingCalendar = false;
        }
    }

    private void LendingCalendar_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var maxOffset = Math.Max(0, DetailsScrollViewer.Extent.Height - DetailsScrollViewer.Viewport.Height);
        var nextOffset = Math.Clamp(
            DetailsScrollViewer.Offset.Y - e.Delta.Y * 60,
            0,
            maxOffset);

        DetailsScrollViewer.Offset = new Vector(DetailsScrollViewer.Offset.X, nextOffset);
        e.Handled = true;
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
