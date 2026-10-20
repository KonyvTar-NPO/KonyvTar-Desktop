using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KönyvTár.Models;

namespace KönyvTár.Services;

public sealed class TemporaryBookService : IBookService
{
    private static readonly IReadOnlyList<Book> Books =
    [
        new("A Pál utcai fiúk", "Molnár Ferenc", "Móra Könyvkiadó", "Elérhető", "9789634157273", "KVT-0001"),
        new("Egri csillagok", "Gárdonyi Géza", "Móra Könyvkiadó", "Kölcsönözve", "9789634157280", "KVT-0002"),
        new("Harry Potter és a bölcsek köve", "J. K. Rowling", "Animus Kiadó", "Elérhető", "9789634572182", "KVT-0003"),
        new("A kis herceg", "Antoine de Saint-Exupéry", "Móra Könyvkiadó", "Elérhető", "9789634157297", "KVT-0004"),
        new("1984", "George Orwell", "Európa Könyvkiadó", "Kölcsönözve", "9789635044329", "KVT-0005")
    ];

    public Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Books);
    }
}
