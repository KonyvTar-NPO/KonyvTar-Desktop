using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KönyvTár.Models;

namespace KönyvTár.Services;

public interface ILendingService
{
    Task<IReadOnlyList<LendingRecord>> GetLendingsAsync(CancellationToken cancellationToken = default);
    Task CreateLendingAsync(LendingRequest request, CancellationToken cancellationToken = default);
    Task UpdateLendingAsync(LendingRecord lending, CancellationToken cancellationToken = default);
    Task DeleteLendingAsync(string lendingId, CancellationToken cancellationToken = default);
}
