using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KönyvTár.Models;
using KönyvTár.Services.Api;

namespace KönyvTár.Services;

public sealed class ApiLendingService : ILendingService
{
    private readonly ApiClient _client;

    public ApiLendingService(ApiClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<LendingRecord>> GetLendingsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        var lendings = await _client.GetAllAsync<ApiBorrowingDto>(
            "borrowings",
            cancellationToken);

        IReadOnlyList<ApiBookDto> books;
        try
        {
            books = await _client.GetAllAsync<ApiBookDto>("books", cancellationToken);
        }
        catch
        {
            books = Array.Empty<ApiBookDto>();
        }

        var booksByIsbn = books
            .Where(book => !string.IsNullOrWhiteSpace(book.Isbn))
            .GroupBy(book => book.Isbn!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        return lendings
            .Where(lending => !string.IsNullOrWhiteSpace(lending.Id))
            .Select(lending =>
            {
                var isbn = lending.BookCopy?.Isbn ?? lending.Book?.Isbn;
                booksByIsbn.TryGetValue(isbn ?? string.Empty, out var metadata);
                return ApiMapping.ToLending(lending, now, metadata);
            })
            .ToList();
    }

    public async Task<LendingRecord> CreateLendingAsync(
        LendingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.User.Id))
            throw new ApiException("A kiválasztott olvasó nem azonosítható.");

        var borrowing = await _client.PostAsync<ApiBorrowingDto>(
            "borrowings",
            new Dictionary<string, object?>
            {
                ["inventory_code"] = request.Book.InventoryCode,
                ["user_id"] = request.User.Id,
                ["borrowed_at"] = request.From.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["due_date"] = request.To.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["notify_by_email"] = true,
            },
            cancellationToken);

        return ApiMapping.ToLending(borrowing, DateTimeOffset.Now) with
        {
            Title = request.Book.Title,
            Author = request.Book.Author,
            Publisher = request.Book.Publisher,
        };
    }

    public async Task<LendingRecord> UpdateLendingAsync(
        LendingRecord lending,
        CancellationToken cancellationToken = default)
    {
        var borrowing = await _client.PatchAsync<ApiBorrowingDto>(
            $"borrowings/{Uri.EscapeDataString(lending.Id)}",
            new Dictionary<string, object?>
            {
                ["due_date"] = lending.DueDate.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["notify_by_email"] = lending.NotifyByEmail,
            },
            cancellationToken);

        return ApiMapping.ToLending(borrowing, DateTimeOffset.Now) with
        {
            Title = lending.Title,
            Author = lending.Author,
            Publisher = lending.Publisher,
        };
    }

    public async Task<LendingRecord> ReturnLendingAsync(
        LendingRecord lending,
        CancellationToken cancellationToken = default)
    {
        await _client.PostAsync<ApiBorrowingDto>(
            $"borrowings/{Uri.EscapeDataString(lending.Id)}/return",
            new Dictionary<string, object?>
            {
                ["returned_at"] = DateTimeOffset.Now.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            },
            cancellationToken);

        // The return endpoint does not load the related reader, library and librarian,
        // so read the complete record before mapping it for the UI.
        var completeBorrowing = await _client.GetAsync<ApiBorrowingDto>(
            $"borrowings/{Uri.EscapeDataString(lending.Id)}",
            cancellationToken);

        return ApiMapping.ToLending(completeBorrowing, DateTimeOffset.Now) with
        {
            Title = lending.Title,
            Author = lending.Author,
            Publisher = lending.Publisher,
        };
    }

    public Task DeleteLendingAsync(
        string lendingId,
        CancellationToken cancellationToken = default) =>
        _client.DeleteAsync($"borrowings/{Uri.EscapeDataString(lendingId)}", cancellationToken);
}
