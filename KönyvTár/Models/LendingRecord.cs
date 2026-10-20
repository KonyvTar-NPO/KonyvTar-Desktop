using System;

namespace KönyvTár.Models;

public sealed record LendingRecord(
    string Id,
    string InventoryCode,
    string UserName,
    string LibraryName,
    string LibrarianName,
    DateTimeOffset BorrowedAt,
    DateTimeOffset DueDate,
    DateTimeOffset? ReturnedAt,
    string Status,
    bool NotifyByEmail);
