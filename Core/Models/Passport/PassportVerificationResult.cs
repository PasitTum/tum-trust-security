namespace Netka.Identity.Core.Models.Passport;

/// <summary>
/// Outcome of a full ICAO 9303 Sovereign Identity verification operation.
/// </summary>
public class PassportVerificationResult : VerificationResult
{
    /// <summary>
    /// Indicates whether Passive Authentication (PA) succeeded (CSCA signature verified against SOD).
    /// </summary>
    public bool IsPassiveAuthenticationValid { get; set; }

    /// <summary>
    /// Indicates whether Active Authentication (AA) succeeded (Chip private key challenge verified).
    /// </summary>
    public bool IsActiveAuthenticationValid { get; set; }

    /// <summary>
    /// Indicates whether the live camera subject matched the DG2 biometric photo.
    /// </summary>
    public bool IsBiometricMatch { get; set; }

    /// <summary>
    /// Biometric match confidence score (0.00 to 1.00).
    /// </summary>
    public double BiometricMatchScore { get; set; }

    /// <summary>
    /// ISO/IEC 30107 Anti-Spoofing Liveness probability (0.00 to 1.00).
    /// </summary>
    public double LivenessScore { get; set; }

    /// <summary>
    /// Indicates whether the subject passed Presentation Attack Detection (real human, not deepfake/mask).
    /// </summary>
    public bool IsLiveHuman { get; set; }

    /// <summary>
    /// Three-letter ISO country code of the passport issuing authority (e.g., "USA", "DEU", "JPN", "GBR", "THA").
    /// </summary>
    public string IssuingCountry { get; set; } = string.Empty;

    /// <summary>
    /// Privacy-preserving Zero-Knowledge Sovereign Pass token containing cryptographically attested claims.
    /// </summary>
    public string? ZeroKnowledgeTrustToken { get; set; }

    /// <summary>
    /// Parsed MRZ details.
    /// </summary>
    public MrzInfo? Mrz { get; set; }
}
