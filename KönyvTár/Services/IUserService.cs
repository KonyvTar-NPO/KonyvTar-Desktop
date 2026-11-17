using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KönyvTár.Models;

namespace KönyvTár.Services;

public interface IUserService
{
    Task<IReadOnlyList<LibraryUser>> GetUsersAsync(CancellationToken cancellationToken = default);
}
