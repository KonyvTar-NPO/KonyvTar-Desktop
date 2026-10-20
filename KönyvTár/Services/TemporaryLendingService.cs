using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using KönyvTár.Models;

namespace KönyvTár.Services;

public sealed class TemporaryLendingService : ILendingService
{
    private static readonly List<LendingRecord> Lendings =
    [
        new("KOL-0001", "KVT-0002", "Kovács Anna", "Központi könyvtár", "Szlonkai Benedek",
            DateTimeOffset.Now.Date.AddDays(-8), DateTimeOffset.Now.Date.AddDays(6), null, "Aktív", true),
        new("KOL-0002", "KVT-0005", "Nagy Bence", "Központi könyvtár", "Szlonkai Benedek",
            DateTimeOffset.Now.Date.AddDays(-30), DateTimeOffset.Now.Date.AddDays(-2), null, "Lejárt", true),
        new("KOL-0003", "KVT-0001", "Szabó Dóra", "Központi könyvtár", "Szlonkai Benedek",
            DateTimeOffset.Now.Date.AddDays(-60), DateTimeOffset.Now.Date.AddDays(-46), DateTimeOffset.Now.Date.AddDays(-48), "Visszahozva", false)
    ];

    public Task<IReadOnlyList<LendingRecord>> GetLendingsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<LendingRecord>>(Lendings.ToList());
    }

    public Task CreateLendingAsync(LendingRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Lendings.Add(new LendingRecord(
            $"KOL-{Lendings.Count + 1:0000}", request.Book.InventoryCode, request.User.Name,
            "Központi könyvtár", "Szlonkai Benedek", request.From, request.To, null, "Aktív", true));
        return Task.CompletedTask;
    }
}
