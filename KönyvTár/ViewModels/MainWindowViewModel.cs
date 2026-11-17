using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KönyvTár.Models;
using KönyvTár.Notifications;
using KönyvTár.Services;
using KönyvTár.Services.Api;

namespace KönyvTár.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IBookService _bookService;
    private readonly IUserService _userService;
    private readonly ILendingService _lendingService;
    private ApiUser? _currentUser;
    private string _searchText = string.Empty;
    private string _statusFilter = "Minden";
    private Book? _selectedBook;
    private LibraryUser? _selectedUser;
    private string _selectedUserSearchText = string.Empty;
    private DateTimeOffset? _fromDate = DateTimeOffset.Now.Date;
    private DateTimeOffset? _toDate = DateTimeOffset.Now.Date.AddDays(1);
    private string _selectedLendingDuration = "1 nap";
    private string _lendingStatusMessage = string.Empty;
    private string _loadErrorMessage = string.Empty;
    private bool _isLoading;
    private string _lendingSearchText = string.Empty;
    private string _lendingStatusFilter = "Minden";
    private LendingRecord? _selectedLending;

    public ObservableCollection<Book> Books { get; } = [];
    public ObservableCollection<Book> FilteredBooks { get; } = [];
    public ObservableCollection<string> StatusFilters { get; } = ["Minden"];
    public ObservableCollection<LibraryUser> Users { get; } = [];
    public ObservableCollection<LibraryUser> AvailableUsers { get; } = [];
    public ObservableCollection<LendingRecord> Lendings { get; } = [];
    public ObservableCollection<LendingRecord> FilteredLendings { get; } = [];
    public ObservableCollection<string> LendingStatusFilters { get; } = ["Minden"];
    public ObservableCollection<string> LendingDurationOptions { get; } = ["1 nap", "1 hét", "1 hónap", "Egyedi"];

    public string CurrentUserName => _currentUser?.FullName ?? "Könyvtáros";

    public string CurrentUserEmail => _currentUser?.Email ?? string.Empty;

    public string CurrentUserRoleLabel => _currentUser?.IsAdmin == true
        ? "Rendszergazda"
        : _currentUser?.IsLibrarian == true
            ? "Könyvtáros"
            : string.Empty;

    public void ApplyCurrentUser(ApiUser user)
    {
        _currentUser = user;
        OnPropertyChanged(nameof(CurrentUserName));
        OnPropertyChanged(nameof(CurrentUserEmail));
        OnPropertyChanged(nameof(CurrentUserRoleLabel));
        OnPropertyChanged(nameof(CanDeleteLending));
    }

    public bool CanDeleteLending => _currentUser?.IsAdmin == true;

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string LoadErrorMessage
    {
        get => _loadErrorMessage;
        private set
        {
            if (SetProperty(ref _loadErrorMessage, value))
                OnPropertyChanged(nameof(HasLoadError));
        }
    }

    public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

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
        private set
        {
            if (SetProperty(ref _lendingStatusMessage, value))
                OnPropertyChanged(nameof(HasLendingStatusMessage));
        }
    }

    public bool HasLendingStatusMessage => !string.IsNullOrWhiteSpace(LendingStatusMessage);

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
        set
        {
            if (SetProperty(ref _selectedLending, value))
            {
                OnPropertyChanged(nameof(HasSelectedLending));
                OnPropertyChanged(nameof(LendingTableColumnWidth));
                OnPropertyChanged(nameof(LendingDetailsColumnWidth));
                OnPropertyChanged(nameof(CanReturnSelectedLending));
            }
        }
    }

    public bool HasSelectedLending => SelectedLending is not null;

    public bool CanReturnSelectedLending => SelectedLending is { Status: "Aktív" or "Lejárt" };

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

                RefreshAvailableUsers();

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
        ? Lendings.FirstOrDefault(lending =>
            lending.InventoryCode == book.InventoryCode && IsOpenLending(lending))
        : null;

    public bool HasSelectedBookLending => SelectedBookLending is not null;

    public bool ShowMissingSelectedBookLending => IsSelectedBookLent && !HasSelectedBookLending;

    public bool IsSelectedBookLent => SelectedBook?.Status == "Kölcsönözve";

    public bool CanStartLending =>
        SelectedBook is { Status: "Elérhető" } && AvailableUsers.Count > 0;

    public async Task UpdateLendingAsync(LendingRecord updatedLending)
    {
        var index = Lendings.ToList().FindIndex(lending => lending.Id == updatedLending.Id);
        if (index < 0)
            return;

        try
        {
            var savedLending = await _lendingService.UpdateLendingAsync(updatedLending);
            Lendings[index] = savedLending;
            if (SelectedLending?.Id == savedLending.Id)
                SelectedLending = savedLending;

            RefreshFilteredLendings();
            NotifySelectedBookLendingChanged();
            const string message = "A kölcsönzés adatai mentve.";
            LendingStatusMessage = message;
            ToastService.Instance.ShowSuccess(message);
        }
        catch (Exception exception)
        {
            var message = DescribeError(
                exception,
                "Nem sikerült módosítani a kölcsönzést.");
            LendingStatusMessage = message;
            ToastService.Instance.ShowError(message);
        }

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

        if (!CanDeleteLending)
        {
            const string message = "Könyvtárosként a kölcsönzések törlése nem engedélyezett.";
            LendingStatusMessage = message;
            ToastService.Instance.ShowWarning(message);
            return;
        }

        try
        {
            await _lendingService.DeleteLendingAsync(lending.Id);
            Lendings.Remove(Lendings.First(item => item.Id == lending.Id));
            MakeBookAvailableIfNoActiveLending(lending.InventoryCode);

            if (SelectedLending?.Id == lending.Id)
                SelectedLending = null;

            RefreshFilteredLendings();
            NotifySelectedBookLendingChanged();
            OnPropertyChanged(nameof(IsSelectedBookLent));
            OnPropertyChanged(nameof(CanStartLending));
            const string message = "A kölcsönzés törölve.";
            LendingStatusMessage = message;
            ToastService.Instance.ShowSuccess(message);
        }
        catch (Exception exception)
        {
            var message = DescribeError(
                exception,
                "Nem sikerült törölni a kölcsönzést.");
            LendingStatusMessage = message;
            ToastService.Instance.ShowError(message);
        }

        RefreshHomeSummary();
    }

    public async Task ReturnSelectedBookLendingAsync()
    {
        if (SelectedBookLending is { } lending)
            await ReturnLendingAsync(lending);
    }

    public async Task ReturnLendingAsync(LendingRecord lending)
    {
        if (!Lendings.Any(item => item.Id == lending.Id))
            return;

        if (!CanReturnSelectedLendingFor(lending))
        {
            const string message = "Csak aktív vagy lejárt kölcsönzés hozható vissza.";
            LendingStatusMessage = message;
            ToastService.Instance.ShowWarning(message);
            return;
        }

        try
        {
            var returnedLending = await _lendingService.ReturnLendingAsync(lending);
            var lendingIndex = Lendings.ToList().FindIndex(item => item.Id == lending.Id);
            if (lendingIndex >= 0)
                Lendings[lendingIndex] = returnedLending;

            if (SelectedLending?.Id == lending.Id)
                SelectedLending = returnedLending;

            MakeBookAvailableIfNoActiveLending(returnedLending.InventoryCode);
            RefreshFilteredLendings();
            NotifySelectedBookLendingChanged();
            OnPropertyChanged(nameof(IsSelectedBookLent));
            OnPropertyChanged(nameof(CanStartLending));
            const string message = "A könyv visszahozatala rögzítve.";
            LendingStatusMessage = message;
            ToastService.Instance.ShowSuccess(message);
        }
        catch (Exception exception)
        {
            var message = DescribeError(
                exception,
                "Nem sikerült rögzíteni a visszahozatalt.");
            LendingStatusMessage = message;
            ToastService.Instance.ShowError(message);
        }

        RefreshHomeSummary();
    }

    public async Task UpdateBookAsync(Book originalBook, Book updatedBook)
    {
        try
        {
            var savedBook = await _bookService.UpdateBookAsync(originalBook, updatedBook);
            var index = Books
                .Select((book, bookIndex) => new { book, bookIndex })
                .FirstOrDefault(item => item.book.InventoryCode == originalBook.InventoryCode)
                ?.bookIndex;

            if (index is null)
                return;

            Books[index.Value] = savedBook;
            if (SelectedBook?.InventoryCode == originalBook.InventoryCode)
                SelectedBook = savedBook;

            RefreshStatusFilters();
            RefreshFilteredBooks();
            RefreshHomeSummary();
            const string message = "A könyv adatai mentve.";
            LendingStatusMessage = message;
            ToastService.Instance.ShowSuccess(message);
        }
        catch (Exception exception)
        {
            var message = DescribeError(
                exception,
                "Nem sikerült menteni a könyv adatait.");
            LendingStatusMessage = message;
            ToastService.Instance.ShowError(message);
        }
    }

    private void MakeBookAvailableIfNoActiveLending(string inventoryCode)
    {
        if (Lendings.Any(item => item.InventoryCode == inventoryCode && IsOpenLending(item)))
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

    public int TotalBooksCount => Books.Count;

    public int AvailableBooksCount => Books.Count(book => book.Status == "Elérhető");

    public int LentBooksCount => Books.Count(book => book.Status == "Kölcsönözve");

    public int LibraryUtilizationPercent => TotalBooksCount == 0
        ? 0
        : (int)Math.Round(LentBooksCount * 100d / TotalBooksCount);

    public int UsersCount => Users.Count;

    public int TotalLendingsCount => Lendings.Count;

    public int ActiveLendingsCount => Lendings.Count(IsOpenLending);

    public int OverdueLendingsCount => Lendings.Count(lending => lending.Status == "Lejárt");

    public int DueTodayLendingsCount => Lendings.Count(lending =>
        IsOpenLending(lending) && lending.DueDate.Date == DateTimeOffset.Now.Date);

    public int DueSoonLendingsCount => Lendings.Count(lending =>
        IsOpenLending(lending) &&
        lending.DueDate.Date >= DateTimeOffset.Now.Date &&
        lending.DueDate.Date <= DateTimeOffset.Now.Date.AddDays(7));

    public int ReturnedLendingsCount => Lendings.Count(lending => lending.Status == "Visszahozva");

    public IReadOnlyList<Book> FeaturedBooks => Books.Take(3).ToList();

    public IReadOnlyList<LendingRecord> NextDueLendings => Lendings
        .Where(IsOpenLending)
        .OrderBy(lending => lending.DueDate)
        .Take(3)
        .ToList();

    public bool HasFeaturedBooks => Books.Count > 0;

    public bool HasNextDueLendings => Lendings.Any(IsOpenLending);

    private static readonly GridLength FullTableWidth = new(1, GridUnitType.Star);
    private static readonly GridLength SplitTableWidth = new(3, GridUnitType.Star);
    private static readonly GridLength DetailsWidth = new(1.5, GridUnitType.Star);

    public GridLength BookTableColumnWidth => HasSelectedBook ? SplitTableWidth : FullTableWidth;

    public GridLength BookDetailsColumnWidth => HasSelectedBook ? DetailsWidth : new GridLength(0);

    public GridLength LendingTableColumnWidth => HasSelectedLending ? SplitTableWidth : FullTableWidth;

    public GridLength LendingDetailsColumnWidth => HasSelectedLending ? DetailsWidth : new GridLength(0);

    public MainWindowViewModel()
        : this(new ApiClient())
    {
    }

    public MainWindowViewModel(ApiClient apiClient)
        : this(
            new ApiBookService(apiClient),
            new ApiUserService(apiClient),
            new ApiLendingService(apiClient),
            apiClient.CurrentUser)
    {
    }

    public MainWindowViewModel(
        IBookService bookService,
        IUserService userService,
        ILendingService lendingService,
        ApiUser? currentUser = null)
    {
        _bookService = bookService;
        _userService = userService;
        _lendingService = lendingService;
        _currentUser = currentUser;

        Books.CollectionChanged += (_, _) => RefreshHomeSummary();
        Users.CollectionChanged += (_, _) =>
        {
            RefreshAvailableUsers();
            RefreshHomeSummary();
        };
        Lendings.CollectionChanged += (_, _) =>
        {
            RefreshHomeSummary();
            NotifySelectedBookLendingChanged();
        };

        if (!Design.IsDesignMode)
            _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        var errors = new List<string>();

        var booksTask = _bookService.GetBooksAsync();
        var usersTask = _userService.GetUsersAsync();
        var lendingsTask = _lendingService.GetLendingsAsync();

        var books = await LoadOrEmptyAsync(
            booksTask,
            "Nem sikerült betölteni a könyvállományt.",
            errors);
        var users = await LoadOrEmptyAsync(
            usersTask,
            "Nem sikerült betölteni az olvasókat.",
            errors);
        var lendings = await LoadOrEmptyAsync(
            lendingsTask,
            "Nem sikerült betölteni a kölcsönzéseket.",
            errors);

        Books.Clear();
        foreach (var book in books)
            Books.Add(book);

        Users.Clear();
        foreach (var user in users)
            Users.Add(user);

        Lendings.Clear();
        foreach (var lending in lendings)
            Lendings.Add(lending);

        LoadErrorMessage = string.Join(" ", errors);
        if (HasLoadError)
            ToastService.Instance.ShowError(LoadErrorMessage);
        RefreshStatusFilters();

        LendingStatusFilters.Clear();
        LendingStatusFilters.Add("Minden");
        foreach (var status in Lendings.Select(lending => lending.Status).Distinct())
            LendingStatusFilters.Add(status);

        RefreshFilteredBooks();
        RefreshFilteredLendings();
        RefreshHomeSummary();
        IsLoading = false;
    }

    private static async Task<IReadOnlyList<T>> LoadOrEmptyAsync<T>(
        Task<IReadOnlyList<T>> task,
        string errorMessage,
        ICollection<string> errors)
    {
        try
        {
            return await task;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            errors.Add(DescribeError(exception, errorMessage));
            return Array.Empty<T>();
        }
    }

    private void ShowLendingWarning(string message)
    {
        LendingStatusMessage = message;
        ToastService.Instance.ShowWarning(message);
    }

    [RelayCommand]
    private async Task InitiateLendingAsync()
    {
        if (SelectedBook is null || SelectedUser is null || FromDate is null || ToDate is null)
        {
            ShowLendingWarning("Válasszon könyvet, felhasználót és dátumtartományt.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedUser.Id))
        {
            ShowLendingWarning("A kiválasztott olvasó nem azonosítható.");
            return;
        }

        if (!SelectedUser.IsMemberOf(SelectedBook.LibraryId))
        {
            ShowLendingWarning("A kiválasztott olvasó nem tagja ennek a könyvtárnak.");
            return;
        }

        if (ToDate < FromDate)
        {
            ShowLendingWarning("A visszahozatali dátum nem lehet korábbi a kölcsönzés dátumánál.");
            return;
        }

        try
        {
            var lending = await _lendingService.CreateLendingAsync(
                new LendingRequest(SelectedBook, SelectedUser, FromDate.Value, ToDate.Value));

            Lendings.Add(lending);
            MarkBookAsLent(lending.InventoryCode);
            RefreshFilteredLendings();
            NotifySelectedBookLendingChanged();
            OnPropertyChanged(nameof(IsSelectedBookLent));
            OnPropertyChanged(nameof(CanStartLending));
            RefreshHomeSummary();
            const string message = "Kölcsönzés sikeresen létrehozva.";
            LendingStatusMessage = message;
            ToastService.Instance.ShowSuccess(message);
        }
        catch (Exception exception)
        {
            var message = DescribeError(
                exception,
                "A kölcsönzés létrehozása sikertelen.");
            LendingStatusMessage = message;
            ToastService.Instance.ShowError(message);
        }
    }

    private void MarkBookAsLent(string inventoryCode)
    {
        var index = Books.ToList().FindIndex(book => book.InventoryCode == inventoryCode);
        if (index < 0 || Books[index].Status == "Kölcsönözve")
            return;

        var lentBook = Books[index] with { Status = "Kölcsönözve" };
        Books[index] = lentBook;
        if (SelectedBook?.InventoryCode == inventoryCode)
            SelectedBook = lentBook;

        RefreshStatusFilters();
        RefreshFilteredBooks();
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

    private void RefreshAvailableUsers()
    {
        var libraryId = SelectedBook?.LibraryId;
        var users = string.IsNullOrWhiteSpace(libraryId)
            ? Users.ToList()
            : Users.Where(user => user.IsMemberOf(libraryId)).ToList();

        AvailableUsers.Clear();
        foreach (var user in users)
            AvailableUsers.Add(user);

        if (SelectedUser is not null &&
            !AvailableUsers.Any(user => user.Id == SelectedUser.Id))
        {
            SelectedUser = null;
            SelectedUserSearchText = string.Empty;
        }

        OnPropertyChanged(nameof(CanStartLending));
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

    private void NotifySelectedBookLendingChanged()
    {
        OnPropertyChanged(nameof(SelectedBookLending));
        OnPropertyChanged(nameof(HasSelectedBookLending));
        OnPropertyChanged(nameof(ShowMissingSelectedBookLending));
    }

    private void RefreshFilteredLendings()
    {
        var search = LendingSearchText.Trim();
        var matches = Lendings.Where(lending =>
            (LendingStatusFilter == "Minden" || lending.Status == LendingStatusFilter) &&
            (search.Length == 0 || lending.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             lending.InventoryCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             lending.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             lending.Author.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             lending.Publisher.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             lending.UserName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             lending.Status.Contains(search, StringComparison.OrdinalIgnoreCase)));

        FilteredLendings.Clear();
        foreach (var lending in matches)
            FilteredLendings.Add(lending);

        if (SelectedLending is not null && !FilteredLendings.Contains(SelectedLending))
            SelectedLending = null;
    }

    private static bool IsOpenLending(LendingRecord lending) =>
        lending.Status is "Aktív" or "Lejárt";

    private static bool CanReturnSelectedLendingFor(LendingRecord lending) =>
        lending.Status is "Aktív" or "Lejárt";

    private static string DescribeError(Exception exception, string fallbackMessage) =>
        exception is ApiException apiException ? apiException.Message : fallbackMessage;
}
