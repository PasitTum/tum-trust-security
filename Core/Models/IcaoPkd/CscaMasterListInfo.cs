using System;

namespace Tum.Trust.Security.Core.Models.IcaoPkd;

/// <summary>
/// Metadata describing an official ICAO PKD Country Signing CA (CSCA) MasterList release.
/// </summary>
public class CscaMasterListInfo
{
    /// <summary>
    /// Sequential version or release identifier (e.g., "ML-2026-R10").
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Three-letter ISO country code of the MasterList issuing authority (e.g., "DEU", "USA", "CHE").
    /// </summary>
    public string IssuerCountryCode { get; set; } = "ICAO";

    /// <summary>
    /// Total count of valid sovereign root certificates contained within this MasterList.
    /// </summary>
    public int TotalCertificatesCount { get; set; }

    /// <summary>
    /// Cryptographic SHA-256 fingerprint of the MasterList CMS container.
    /// </summary>
    public string HashSha256 { get; set; } = string.Empty;

    /// <summary>
    /// Expiration date of the MasterList signature.
    /// </summary>
    public DateTime ValidUntilUtc { get; set; } = DateTime.UtcNow.AddYears(1);
}
