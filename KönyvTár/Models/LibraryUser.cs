namespace KönyvTár.Models;

public sealed record LibraryUser(
    string Name,
    string Email)
{
    public override string ToString() => $"{Name} ({Email})";
}
