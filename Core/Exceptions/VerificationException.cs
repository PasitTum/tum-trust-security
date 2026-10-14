namespace Tum.Trust.Security.Core.Exceptions;

/// <summary>
/// Exception thrown when an identity verification operation fails due to provider, network, or validation errors.
/// </summary>
public class VerificationException : Exception
{
    /// <summary>
    /// Machine-readable error code for fault attribution.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Optional HTTP status code returned by the external provider.
    /// </summary>
    public int? HttpStatusCode { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="VerificationException"/>.
    /// </summary>
    /// <param name="message">Descriptive error message.</param>
    /// <param name="errorCode">Machine-readable error code.</param>
    /// <param name="httpStatusCode">Optional HTTP status code from the provider.</param>
    /// <param name="innerException">Optional underlying exception.</param>
    public VerificationException(
        string message,
        string errorCode,
        int? httpStatusCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }
}
