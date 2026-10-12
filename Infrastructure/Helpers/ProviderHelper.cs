using System.Text.Json;
using Netka.Identity.Core.Exceptions;
using Netka.Identity.Core.Interfaces;
using Netka.Identity.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Netka.Identity.Infrastructure.Helpers;

/// <summary>
/// Shared helper and extension methods for identity verification providers (ThaID, DOPA)
/// and consumer applications. Compatible across .NET 5.0, .NET 8.0, and .NET 10.0.
/// </summary>
public static class ProviderHelper
{
    private const int ThaiNationalIdLength = 13;

    /// <summary>
    /// Validates whether a string is a valid 13-digit Thai National ID (PID)
    /// using the official Modulo 11 checksum algorithm.
    /// </summary>
    public static bool IsValidThaiNationalId(this string? pid)
    {
        if (string.IsNullOrWhiteSpace(pid) || pid.Length != ThaiNationalIdLength)
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var c = pid[i];
            if (c < '0' || c > '9')
            {
                return false;
            }

            sum += (c - '0') * (13 - i);
        }

        var lastChar = pid[12];
        if (lastChar < '0' || lastChar > '9')
        {
            return false;
        }

        var checkDigit = (11 - (sum % 11)) % 10;
        return checkDigit == (lastChar - '0');
    }

    /// <summary>
    /// Reads a required string value from an <see cref="IConfigurationSection"/> or throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    public static string GetRequiredValue(this IConfigurationSection section, string key)
    {
        if (section is null)
        {
            throw new ArgumentNullException(nameof(section));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Configuration key cannot be null or whitespace.", nameof(key));
        }

        var value = section[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required configuration: {section.Path}:{key}");
        }

        return value;
    }

    /// <summary>
    /// Retrieves a string value from a parameter dictionary by checking one or more candidate keys
    /// using culture-invariant, case-insensitive (<see cref="StringComparison.OrdinalIgnoreCase"/>) matching.
    /// </summary>
    public static string? GetStringValue(this IDictionary<string, object>? parameters, params string[] candidateKeys)
    {
        if (parameters is null)
        {
            return null;
        }

        foreach (var key in candidateKeys)
        {
            if (parameters.TryGetValue(key, out var rawValue))
            {
                var text = rawValue?.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }

            // Fallback for dictionaries created without StringComparer.OrdinalIgnoreCase
            foreach (var kvp in parameters)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    var text = kvp.Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text.Trim();
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a standardized failed <see cref="VerificationResult"/> with structured <see cref="VerificationError"/>.
    /// </summary>
    public static VerificationResult CreateFailureResult(
        string errorCode,
        string message,
        int? httpStatusCode = null)
    {
        return new VerificationResult
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            Message = message,
            Error = new VerificationError
            {
                Code = errorCode,
                Message = message,
                HttpStatusCode = httpStatusCode
            }
        };
    }

    /// <summary>
    /// Parses a JSON object string into a case-insensitive dictionary of primitive/native values.
    /// </summary>
    public static IDictionary<string, object> ParseJsonToDictionary(string json, string errorCodeOnFailure)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new VerificationException("Provider response root must be a JSON object.", errorCodeOnFailure);
            }

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                result[property.Name] = ConvertJsonElement(property.Value);
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new VerificationException("Failed to parse JSON response from provider.", errorCodeOnFailure, null, ex);
        }
    }

    /// <summary>
    /// Converts a <see cref="JsonElement"/> into a standard .NET primitive value, dictionary, or list.
    /// </summary>
    public static object ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;

            case JsonValueKind.Number:
                if (element.TryGetInt64(out long longValue))
                {
                    return longValue;
                }
                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return string.Empty;

            case JsonValueKind.Object:
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElement(prop.Value);
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonElement(item));
                }
                return list;

            default:
                return element.GetRawText();
        }
    }

    /// <summary>
    /// Encrypts sensitive PII fields (e.g., citizen_id, pid) using <see cref="IEncryptionService"/>
    /// and writes an encrypted PII vault log entry per the Tri-Log Strategy.
    /// </summary>
    public static void LogEncryptedPiiIfPresent(
        this ILogger logger,
        IEncryptionService encryptionService,
        string providerName,
        IDictionary<string, object> userInfo,
        params string[] candidateKeys)
    {
        if (logger is null) throw new ArgumentNullException(nameof(logger));
        if (encryptionService is null) throw new ArgumentNullException(nameof(encryptionService));
        if (userInfo is null) throw new ArgumentNullException(nameof(userInfo));

        var piiValue = userInfo.GetStringValue(candidateKeys);
        if (!string.IsNullOrWhiteSpace(piiValue))
        {
            var encryptedPii = encryptionService.Encrypt(piiValue);
            logger.LogInformation(
                "[PII-VAULT] Verified {Provider} identity. EncryptedSubject: {EncryptedSubject}",
                providerName,
                encryptedPii);
        }
    }
}
