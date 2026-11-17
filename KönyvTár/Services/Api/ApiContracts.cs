using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KönyvTár.Models;

namespace KönyvTár.Services.Api;

public sealed record ApiLibrary(
    string Id,
    string Name,
    string Address);

public sealed record ApiUser(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Role,
    IReadOnlyList<ApiLibrary> ManagedLibraries)
{
    public string FullName
    {
        get
        {
            var name = $"{FirstName} {LastName}".Trim();
            return name.Length > 0 ? name : Email;
        }
    }

    public bool IsAdmin => string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);

    public bool IsLibrarian => string.Equals(Role, "librarian", StringComparison.OrdinalIgnoreCase);

    public bool IsStaff => IsAdmin || IsLibrarian;
}

internal sealed class ApiPage<T>
{
    public List<T> Data { get; set; } = [];

    public ApiMeta? Meta { get; set; }
}

internal sealed class ApiMeta
{
    public int CurrentPage { get; set; }

    public int LastPage { get; set; }
}

internal sealed class ApiDataEnvelope<T>
{
    public T? Data { get; set; }
}

internal sealed class ApiLoginEnvelope
{
    public string? Token { get; set; }

    public ApiUserDto? User { get; set; }
}

internal sealed class ApiUserDto
{
    public string? Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Role { get; set; }

    public string? Status { get; set; }

    public List<ApiLibraryDto>? ManagedLibraries { get; set; }
}

internal sealed class ApiLibraryDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }
}

internal sealed class ApiAuthorDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }
}

internal sealed class ApiPublisherDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }
}

internal sealed class ApiBookDto
{
    public string? Isbn { get; set; }

    public string? Title { get; set; }

    public string? AuthorId { get; set; }

    public string? PublisherId { get; set; }

    public int? PublicationYear { get; set; }

    public string? Genre { get; set; }

    public ApiAuthorDto? Author { get; set; }

    public ApiPublisherDto? Publisher { get; set; }
}

internal sealed class ApiBookCopyDto
{
    public string? InventoryCode { get; set; }

    public string? Isbn { get; set; }

    public string? LibraryId { get; set; }

    public string? BookCondition { get; set; }

    public string? BookStatus { get; set; }

    public ApiBookDto? Book { get; set; }

    public ApiLibraryDto? Library { get; set; }
}

internal sealed class ApiMembershipDto
{
    public string? Id { get; set; }

    public string? UserId { get; set; }

    public string? LibraryId { get; set; }

    public string? Status { get; set; }

    public ApiUserDto? User { get; set; }

    public ApiLibraryDto? Library { get; set; }
}

internal sealed class ApiBorrowingDto
{
    public string? Id { get; set; }

    public string? InventoryCode { get; set; }

    public string? UserId { get; set; }

    public string? LibraryId { get; set; }

    public string? LibrarianId { get; set; }

    public string? BorrowedAt { get; set; }

    public string? DueDate { get; set; }

    public string? ReturnedAt { get; set; }

    public string? Status { get; set; }

    public bool NotifyByEmail { get; set; }

    public bool IsOverdue { get; set; }

    public ApiUserDto? User { get; set; }

    public ApiLibraryDto? Library { get; set; }

    public ApiUserDto? Librarian { get; set; }

    public ApiBookCopyDto? BookCopy { get; set; }

    public ApiBookDto? Book { get; set; }
}

internal static class ApiMapping
{
    public static ApiUser ToUser(ApiUserDto dto)
    {
        var managedLibraries = dto.ManagedLibraries?
            .Where(library => !string.IsNullOrWhiteSpace(library.Id))
            .Select(library => new ApiLibrary(
                library.Id!,
                library.Name ?? string.Empty,
                library.Address ?? string.Empty))
            .ToList() ?? [];

        return new ApiUser(
            dto.Id ?? string.Empty,
            dto.FirstName ?? string.Empty,
            dto.LastName ?? string.Empty,
            dto.Email ?? string.Empty,
            dto.PhoneNumber ?? string.Empty,
            dto.Role ?? string.Empty,
            managedLibraries);
    }

    public static Book ToBook(ApiBookCopyDto copy, ApiBookDto? metadata)
    {
        var book = metadata ?? copy.Book;

        return new Book(
            book?.Title ?? copy.Isbn ?? string.Empty,
            book?.Author?.Name ?? string.Empty,
            book?.Publisher?.Name ?? string.Empty,
            BookStatusLabel(copy.BookStatus),
            book?.Isbn ?? copy.Isbn ?? string.Empty,
            copy.InventoryCode ?? string.Empty,
            book?.AuthorId,
            book?.PublisherId,
            copy.LibraryId,
            copy.Library?.Name);
    }

    public static LendingRecord ToLending(ApiBorrowingDto dto, DateTimeOffset now, ApiBookDto? metadata = null)
    {
        var book = metadata ?? dto.Book ?? dto.BookCopy?.Book;
        var userName = dto.User?.FullName?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
            userName = dto.User?.Email ?? "Ismeretlen olvasó";

        var librarianName = dto.Librarian?.FullName?.Trim();
        if (string.IsNullOrWhiteSpace(librarianName))
            librarianName = "—";

        return new LendingRecord(
            dto.Id ?? string.Empty,
            dto.InventoryCode ?? string.Empty,
            userName,
            dto.Library?.Name ?? string.Empty,
            librarianName,
            ParseDate(dto.BorrowedAt, now.Date),
            ParseDate(dto.DueDate, now.Date),
            ParseNullableDate(dto.ReturnedAt),
            LendingStatusLabel(dto.Status, dto.IsOverdue),
            dto.NotifyByEmail,
            book?.Title ?? string.Empty,
            book?.Author?.Name ?? string.Empty,
            book?.Publisher?.Name ?? string.Empty);
    }

    public static string BookStatusLabel(string? status) => status switch
    {
        "available" => "Elérhető",
        "borrowed" => "Kölcsönözve",
        "unavailable" => "Nem elérhető",
        null or "" => "Ismeretlen",
        _ => status,
    };

    public static string LendingStatusLabel(string? status, bool isOverdue) => status switch
    {
        "active" when isOverdue => "Lejárt",
        "active" => "Aktív",
        "overdue" => "Lejárt",
        "returned" => "Visszahozva",
        "lost" => "Elveszett",
        null or "" => "Ismeretlen",
        _ => status,
    };

    private static DateTimeOffset ParseDate(string? value, DateTimeOffset fallback) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out var parsed)
            ? parsed
            : fallback;

    private static DateTimeOffset? ParseNullableDate(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out var parsed)
            ? parsed
            : null;
}
