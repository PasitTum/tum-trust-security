namespace Tum.Trust.Security.Infrastructure.Helpers;

/// <summary>
/// String masking utilities for secure logging and display of sensitive data.
/// Compatible across .NET 5.0, .NET 8.0, and .NET 10.0.
/// </summary>
public static class StringMaskingHelper
{
    /// <summary>
    /// Masks a string by showing only the first 4 and last 4 characters, with ellipsis in between.
    /// Shorter strings (≤8 characters) are completely masked with "***".
    /// </summary>
    /// <param name="value">String to mask</param>
    /// <returns>Masked string (e.g., "abc1…def9" or "***")</returns>
    public static string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Length <= 8)
            return "***";

        return value[..4] + "…" + value[^4..];
    }

    /// <summary>
    /// Masks a Thai National ID (PID) by showing first digit, 8 asterisks, and last 4 digits.
    /// Very short strings (&lt;5 characters) are completely masked with "***".
    /// </summary>
    /// <param name="pid">Thai National ID to mask</param>
    /// <returns>Masked PID (e.g., "1********1234" or "***")</returns>
    public static string MaskThaiNationalId(string? pid)
    {
        if (string.IsNullOrEmpty(pid) || pid.Length < 5)
            return "***";

        return pid[..1] + "********" + pid[^4..];
    }

    /// <summary>
    /// Masks a Thai National ID (PID) for safe display/logging.
    /// Alias for <see cref="MaskThaiNationalId(string?)"/>.
    /// </summary>
    /// <param name="pid">Thai National ID to mask</param>
    /// <returns>Masked PID (e.g., "1********1234" or "***")</returns>
    public static string MaskPid(string? pid) => MaskThaiNationalId(pid);
}
