using Netka.Identity.Core.Models;

namespace Netka.Identity.Core.Interfaces;

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
}
