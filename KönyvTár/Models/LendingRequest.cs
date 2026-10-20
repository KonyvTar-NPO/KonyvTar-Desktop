using System;

namespace KönyvTár.Models;

public sealed record LendingRequest(
    Book Book,
    LibraryUser User,
    DateTimeOffset From,
    DateTimeOffset To);
