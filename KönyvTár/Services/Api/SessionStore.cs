using System;
using System.IO;
using System.Threading.Tasks;

namespace KönyvTár.Services.Api;

internal static class SessionStore
{
    private static readonly string SessionPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Konyvtar",
        "session.token");

    public static async Task<string?> ReadAsync()
    {
        if (!File.Exists(SessionPath))
            return null;

        var token = (await File.ReadAllTextAsync(SessionPath)).Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    public static async Task SaveAsync(string token)
    {
        var directory = Path.GetDirectoryName(SessionPath)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(SessionPath, token);

        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(SessionPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    public static Task ClearAsync()
    {
        if (File.Exists(SessionPath))
            File.Delete(SessionPath);
        return Task.CompletedTask;
    }
}
