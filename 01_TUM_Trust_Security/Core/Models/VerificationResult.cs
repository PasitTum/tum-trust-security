namespace Tum.Trust.Security.Core.Models;

/// <summary>
/// Hybrid model representing the outcome of an identity verification operation.
/// </summary>
public class VerificationResult
{
    /// <summary>
    /// Indicates whether the identity verification succeeded.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Human-readable summary message of the verification result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Machine-readable error code when <see cref="IsSuccess"/> is false.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Detailed structured error information when <see cref="IsSuccess"/> is false.
    /// </summary>
    public VerificationError? Error { get; set; }

    /// <summary>
    /// Dynamic payload container for diverse provider attributes (e.g., citizen_id, email, given_name).
    /// Uses <see cref="StringComparer.OrdinalIgnoreCase"/> by default.
    /// </summary>
    public IDictionary<string, object> Data { get; set; } =
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
