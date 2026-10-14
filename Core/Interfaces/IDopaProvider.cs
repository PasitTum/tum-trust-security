using Tum.Trust.Security.Core.Models;

namespace Tum.Trust.Security.Core.Interfaces;

/// <summary>
/// Defines the contract for DOPA citizen card / Smart Card verification operations.
/// Each method corresponds to a specific DOPA verification method and returns the provider response unmodified.
/// </summary>
public interface IDopaProvider : IVerificationProvider
{
    /// <summary>
    /// Verifies citizen card status with DOPA using front-of-card demographics and back-of-card Laser ID.
    /// </summary>
    /// <param name="pid">13-digit Thai National ID number.</param>
    /// <param name="firstName">Citizen first name in Thai.</param>
    /// <param name="lastName">Citizen last name in Thai.</param>
    /// <param name="birthDate">Birth date in Buddhist Era format (YYYYMMDD, e.g. "25300101").</param>
    /// <param name="laserId">Back-of-card Laser ID code (e.g. "ME0123456789").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="VerificationResult"/> containing the raw response from DOPA.</returns>
    Task<VerificationResult> VerifyByLaserAsync(
        string pid,
        string firstName,
        string lastName,
        string birthDate,
        string laserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies citizen Smart Card status with DOPA using the 13-digit PID and Smart Card Chip Number.
    /// </summary>
    /// <param name="pid">13-digit Thai National ID number.</param>
    /// <param name="chipNo">Smart Card chip number read from the card reader.</param>
    /// <param name="bp1No">Optional BP1 request number read from the Smart Card.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="VerificationResult"/> containing the raw response from DOPA.</returns>
    Task<VerificationResult> VerifyByChipAsync(
        string pid,
        string chipNo,
        string? bp1No = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs 1:1 biometric face verification with the DOPA Face Verification API using a 13-digit PID and a Base64-encoded face image.
    /// Automatically strips any <c>data:image/...;base64,</c> prefix before sending to DOPA.
    /// </summary>
    /// <param name="pid">13-digit Thai National ID number.</param>
    /// <param name="faceImageBase64">Base64-encoded JPEG/PNG image of the citizen's face (with or without <c>data:image/...;base64,</c> prefix).</param>
    /// <param name="threshold">Optional similarity threshold override (0.0 to 1.0). If omitted, uses <see cref="DopaOptions.FaceThreshold"/> (default 0.80).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="VerificationResult"/> containing the raw face verification response (<c>similarity</c>, <c>confidence</c>, <c>match</c>, <c>result_code</c>, <c>result_text</c>).</returns>
    Task<VerificationResult> VerifyByFaceAsync(
        string pid,
        string faceImageBase64,
        float? threshold = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests an OAuth 2.0 <c>client_credentials</c> Bearer access token from the configured DOPA Token Endpoint (<see cref="DopaOptions.TokenUrl"/>).
    /// Note: <see cref="IDopaProvider"/> automatically requests and caches this token when <see cref="DopaOptions.TokenUrl"/> and <see cref="DopaOptions.ClientId"/> are configured.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="VerificationResult"/> containing the token response (<c>access_token</c>, <c>token_type</c>, <c>expires_in</c>).</returns>
    Task<VerificationResult> RequestAccessTokenAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs an active health check / connectivity test against the configured DOPA Gateway or DGA validation service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="VerificationResult"/> indicating whether the DOPA service is reachable and responsive.</returns>
    Task<VerificationResult> HealthCheckAsync(
        CancellationToken cancellationToken = default);
}
