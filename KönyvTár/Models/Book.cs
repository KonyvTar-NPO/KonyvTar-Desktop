namespace KönyvTár.Models;

public sealed record Book(
    string Title,
    string Author,
    string Publisher,
    string Status,
    string ISBN,
    string InventoryCode,
    string? AuthorId = null,
    string? PublisherId = null,
    string? LibraryId = null,
    string? LibraryName = null);
