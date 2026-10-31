using System;

namespace Tum.Trust.Security.Core.Exceptions;

/// <summary>
/// Exception thrown when validation of identity data (such as Thai National ID, checksum, or smart card payload) fails.
/// </summary>
public class IdentityValidationException : VerificationException
{
    /// <summary>
    /// Initializes a new instance of <see cref="IdentityValidationException"/>.
    /// </summary>
    /// <param name="message">Descriptive error message.</param>
    /// <param name="errorCode">Machine-readable error code.</param>
    /// <param name="innerException">Optional underlying exception.</param>
    public IdentityValidationException(
        string message,
        string errorCode = "IDENTITY_VALIDATION_FAILED",
        Exception? innerException = null)
        : base(message, errorCode, null, innerException)
    {
    }
}
