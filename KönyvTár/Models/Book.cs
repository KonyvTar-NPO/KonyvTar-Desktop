namespace KönyvTár.Models;

public sealed record Book(
    string Title,
    string Author,
    string Publisher,
    string Status,
    string ISBN,
    string InventoryCode);
