using Tum.Trust.Security.Core.Models.Biometrics;

namespace Tum.Trust.Security.Core.Interfaces;

/// <summary>
/// Contract for 1:1 facial biometric feature vector extraction and cosine similarity verification.
/// </summary>
public interface IBiometricFaceMatcher
{
    /// <summary>
    /// Compares a live probe face image against an authoritative reference photo (e.g. from ePassport DG2 or national database).
    /// </summary>
    /// <param name="probeFaceImage">Live probe frame captured by the kiosk/device camera.</param>
    /// <param name="referenceFaceImage">Official biometric photo extracted from the sovereign credential or ID chip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="FaceMatchResult"/> detailing match status, similarity metric, and embedding dimensions.</returns>
    Task<FaceMatchResult> MatchFacesAsync(
        byte[] probeFaceImage, 
        byte[] referenceFaceImage, 
        CancellationToken cancellationToken = default);
}
