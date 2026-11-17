using System.Collections.Generic;
using System.Linq;

namespace KönyvTár.Models;

public sealed record LibraryUser(
    string Name,
    string Email,
    string? Id = null,
    IReadOnlyList<string>? LibraryIds = null)
{
    public override string ToString() => $"{Name} ({Email})";

    public bool IsMemberOf(string? libraryId) =>
        string.IsNullOrWhiteSpace(libraryId)
        || LibraryIds is null
        || LibraryIds.Contains(libraryId);
}
