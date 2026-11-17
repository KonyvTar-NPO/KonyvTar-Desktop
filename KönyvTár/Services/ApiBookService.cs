using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KönyvTár.Models;
using KönyvTár.Services.Api;

namespace KönyvTár.Services;

public sealed class ApiBookService : IBookService
{
    private readonly ApiClient _client;

    public ApiBookService(ApiClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        var booksTask = _client.GetAllAsync<ApiBookDto>("books", cancellationToken);
        var copiesTask = GetBookCopiesAsync(cancellationToken);

        await Task.WhenAll(booksTask, copiesTask);

        var metadataByIsbn = (await booksTask)
            .Where(book => !string.IsNullOrWhiteSpace(book.Isbn))
            .GroupBy(book => book.Isbn!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        return (await copiesTask)
            .Where(copy => !string.IsNullOrWhiteSpace(copy.InventoryCode))
            .Select(copy => ApiMapping.ToBook(
                copy,
                copy.Isbn is not null && metadataByIsbn.TryGetValue(copy.Isbn, out var metadata)
                    ? metadata
                    : null))
            .OrderBy(book => book.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(book => book.InventoryCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<Book> UpdateBookAsync(
        Book originalBook,
        Book updatedBook,
        CancellationToken cancellationToken = default)
    {
        var authorChanged = !string.Equals(
            originalBook.Author,
            updatedBook.Author,
            StringComparison.Ordinal);
        var publisherChanged = !string.Equals(
            originalBook.Publisher,
            updatedBook.Publisher,
            StringComparison.Ordinal);
        var isbnChanged = !string.Equals(
            originalBook.ISBN,
            updatedBook.ISBN,
            StringComparison.Ordinal);
        var titleChanged = !string.Equals(
            originalBook.Title,
            updatedBook.Title,
            StringComparison.Ordinal);

        var authorId = originalBook.AuthorId;
        var publisherId = originalBook.PublisherId;

        if (authorChanged)
            authorId = await ResolveAuthorIdAsync(updatedBook.Author.Trim(), cancellationToken);

        if (publisherChanged)
            publisherId = await ResolvePublisherIdAsync(updatedBook.Publisher.Trim(), cancellationToken);

        ApiBookDto? savedBook = null;

        if (titleChanged || isbnChanged || authorChanged || publisherChanged)
        {
            if (string.IsNullOrWhiteSpace(originalBook.ISBN))
                throw new ApiException("A könyv ISBN-je hiányzik, ezért nem módosítható.");

            var payload = new Dictionary<string, object?>
            {
                ["title"] = updatedBook.Title.Trim(),
            };

            if (isbnChanged)
                payload["isbn"] = updatedBook.ISBN.Trim();

            if (authorChanged)
                payload["author_id"] = authorId;

            if (publisherChanged)
                payload["publisher_id"] = publisherId;

            savedBook = await _client.PatchAsync<ApiBookDto>(
                $"books/{Uri.EscapeDataString(originalBook.ISBN)}",
                payload,
                cancellationToken);
        }

        if (!string.Equals(
                originalBook.InventoryCode,
                updatedBook.InventoryCode,
                StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(originalBook.InventoryCode))
                throw new ApiException("A példány leltári száma hiányzik, ezért nem módosítható.");

            await _client.PatchAsync<ApiBookCopyDto>(
                $"book-copies/{Uri.EscapeDataString(originalBook.InventoryCode)}",
                new Dictionary<string, object?>
                {
                    ["inventory_code"] = updatedBook.InventoryCode.Trim(),
                },
                cancellationToken);
        }

        return updatedBook with
        {
            Title = savedBook?.Title ?? updatedBook.Title,
            Author = savedBook?.Author?.Name ?? updatedBook.Author,
            Publisher = savedBook?.Publisher?.Name ?? updatedBook.Publisher,
            ISBN = savedBook?.Isbn ?? updatedBook.ISBN,
            InventoryCode = updatedBook.InventoryCode.Trim(),
            AuthorId = authorId,
            PublisherId = publisherId,
        };
    }

    private async Task<IReadOnlyList<ApiBookCopyDto>> GetBookCopiesAsync(
        CancellationToken cancellationToken)
    {
        if (!_client.IsLibrarian || _client.IsAdmin)
        {
            return await _client.GetAllAsync<ApiBookCopyDto>(
                "book-copies",
                cancellationToken);
        }

        var copies = new List<ApiBookCopyDto>();

        foreach (var library in _client.ManagedLibraries)
        {
            if (string.IsNullOrWhiteSpace(library.Id))
                continue;

            var libraryCopies = await _client.GetAllAsync<ApiBookCopyDto>(
                $"book-copies?library_id={Uri.EscapeDataString(library.Id)}",
                cancellationToken);

            copies.AddRange(libraryCopies);
        }

        return copies
            .Where(copy => !string.IsNullOrWhiteSpace(copy.InventoryCode))
            .GroupBy(copy => copy.InventoryCode!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private async Task<string> ResolveAuthorIdAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var authors = await _client.GetAllAsync<ApiAuthorDto>(
            $"authors?search={Uri.EscapeDataString(name)}",
            cancellationToken);

        var match = authors.FirstOrDefault(author =>
            string.Equals(author.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));

        if (match?.Id is { Length: > 0 } matchId)
            return matchId;

        var created = await _client.PostAsync<ApiAuthorDto>(
            "authors",
            new Dictionary<string, object?> { ["name"] = name },
            cancellationToken);

        return created.Id
               ?? throw new ApiException("A szerző létrehozása után nem érkezett azonosító.");
    }

    private async Task<string> ResolvePublisherIdAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var publishers = await _client.GetAllAsync<ApiPublisherDto>(
            $"publishers?search={Uri.EscapeDataString(name)}",
            cancellationToken);

        var match = publishers.FirstOrDefault(publisher =>
            string.Equals(publisher.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));

        if (match?.Id is { Length: > 0 } matchId)
            return matchId;

        var created = await _client.PostAsync<ApiPublisherDto>(
            "publishers",
            new Dictionary<string, object?> { ["name"] = name },
            cancellationToken);

        return created.Id
               ?? throw new ApiException("A kiadó létrehozása után nem érkezett azonosító.");
    }
}
