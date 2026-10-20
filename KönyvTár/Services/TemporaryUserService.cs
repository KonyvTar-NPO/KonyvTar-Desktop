using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KönyvTár.Models;

namespace KönyvTár.Services;

public sealed class TemporaryUserService : IUserService
{
    private static readonly IReadOnlyList<LibraryUser> Users =
    [
        new("Kovács Anna", "anna.kovacs@example.com"),
        new("Nagy Bence", "bence.nagy@example.com"),
        new("Szabó Dóra", "dora.szabo@example.com")
    ];

    public Task<IReadOnlyList<LibraryUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Users);
    }
}
