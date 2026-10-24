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
    private readonly IBookService _bookService;
    private readonly IUserService _userService;
    private readonly ILendingService _lendingService;
    private string _searchText = string.Empty;
    private string _statusFilter = "Minden";
    private Book? _selectedBook;
    private LibraryUser? _selectedUser;
    private string _selectedUserSearchText = string.Empty;
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

    public string SelectedUserSearchText
    {
        get => _selectedUserSearchText;
        set => SetProperty(ref _selectedUserSearchText, value);
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
            OnPropertyChanged(nameof(CanReturnSelectedLending));
        } }
    }

    public bool HasSelectedLending => SelectedLending is not null;

    public bool CanReturnSelectedLending => SelectedLending is { ReturnedAt: null };

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
            var previousInventoryCode = _selectedBook?.InventoryCode;
            if (SetProperty(ref _selectedBook, value))
            {
                OnPropertyChanged(nameof(HasSelectedBook));
                OnPropertyChanged(nameof(SelectedBookLending));
                OnPropertyChanged(nameof(HasSelectedBookLending));
                OnPropertyChanged(nameof(ShowMissingSelectedBookLending));
                OnPropertyChanged(nameof(IsSelectedBookLent));
                OnPropertyChanged(nameof(CanStartLending));
                OnPropertyChanged(nameof(BookTableColumnWidth));
                OnPropertyChanged(nameof(BookDetailsColumnWidth));

                if (previousInventoryCode != value?.InventoryCode)
                    ResetLendingInputs();
            }
        }
    }

    private void ResetLendingInputs()
    {
        SelectedUser = null;
        SelectedUserSearchText = string.Empty;
        LendingStatusMessage = string.Empty;
        SelectedLendingDuration = "1 nap";
        FromDate = DateTimeOffset.Now.Date;
        ToDate = FromDate?.AddDays(1);
    }

    public bool HasSelectedBook => SelectedBook is not null;

    public LendingRecord? SelectedBookLending => SelectedBook is { } book
        ? Lendings.FirstOrDefault(lending => lending.InventoryCode == book.InventoryCode && lending.ReturnedAt is null)
        : null;

    public bool HasSelectedBookLending => SelectedBookLending is not null;

    public bool ShowMissingSelectedBookLending => IsSelectedBookLent && !HasSelectedBookLending;

    public bool IsSelectedBookLent => SelectedBook?.Status == "Kölcsönözve";

    public bool CanStartLending => HasSelectedBook && !IsSelectedBookLent;

    public async Task UpdateLendingAsync(LendingRecord updatedLending)
    {
        var index = Lendings.ToList().FindIndex(lending => lending.Id == updatedLending.Id);
        if (index < 0)
            return;

        await _lendingService.UpdateLendingAsync(updatedLending);
        Lendings[index] = updatedLending;
        if (SelectedLending?.Id == updatedLending.Id)
            SelectedLending = updatedLending;
        RefreshFilteredLendings();
        OnPropertyChanged(nameof(SelectedBookLending));
        OnPropertyChanged(nameof(HasSelectedBookLending));
        OnPropertyChanged(nameof(ShowMissingSelectedBookLending));
        RefreshHomeSummary();
    }

    public async Task DeleteSelectedBookLendingAsync()
    {
        if (SelectedBookLending is { } lending)
            await DeleteLendingAsync(lending);
    }

    public async Task DeleteLendingAsync(LendingRecord lending)
    {
        if (!Lendings.Any(item => item.Id == lending.Id))
            return;

        await _lendingService.DeleteLendingAsync(lending.Id);
        Lendings.Remove(Lendings.First(item => item.Id == lending.Id));
        MakeBookAvailableIfNoActiveLending(lending.InventoryCode);
        if (SelectedLending?.Id == lending.Id)
            SelectedLending = null;
        RefreshFilteredLendings();
        OnPropertyChanged(nameof(SelectedBookLending));
        OnPropertyChanged(nameof(HasSelectedBookLending));
        OnPropertyChanged(nameof(IsSelectedBookLent));
        OnPropertyChanged(nameof(ShowMissingSelectedBookLending));
        OnPropertyChanged(nameof(CanStartLending));
        RefreshHomeSummary();
    }

    public async Task ReturnSelectedBookLendingAsync()
    {
        if (SelectedBookLending is { } lending)
            await ReturnLendingAsync(lending);
    }

    public async Task ReturnLendingAsync(LendingRecord lending)
    {
        if (lending.ReturnedAt is not null || !Lendings.Any(item => item.Id == lending.Id))
            return;

        var returnedLending = lending with
        {
            ReturnedAt = DateTimeOffset.Now,
            Status = "Visszahozva"
        };
        await _lendingService.UpdateLendingAsync(returnedLending);

        var lendingIndex = Lendings.ToList().FindIndex(item => item.Id == lending.Id);
        if (lendingIndex >= 0)
            Lendings[lendingIndex] = returnedLending;
        if (SelectedLending?.Id == lending.Id)
            SelectedLending = returnedLending;
        MakeBookAvailableIfNoActiveLending(lending.InventoryCode);
        RefreshFilteredLendings();
        OnPropertyChanged(nameof(SelectedBookLending));
        OnPropertyChanged(nameof(HasSelectedBookLending));
        OnPropertyChanged(nameof(IsSelectedBookLent));
        OnPropertyChanged(nameof(CanStartLending));
        OnPropertyChanged(nameof(ShowMissingSelectedBookLending));
        RefreshHomeSummary();
    }

    private void MakeBookAvailableIfNoActiveLending(string inventoryCode)
    {
        if (Lendings.Any(item => item.InventoryCode == inventoryCode && item.ReturnedAt is null))
            return;

        var index = Books.ToList().FindIndex(book => book.InventoryCode == inventoryCode);
        if (index < 0 || Books[index].Status != "Kölcsönözve")
            return;

        var availableBook = Books[index] with { Status = "Elérhető" };
        Books[index] = availableBook;
        if (SelectedBook?.InventoryCode == inventoryCode)
            SelectedBook = availableBook;
        RefreshStatusFilters();
        RefreshFilteredBooks();
    }

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
        RefreshHomeSummary();
    }

    public int TotalBooksCount => Books.Count;

    public int AvailableBooksCount => Books.Count(book => book.Status == "Elérhető");

    public int LentBooksCount => Books.Count(book => book.Status == "Kölcsönözve");

    public int LibraryUtilizationPercent => TotalBooksCount == 0
        ? 0
        : (int)Math.Round(LentBooksCount * 100d / TotalBooksCount);

    public int UsersCount => Users.Count;

    public int TotalLendingsCount => Lendings.Count;

    public int ActiveLendingsCount => Lendings.Count(lending => lending.ReturnedAt is null);

    public int OverdueLendingsCount => Lendings.Count(lending =>
        lending.ReturnedAt is null && lending.DueDate.Date < DateTimeOffset.Now.Date);

    public int DueTodayLendingsCount => Lendings.Count(lending =>
        lending.ReturnedAt is null && lending.DueDate.Date == DateTimeOffset.Now.Date);

    public int DueSoonLendingsCount => Lendings.Count(lending =>
        lending.ReturnedAt is null &&
        lending.DueDate.Date >= DateTimeOffset.Now.Date &&
        lending.DueDate.Date <= DateTimeOffset.Now.Date.AddDays(7));

    public int ReturnedLendingsCount => Lendings.Count(lending => lending.ReturnedAt is not null);

    public IReadOnlyList<Book> FeaturedBooks => Books.Take(3).ToList();

    public IReadOnlyList<LendingRecord> NextDueLendings => Lendings
        .Where(lending => lending.ReturnedAt is null)
        .OrderBy(lending => lending.DueDate)
        .Take(3)
        .ToList();

    public bool HasFeaturedBooks => Books.Count > 0;

    public bool HasNextDueLendings => Lendings.Any(lending => lending.ReturnedAt is null);

    private static readonly GridLength FullTableWidth = new(1, GridUnitType.Star);
    private static readonly GridLength SplitTableWidth = new(3, GridUnitType.Star);
    private static readonly GridLength DetailsWidth = new(1.5, GridUnitType.Star);

    public GridLength BookTableColumnWidth => HasSelectedBook ? SplitTableWidth : FullTableWidth;

    public GridLength BookDetailsColumnWidth => HasSelectedBook ? DetailsWidth : new GridLength(0);

    public GridLength LendingTableColumnWidth => HasSelectedLending ? SplitTableWidth : FullTableWidth;

    public GridLength LendingDetailsColumnWidth => HasSelectedLending ? DetailsWidth : new GridLength(0);

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
        Books.CollectionChanged += (_, _) => RefreshHomeSummary();
        Users.CollectionChanged += (_, _) => RefreshHomeSummary();
        Lendings.CollectionChanged += (_, _) =>
        {
            RefreshHomeSummary();
            OnPropertyChanged(nameof(SelectedBookLending));
            OnPropertyChanged(nameof(HasSelectedBookLending));
            OnPropertyChanged(nameof(ShowMissingSelectedBookLending));
        };
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
            var lentBook = SelectedBook with { Status = "Kölcsönözve" };
            var bookIndex = Books.IndexOf(SelectedBook);
            if (bookIndex >= 0)
            {
                Books[bookIndex] = lentBook;
                SelectedBook = lentBook;
                RefreshStatusFilters();
                RefreshFilteredBooks();
                OnPropertyChanged(nameof(SelectedBookLending));
                OnPropertyChanged(nameof(HasSelectedBookLending));
                OnPropertyChanged(nameof(ShowMissingSelectedBookLending));
                OnPropertyChanged(nameof(IsSelectedBookLent));
                OnPropertyChanged(nameof(CanStartLending));
            }
            RefreshHomeSummary();
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

    private void RefreshHomeSummary()
    {
        OnPropertyChanged(nameof(TotalBooksCount));
        OnPropertyChanged(nameof(AvailableBooksCount));
        OnPropertyChanged(nameof(LentBooksCount));
        OnPropertyChanged(nameof(LibraryUtilizationPercent));
        OnPropertyChanged(nameof(UsersCount));
        OnPropertyChanged(nameof(TotalLendingsCount));
        OnPropertyChanged(nameof(ActiveLendingsCount));
        OnPropertyChanged(nameof(OverdueLendingsCount));
        OnPropertyChanged(nameof(DueTodayLendingsCount));
        OnPropertyChanged(nameof(DueSoonLendingsCount));
        OnPropertyChanged(nameof(ReturnedLendingsCount));
        OnPropertyChanged(nameof(FeaturedBooks));
        OnPropertyChanged(nameof(NextDueLendings));
        OnPropertyChanged(nameof(HasFeaturedBooks));
        OnPropertyChanged(nameof(HasNextDueLendings));
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
