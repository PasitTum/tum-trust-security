namespace Tum.Trust.Security.Core.Models.Passport;

/// <summary>
/// Container holding raw NFC Data Groups read from an ICAO 9303 ePassport chip.
/// </summary>
public class NfcDataGroupContainer
{
    /// <summary>
    /// Optical or entered MRZ for cross-checking against DG1.
    /// </summary>
    public MrzInfo? OpticalMrz { get; set; }

    /// <summary>
    /// Dictionary of read data groups keyed by Data Group number (e.g. 1 for DG1, 2 for DG2, 14 for DG14, 15 for DG15).
    /// </summary>
    public IDictionary<int, byte[]> DataGroups { get; set; } = new Dictionary<int, byte[]>();

    /// <summary>
    /// Binary content of Document Security Object (EF.SOD).
    /// </summary>
    public byte[]? SodBytes { get; set; }
}

/// <summary>
/// Comprehensive outcome of NFC Data Group inspection and cryptographic verification.
/// </summary>
public class NfcDataGroupInspectionResult
{
    /// <summary>
    /// Indicates whether overall inspection and validation succeeded.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Indicates whether Passive Authentication hash verification passed.
    /// </summary>
    public bool IsPassiveAuthenticationValid { get; set; }

    /// <summary>
    /// Indicates whether electronic DG1 MRZ matches optical MRZ.
    /// </summary>
    public bool IsDg1MrzMatch { get; set; }

    /// <summary>
    /// Indicates whether valid DG2 facial biometrics are present.
    /// </summary>
    public bool HasFaceBiometrics { get; set; }

    /// <summary>
    /// Indicates whether DG15 Active Authentication public key is present.
    /// </summary>
    public bool HasActiveAuthentication { get; set; }

    /// <summary>
    /// Indicates whether DG14 Chip Authentication parameters are present.
    /// </summary>
    public bool HasChipAuthentication { get; set; }

    /// <summary>
    /// Extracted biometric face metadata and image bytes.
    /// </summary>
    public Dg2FaceBiometricInfo? FaceInfo { get; set; }

    /// <summary>
    /// Extracted DG14 Chip Authentication details.
    /// </summary>
    public Dg14ChipAuthenticationInfo? ChipAuthInfo { get; set; }

    /// <summary>
    /// Extracted DG15 Active Authentication details.
    /// </summary>
    public Dg15ActiveAuthenticationInfo? ActiveAuthInfo { get; set; }

    /// <summary>
    /// Detailed SOD hash verification results.
    /// </summary>
    public SodVerificationResult? SodResult { get; set; }

    /// <summary>
    /// Informational warnings or discrepancies collected during inspection.
    /// </summary>
    public IList<string> ValidationMessages { get; set; } = new List<string>();
}

/// <summary>
/// Biometric face metadata and photo extracted from Data Group 2.
/// </summary>
public class Dg2FaceBiometricInfo
{
    /// <summary>
    /// Indicates whether a valid facial image payload was found and parsed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Image encoding format (e.g., "JPEG" or "JPEG 2000").
    /// </summary>
    public string ImageFormat { get; set; } = "JPEG";

    /// <summary>
    /// Detected image width in pixels.
    /// </summary>
    public int ImageWidth { get; set; }

    /// <summary>
    /// Detected image height in pixels.
    /// </summary>
    public int ImageHeight { get; set; }

    /// <summary>
    /// Raw extracted image bytes ready for decoding or rendering.
    /// </summary>
    public byte[]? ImageBytes { get; set; }

    /// <summary>
    /// Facial expression attribute recorded in biometric header.
    /// </summary>
    public string? FacialExpression { get; set; } = "Neutral";
}

/// <summary>
/// Security info extracted from Data Group 14 (Chip Authentication / PACE).
/// </summary>
public class Dg14ChipAuthenticationInfo
{
    /// <summary>
    /// Indicates whether DG14 is present and populated on the chip.
    /// </summary>
    public bool IsPresent { get; set; }

    /// <summary>
    /// Object Identifier (OID) of the chip authentication mechanism.
    /// </summary>
    public string AlgorithmOid { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable protocol name.
    /// </summary>
    public string ProtocolName { get; set; } = "ChipAuthentication-ECDH";

    /// <summary>
    /// Diffie-Hellman / Elliptic Curve key size in bits.
    /// </summary>
    public int KeyLengthBits { get; set; } = 256;
}

/// <summary>
/// Public key extracted from Data Group 15 for Active Authentication.
/// </summary>
public class Dg15ActiveAuthenticationInfo
{
    /// <summary>
    /// Indicates whether DG15 is present on the chip.
    /// </summary>
    public bool IsPresent { get; set; }

    /// <summary>
    /// Algorithm type of the active authentication public key (e.g. "RSA" or "ECDSA").
    /// </summary>
    public string KeyType { get; set; } = "RSA";

    /// <summary>
    /// Key modulus or curve length in bits.
    /// </summary>
    public int ModulusLengthBits { get; set; } = 2048;

    /// <summary>
    /// SubjectPublicKeyInfo DER encoded public key bytes.
    /// </summary>
    public byte[]? PublicKeyDerBytes { get; set; }
}

/// <summary>
/// Cryptographic result of Passive Authentication (SOD hash checks).
/// </summary>
public class SodVerificationResult
{
    /// <summary>
    /// Indicates whether all Data Group hashes matched the hashes stored in the SOD.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Hash algorithm used in the SOD (e.g., "SHA-256").
    /// </summary>
    public string HashAlgorithm { get; set; } = "SHA-256";

    /// <summary>
    /// Number of verified data groups.
    /// </summary>
    public int VerifiedDataGroupsCount { get; set; }

    /// <summary>
    /// Map of Data Group numbers to their verification status.
    /// </summary>
    public IDictionary<int, bool> DataGroupHashMatches { get; set; } = new Dictionary<int, bool>();

    /// <summary>
    /// List of hash mismatch errors or discrepancies.
    /// </summary>
    public IList<string> Discrepancies { get; set; } = new List<string>();
}
