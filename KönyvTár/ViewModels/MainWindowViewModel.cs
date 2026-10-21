using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KönyvTár.Models;
using KönyvTár.Services;

namespace KönyvTár.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";

    private readonly IBookService _bookService;
    private readonly IUserService _userService;
    private readonly ILendingService _lendingService;
    private string _searchText = string.Empty;
    private string _statusFilter = "Minden";
    private Book? _selectedBook;
    private LibraryUser? _selectedUser;
    private DateTimeOffset? _fromDate = DateTimeOffset.Now.Date;
    private DateTimeOffset? _toDate = DateTimeOffset.Now.Date.AddDays(1);
    private string _selectedLendingDuration = "1 nap";
    private string _lendingStatusMessage = string.Empty;
    private string _lendingSearchText = string.Empty;
    private string _lendingStatusFilter = "Minden";
    private LendingRecord? _selectedLending;

    public ObservableCollection<Book> Books { get; } = [];
    public ObservableCollection<Book> FilteredBooks { get; } = [];
    public ObservableCollection<string> StatusFilters { get; } = ["Minden"];
    public ObservableCollection<LibraryUser> Users { get; } = [];
    public ObservableCollection<LendingRecord> Lendings { get; } = [];
    public ObservableCollection<LendingRecord> FilteredLendings { get; } = [];
    public ObservableCollection<string> LendingStatusFilters { get; } = ["Minden"];
    public ObservableCollection<string> LendingDurationOptions { get; } = ["1 nap", "1 hét", "1 hónap", "Egyedi"];

    public string SelectedLendingDuration
    {
        get => _selectedLendingDuration;
        set
        {
            if (!SetProperty(ref _selectedLendingDuration, value))
                return;

            OnPropertyChanged(nameof(IsCustomLendingDuration));
            if (value == "Egyedi")
                FromDate = DateTimeOffset.Now.Date;

            ToDate = value switch
            {
                "1 nap" => FromDate?.AddDays(1),
                "1 hét" => FromDate?.AddDays(7),
                "1 hónap" => FromDate?.AddMonths(1),
                _ => null
            };
        }
    }

    public bool IsCustomLendingDuration => SelectedLendingDuration == "Egyedi";

    public string LendingDateRange => FromDate is not { } fromDate
        ? "Kezdő dátum hiányzik"
        : ToDate is not { } toDate
            ? $"{fromDate:yyyy. MM. dd.} – válasszon befejezési dátumot"
            : $"{fromDate:yyyy. MM. dd.} – {toDate:yyyy. MM. dd.}";

    public LibraryUser? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
    }

    public DateTimeOffset? FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value))
                OnPropertyChanged(nameof(LendingDateRange));
        }
    }

    public DateTimeOffset? ToDate
    {
        get => _toDate;
        set
        {
            if (SetProperty(ref _toDate, value))
                OnPropertyChanged(nameof(LendingDateRange));
        }
    }

    public string LendingStatusMessage
    {
        get => _lendingStatusMessage;
        private set => SetProperty(ref _lendingStatusMessage, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                RefreshFilteredBooks();
        }
    }

    public string LendingSearchText
    {
        get => _lendingSearchText;
        set { if (SetProperty(ref _lendingSearchText, value)) RefreshFilteredLendings(); }
    }

    public string LendingStatusFilter
    {
        get => _lendingStatusFilter;
        set { if (SetProperty(ref _lendingStatusFilter, value)) RefreshFilteredLendings(); }
    }

    public LendingRecord? SelectedLending
    {
        get => _selectedLending;
        set { if (SetProperty(ref _selectedLending, value))
        {
            OnPropertyChanged(nameof(HasSelectedLending));
            OnPropertyChanged(nameof(LendingTableColumnWidth));
            OnPropertyChanged(nameof(LendingDetailsColumnWidth));
        } }
    }

    public bool HasSelectedLending => SelectedLending is not null;

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
                RefreshFilteredBooks();
        }
    }

    public Book? SelectedBook
    {
        get => _selectedBook;
        set
        {
            if (SetProperty(ref _selectedBook, value))
            {
                OnPropertyChanged(nameof(HasSelectedBook));
                OnPropertyChanged(nameof(BookTableColumnWidth));
                OnPropertyChanged(nameof(BookDetailsColumnWidth));
            }
        }
    }

    public bool HasSelectedBook => SelectedBook is not null;

    public void UpdateBook(Book originalBook, Book updatedBook)
    {
        var index = Books
            .Select((book, bookIndex) => new { book, bookIndex })
            .FirstOrDefault(item => item.book.InventoryCode == originalBook.InventoryCode)
            ?.bookIndex;

        if (index is null)
            return;

        Books[index.Value] = updatedBook;
        if (SelectedBook?.InventoryCode == originalBook.InventoryCode)
            SelectedBook = updatedBook;

        RefreshStatusFilters();
        RefreshFilteredBooks();
    }

    public GridLength BookTableColumnWidth => HasSelectedBook
        ? new GridLength(3, GridUnitType.Star)
        : new GridLength(1, GridUnitType.Star);

    public GridLength BookDetailsColumnWidth => HasSelectedBook
        ? new GridLength(1.5, GridUnitType.Star)
        : new GridLength(0);
    
    private GridLength LendingTableColumnWidth => HasSelectedLending
        ? new GridLength(3, GridUnitType.Star)
        : new  GridLength(1, GridUnitType.Star);
    
    public GridLength LendingDetailsColumnWidth => HasSelectedLending
        ? new GridLength(1.5, GridUnitType.Star)
        : new GridLength(0);

    public MainWindowViewModel()
        : this(new TemporaryBookService(), new TemporaryUserService(), new TemporaryLendingService())
    {
    }

    public MainWindowViewModel(IBookService bookService)
        : this(bookService, new TemporaryUserService(), new TemporaryLendingService())
    {
    }

    public MainWindowViewModel(
        IBookService bookService,
        IUserService userService,
        ILendingService lendingService)
    {
        _bookService = bookService;
        _userService = userService;
        _lendingService = lendingService;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var booksTask = _bookService.GetBooksAsync();
        var usersTask = _userService.GetUsersAsync();
        var lendingsTask = _lendingService.GetLendingsAsync();
        await Task.WhenAll(booksTask, usersTask, lendingsTask);

        Books.Clear();
        foreach (var book in await booksTask)
            Books.Add(book);

        Users.Clear();
        foreach (var user in await usersTask)
            Users.Add(user);

        Lendings.Clear();
        foreach (var lending in await lendingsTask)
            Lendings.Add(lending);

        RefreshStatusFilters();

        LendingStatusFilters.Clear();
        LendingStatusFilters.Add("Minden");
        foreach (var status in Lendings.Select(lending => lending.Status).Distinct())
            LendingStatusFilters.Add(status);

        RefreshFilteredBooks();
        RefreshFilteredLendings();
    }

    [RelayCommand]
    private async Task InitiateLendingAsync()
    {
        if (SelectedBook is null || SelectedUser is null || FromDate is null || ToDate is null)
        {
            LendingStatusMessage = "Válasszon könyvet, felhasználót és dátumtartományt.";
            return;
        }

        if (ToDate < FromDate)
        {
            LendingStatusMessage = "A visszahozatali dátum nem lehet korábbi a kölcsönzés dátumánál.";
            return;
        }

        try
        {
            await _lendingService.CreateLendingAsync(
                new LendingRequest(SelectedBook, SelectedUser, FromDate.Value, ToDate.Value));
            Lendings.Add(new LendingRecord(
                $"KOL-{Lendings.Count + 1:0000}", SelectedBook.InventoryCode, SelectedUser.Name,
                "Központi könyvtár", "Szlonkai Benedek", FromDate.Value, ToDate.Value,
                null, "Aktív", true));
            RefreshFilteredLendings();
            LendingStatusMessage = "Kölcsönzés sikeresen létrehozva.";
        }
        catch (Exception)
        {
            LendingStatusMessage = "A kölcsönzés létrehozása sikertelen.";
        }
    }

    private void RefreshFilteredBooks()
    {
        var search = SearchText.Trim();
        var matches = Books.Where(book =>
            (StatusFilter == "Minden" || book.Status == StatusFilter) &&
            (search.Length == 0 ||
             book.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             book.Author.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             book.Publisher.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             book.InventoryCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             book.ISBN.Contains(search, StringComparison.OrdinalIgnoreCase)));

        FilteredBooks.Clear();
        foreach (var book in matches)
            FilteredBooks.Add(book);

        if (SelectedBook is not null && !FilteredBooks.Contains(SelectedBook))
            SelectedBook = null;
    }

    private void RefreshStatusFilters()
    {
        StatusFilters.Clear();
        StatusFilters.Add("Minden");
        foreach (var status in Books.Select(book => book.Status)
                     .Where(status => !string.IsNullOrWhiteSpace(status))
                     .Distinct())
            StatusFilters.Add(status);

        if (!StatusFilters.Contains(StatusFilter))
            StatusFilter = "Minden";
    }

    private void RefreshFilteredLendings()
    {
        var search = LendingSearchText.Trim();
        var matches = Lendings.Where(lending =>
            (LendingStatusFilter == "Minden" || lending.Status == LendingStatusFilter) &&
            (search.Length == 0 || lending.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             lending.InventoryCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             lending.UserName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             lending.Status.Contains(search, StringComparison.OrdinalIgnoreCase)));

        FilteredLendings.Clear();
        foreach (var lending in matches)
            FilteredLendings.Add(lending);
        if (SelectedLending is not null && !FilteredLendings.Contains(SelectedLending))
            SelectedLending = null;
    }
}
