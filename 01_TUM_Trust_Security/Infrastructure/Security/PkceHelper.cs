using System.Security.Cryptography;
using System.Text;

namespace Tum.Trust.Security.Infrastructure.Security;

/// <summary>
/// Helper class for PKCE (Proof Key for Code Exchange, RFC 7636) operations.
/// Compatible across .NET 5.0, .NET 8.0, and .NET 10.0.
/// </summary>
public static class PkceHelper
{
    private const int MinCodeVerifierLength = 43;
    private const int MaxCodeVerifierLength = 128;

    /// <summary>
    /// Generates a cryptographically secure URL-safe Base64 string (without padding) from the specified number of random bytes.
    /// Useful for generating OAuth 2.0 <c>state</c>, OIDC <c>nonce</c>, or PKCE values.
    /// </summary>
    /// <param name="byteLength">Number of random bytes to generate (default: 32 bytes = 256 bits).</param>
    public static string RandomUrlSafe(int byteLength = 32)
    {
        if (byteLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Byte length must be greater than zero.");
        }

        byte[] bytes = new byte[byteLength];
        RandomNumberGenerator.Fill(bytes);

        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <summary>
    /// Generates a cryptographically secure URL-safe OAuth 2.0 <c>state</c> string.
    /// </summary>
    public static string GenerateState(int byteLength = 32) => RandomUrlSafe(byteLength);

    /// <summary>
    /// Generates a cryptographically secure URL-safe OpenID Connect <c>nonce</c> string.
    /// </summary>
    public static string GenerateNonce(int byteLength = 32) => RandomUrlSafe(byteLength);

    /// <summary>
    /// Masks a general token, state, or secret string for safe logging (e.g., <c>"abcd…wxyz"</c>).
    /// </summary>
    public static string Mask(string? value) => Helpers.ProviderHelper.Mask(value);

    /// <summary>
    /// Masks a Thai National ID (PID) for safe display/logging (e.g., <c>"1********4561"</c>).
    /// </summary>
    public static string MaskPid(string? pid) => Helpers.ProviderHelper.MaskPid(pid);

    /// <summary>
    /// Generates a cryptographically secure random PKCE code verifier (43-128 characters).
    /// </summary>
    public static string GenerateCodeVerifier(int length = MaxCodeVerifierLength)
    {
        if (length < MinCodeVerifierLength || length > MaxCodeVerifierLength)
        {
            throw new ArgumentException(
                $"Code verifier length must be between {MinCodeVerifierLength} and {MaxCodeVerifierLength}",
                nameof(length));
        }

        byte[] bytes = new byte[length * 3 / 4];
        RandomNumberGenerator.Fill(bytes);

        string verifier = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        return verifier.Length > length
            ? verifier[..length]
            : verifier.PadRight(length, 'A');
    }

    /// <summary>
    /// Generates PKCE code challenge from a code verifier using SHA256 (S256 method).
    /// </summary>
    public static string GenerateCodeChallenge(string codeVerifier)
    {
        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            throw new ArgumentException("Code verifier cannot be null or whitespace.", nameof(codeVerifier));
        }

        if (codeVerifier.Length < MinCodeVerifierLength || codeVerifier.Length > MaxCodeVerifierLength)
        {
            throw new ArgumentException(
                $"Code verifier length must be between {MinCodeVerifierLength} and {MaxCodeVerifierLength}",
                nameof(codeVerifier));
        }

        if (!IsValidCodeVerifier(codeVerifier))
        {
            throw new ArgumentException(
                "Code verifier contains invalid characters. Only unreserved characters [A-Z][a-z][0-9]-._~ are allowed",
                nameof(codeVerifier));
        }

        byte[] verifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(verifierBytes);

        return Convert.ToBase64String(hash)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <summary>
    /// Validates that a code verifier contains only unreserved characters per RFC 7636.
    /// </summary>
    public static bool IsValidCodeVerifier(string codeVerifier)
    {
        if (string.IsNullOrEmpty(codeVerifier))
        {
            return false;
        }

        return codeVerifier.All(c =>
            IsAsciiAlphanumeric(c) || c == '-' || c == '.' || c == '_' || c == '~');
    }

    /// <summary>
    /// Validates code challenge format (must be base64url-encoded without padding).
    /// </summary>
    public static bool IsValidCodeChallenge(string codeChallenge)
    {
        if (string.IsNullOrEmpty(codeChallenge) || codeChallenge.Length > 128)
        {
            return false;
        }

        return codeChallenge.All(c => IsAsciiAlphanumeric(c) || c == '-' || c == '_');
    }

    /// <summary>
    /// Verifies that a provided code verifier matches the expected code challenge (S256 method).
    /// </summary>
    public static bool VerifyCodeChallenge(string codeVerifier, string expectedCodeChallenge)
    {
        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            throw new ArgumentException("Code verifier cannot be null or whitespace.", nameof(codeVerifier));
        }

        if (string.IsNullOrWhiteSpace(expectedCodeChallenge))
        {
            throw new ArgumentException("Expected code challenge cannot be null or whitespace.", nameof(expectedCodeChallenge));
        }

        if (!IsValidCodeVerifier(codeVerifier))
        {
            return false;
        }

        try
        {
            string computedChallenge = GenerateCodeChallenge(codeVerifier);
            byte[] aBytes = Encoding.ASCII.GetBytes(computedChallenge);
            byte[] bBytes = Encoding.ASCII.GetBytes(expectedCodeChallenge);
            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsAsciiAlphanumeric(char c)
    {
        return (c >= 'A' && c <= 'Z') ||
               (c >= 'a' && c <= 'z') ||
               (c >= '0' && c <= '9');
    }
}
