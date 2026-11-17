using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KönyvTár.Services.Api;

public sealed class ApiClient : IDisposable
{
    public const string DefaultBaseUrl = "http://localhost:8000";

    private const int PageSize = 100;

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _token;

    public ApiClient(string? baseUrl = null)
    {
        var resolvedBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? Environment.GetEnvironmentVariable("KONYVTAR_API_URL")
            : baseUrl;

        if (string.IsNullOrWhiteSpace(resolvedBaseUrl))
            resolvedBaseUrl = DefaultBaseUrl;

        resolvedBaseUrl = resolvedBaseUrl.Trim().TrimEnd('/');
        if (!resolvedBaseUrl.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
            resolvedBaseUrl += "/api/v1";

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(resolvedBaseUrl + "/", UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(30),
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("KonyvtarDesktop/1.0");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }

    public Uri BaseAddress => _httpClient.BaseAddress!;

    public ApiUser? CurrentUser { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(_token);

    internal string? SessionToken => _token;

    public async Task<ApiUser> RestoreSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        _token = token;
        try
        {
            var currentUser = await GetAsync<ApiUserDto>("auth/me", cancellationToken);
            CurrentUser = ApiMapping.ToUser(currentUser);
            return CurrentUser;
        }
        catch
        {
            _token = null;
            CurrentUser = null;
            throw;
        }
    }

    public async Task<ApiUser> UpdateProfileAsync(
        string firstName, string lastName, string email, string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var dto = await PatchAsync<ApiUserDto>("profile", new Dictionary<string, object?>
        {
            ["first_name"] = firstName,
            ["last_name"] = lastName,
            ["email"] = email,
            ["phone_number"] = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
        }, cancellationToken);
        return CurrentUser = ApiMapping.ToUser(dto);
    }

    public async Task ChangePasswordAsync(
        string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Put, "profile/password", new Dictionary<string, object?>
        {
            ["current_password"] = currentPassword,
            ["password"] = newPassword,
            ["password_confirmation"] = newPassword,
        }, cancellationToken);
    }

    public bool IsAdmin => CurrentUser?.IsAdmin == true;

    public bool IsLibrarian => CurrentUser?.IsLibrarian == true;

    public IReadOnlyList<ApiLibrary> ManagedLibraries =>
        CurrentUser?.ManagedLibraries ?? Array.Empty<ApiLibrary>();

    public async Task<ApiUser> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var login = await SendJsonAsync<ApiLoginEnvelope>(
            HttpMethod.Post,
            "auth/login",
            new Dictionary<string, object?>
            {
                ["email"] = email,
                ["password"] = password,
                ["device_name"] = "Könyvtár asztali alkalmazás",
            },
            cancellationToken,
            authenticated: false);

        if (string.IsNullOrWhiteSpace(login.Token) || login.User is null)
            throw new ApiException("Az API nem küldött érvényes belépési tokent.");

        _token = login.Token;

        try
        {
            var currentUser = await GetAsync<ApiUserDto>("auth/me", cancellationToken);
            CurrentUser = ApiMapping.ToUser(currentUser);
            return CurrentUser;
        }
        catch
        {
            _token = null;
            CurrentUser = null;
            throw;
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated)
            return;

        try
        {
            using var response = await SendAsync(
                HttpMethod.Post,
                "auth/logout",
                null,
                cancellationToken);
        }
        catch (ApiException)
        {
            // The local session is cleared even when the server cannot revoke the token.
        }
        finally
        {
            _token = null;
            CurrentUser = null;
        }
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(
        string path,
        CancellationToken cancellationToken = default)
    {
        var items = new List<T>();
        var page = 1;

        while (true)
        {
            var pagedPath = AppendQuery(path, $"per_page={PageSize}&page={page}");
            var result = await SendJsonAsync<ApiPage<T>>(
                HttpMethod.Get,
                pagedPath,
                null,
                cancellationToken);

            if (result.Data.Count > 0)
                items.AddRange(result.Data);

            if (result.Meta is null || page >= result.Meta.LastPage)
                break;

            page++;
        }

        return items;
    }

    public async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        var result = await SendJsonAsync<ApiDataEnvelope<T>>(
            HttpMethod.Get,
            path,
            null,
            cancellationToken);

        return result.Data ?? throw EmptyResponse();
    }

    public async Task<T> PostAsync<T>(
        string path,
        object? body,
        CancellationToken cancellationToken = default)
    {
        var result = await SendJsonAsync<ApiDataEnvelope<T>>(
            HttpMethod.Post,
            path,
            body,
            cancellationToken);

        return result.Data ?? throw EmptyResponse();
    }

    public async Task<T> PatchAsync<T>(
        string path,
        object? body,
        CancellationToken cancellationToken = default)
    {
        var result = await SendJsonAsync<ApiDataEnvelope<T>>(
            HttpMethod.Patch,
            path,
            body,
            cancellationToken);

        return result.Data ?? throw EmptyResponse();
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            path,
            null,
            cancellationToken);
    }

    public void Dispose() => _httpClient.Dispose();

    private async Task<T> SendJsonAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken,
        bool authenticated = true)
    {
        using var response = await SendAsync(method, path, body, cancellationToken, authenticated);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var result = await JsonSerializer.DeserializeAsync<T>(
            stream,
            _jsonOptions,
            cancellationToken);

        return result ?? throw EmptyResponse();
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken,
        bool authenticated = true)
    {
        using var request = new HttpRequestMessage(method, path);

        if (authenticated && !string.IsNullOrWhiteSpace(_token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        try
        {
            var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.IsSuccessStatusCode)
                return response;

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Dispose();
            throw ApiException.FromResponse(response.StatusCode, responseBody);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            throw ApiException.NetworkError(_httpClient.BaseAddress, exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw ApiException.Timeout(_httpClient.BaseAddress, exception);
        }
    }

    private static ApiException EmptyResponse() =>
        new("Az API üres választ adott vissza.");

    private static string AppendQuery(string path, string query) =>
        path.Contains('?', StringComparison.Ordinal)
            ? $"{path}&{query}"
            : $"{path}?{query}";
}
