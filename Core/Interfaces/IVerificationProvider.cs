using Netka.Identity.Core.Models;

namespace Netka.Identity.Core.Interfaces;

/// <summary>
/// Defines the contract for identity verification providers (ThaID, Google, Microsoft, etc.).
/// </summary>
public interface IVerificationProvider
{
    /// <summary>
    /// Gets the unique identifier for this provider (e.g., "ThaID", "DOPA", "Google", "Microsoft").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Verifies user identity and returns the provider response unmodified.
    /// </summary>
    /// <param name="request">The verification request containing the authorization code or verification parameters.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A <see cref="VerificationResult"/> containing verified user attributes or error details.</returns>
    Task<VerificationResult> VerifyAsync(VerificationRequest request, CancellationToken cancellationToken = default);
}
