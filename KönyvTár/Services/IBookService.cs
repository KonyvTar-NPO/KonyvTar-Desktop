using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KönyvTár.Models;

namespace KönyvTár.Services;

public interface IBookService
{
    Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken cancellationToken = default);
}
