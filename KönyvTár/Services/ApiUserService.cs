using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KönyvTár.Models;
using KönyvTár.Services.Api;

namespace KönyvTár.Services;

public sealed class ApiUserService : IUserService
{
    private readonly ApiClient _client;

    public ApiUserService(ApiClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<LibraryUser>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var memberships = await _client.GetAllAsync<ApiMembershipDto>(
            "memberships?status=active",
            cancellationToken);

        return memberships
            .Where(membership =>
                membership.User is not null
                && !string.IsNullOrWhiteSpace(membership.User.Id)
                && !string.IsNullOrWhiteSpace(membership.LibraryId)
                && string.Equals(membership.Status, "active", StringComparison.OrdinalIgnoreCase))
            .GroupBy(membership => membership.User!.Id!, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                var user = first.User!;
                var name = user.FullName?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    name = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(name))
                    name = user.Email ?? "Ismeretlen olvasó";

                var libraryIds = group
                    .Select(membership => membership.LibraryId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new LibraryUser(
                    name,
                    user.Email ?? string.Empty,
                    user.Id,
                    libraryIds);
            })
            .OrderBy(user => user.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
