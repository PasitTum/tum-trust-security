using Tum.Trust.Security.Core.Models.Biometrics;

namespace Tum.Trust.Security.Core.Interfaces;

/// <summary>
/// Contract for high-speed edge AI Presentation Attack Detection (PAD) and anti-deepfake liveness verification.
/// </summary>
public interface IFaceLivenessDetector
{
    /// <summary>
    /// Analyzes a live camera image frame to detect biological liveness, synthetic media, screen replays, or physical masks.
    /// </summary>
    /// <param name="imageBytes">Raw image bytes (JPEG, PNG, or uncompressed RGB24 buffer).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="LivenessDetectionResult"/> containing liveness probability, detected attack type, and telemetry.</returns>
    Task<LivenessDetectionResult> DetectLivenessAsync(
        byte[] imageBytes, 
        CancellationToken cancellationToken = default);
}
