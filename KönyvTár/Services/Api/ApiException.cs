using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace KönyvTár.Services.Api;

public sealed class ApiException : Exception
{
    private static readonly IReadOnlyDictionary<string, string> KnownMessages =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["These credentials do not match our records."] = "Hibás e-mail cím vagy jelszó.",
            ["This account is not active."] = "A fiók nem aktív.",
            ["The selected inventory code is invalid."] = "A kiválasztott leltári szám érvénytelen.",
            ["This reader account is not active."] = "Az olvasói fiók nem aktív.",
            ["This reader is not an active member of the library."] =
                "Ez az olvasó nem aktív tagja a kiválasztott könyvtárnak.",
            ["This copy is not available for borrowing."] = "Ez a példány jelenleg nem kölcsönözhető.",
            ["This copy already has an open borrowing."] = "Ehhez a példányhoz már tartozik nyitott kölcsönzés.",
            ["The due date must be after or equal to the borrowing date."] =
                "A lejárati dátum nem lehet korábbi a kölcsönzés dátumánál.",
            ["The return date must be after or equal to the borrowing date."] =
                "A visszahozatal dátuma nem lehet korábbi a kölcsönzés dátumánál.",
            ["Only an active or overdue borrowing can be returned."] =
                "Csak aktív vagy lejárt kölcsönzés hozható vissza.",
            ["Only an active or overdue borrowing can be marked as lost."] =
                "Csak aktív vagy lejárt kölcsönzés jelölhető elveszettnek.",
            ["The inventory code cannot be changed while the copy has borrowing history."] =
                "A leltári szám nem módosítható, mert a példánynak már van kölcsönzési előzménye.",
            ["A copy with an open borrowing cannot be moved to another library."] =
                "Nyitott kölcsönzéssel rendelkező példány nem helyezhető át másik könyvtárba.",
        };

    public ApiException(
        string message,
        HttpStatusCode? statusCode = null,
        IReadOnlyDictionary<string, string[]>? validationErrors = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>();
    }

    public HttpStatusCode? StatusCode { get; }

    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;

    public static ApiException FromResponse(HttpStatusCode statusCode, string? responseBody)
    {
        var message = statusCode switch
        {
            HttpStatusCode.Unauthorized => "Lejárt a munkamenet. Jelentkezz be újra.",
            HttpStatusCode.Forbidden => "Nincs jogosultságod ehhez a művelethez.",
            HttpStatusCode.NotFound => "A keresett adat nem található.",
            HttpStatusCode.InternalServerError => "Belső szerverhiba történt az API-ban.",
            _ => $"Az API hibát adott vissza ({(int)statusCode}).",
        };

        var validationErrors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                using var document = JsonDocument.Parse(responseBody);
                var root = document.RootElement;

                if (root.TryGetProperty("errors", out var errors)
                    && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in errors.EnumerateObject())
                    {
                        var messages = new List<string>();
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in property.Value.EnumerateArray())
                            {
                                if (element.ValueKind == JsonValueKind.String)
                                    messages.Add(element.GetString() ?? string.Empty);
                            }
                        }

                        validationErrors[property.Name] = messages.ToArray();
                    }
                }

                if (statusCode != HttpStatusCode.Unauthorized
                    && root.TryGetProperty("message", out var responseMessage)
                    && responseMessage.ValueKind == JsonValueKind.String
                    && responseMessage.GetString() is { Length: > 0 } serverMessage)
                {
                    message = Translate(serverMessage);
                }

                if (validationErrors.Count > 0)
                {
                    var firstValidationMessage = validationErrors.Values
                        .SelectMany(messages => messages)
                        .FirstOrDefault(validationMessage => !string.IsNullOrWhiteSpace(validationMessage));

                    if (firstValidationMessage is not null)
                        message = Translate(firstValidationMessage);
                }
            }
            catch (JsonException)
            {
                // The response was not JSON. Keep the HTTP status based message.
            }
        }

        return new ApiException(message, statusCode, validationErrors);
    }

    public static ApiException NetworkError(Uri? address, Exception innerException) =>
        new(
            address is null
                ? "Nem érhető el a könyvtár API. Ellenőrizd, hogy fut-e a Laravel szerver."
                : $"Nem érhető el a könyvtár API ({address.GetLeftPart(UriPartial.Authority)}). " +
                  "Ellenőrizd, hogy fut-e a Laravel szerver.",
            innerException: innerException);

    public static ApiException Timeout(Uri? address, Exception innerException) =>
        new(
            address is null
                ? "Az API nem válaszolt időben."
                : $"Az API nem válaszolt időben ({address.GetLeftPart(UriPartial.Authority)}).",
            innerException: innerException);

    private static string Translate(string message) =>
        KnownMessages.TryGetValue(message, out var translated) ? translated : message;
}
