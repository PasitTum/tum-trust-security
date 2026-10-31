namespace Tum.Trust.Security.Core.Models;

/// <summary>
/// Pure DTO representing structured error details for a failed verification attempt.
/// </summary>
public class VerificationError
{
    /// <summary>
    /// Machine-readable error code (e.g., "INVALID_STATE", "TOKEN_EXCHANGE_FAILED", "PROVIDER_UNAVAILABLE").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Descriptive error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional HTTP status code returned by the external provider.
    /// </summary>
    public int? HttpStatusCode { get; set; }
}
