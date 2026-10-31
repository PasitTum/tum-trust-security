namespace Tum.Trust.Security.Core.Models.Passport;

/// <summary>
/// Represents an ePassport verification payload containing NFC Data Groups or raw APDU dumps.
/// </summary>
public class PassportVerificationRequest
{
    /// <summary>
    /// Parsed MRZ metadata used to derive Basic Access Control (BAC) / PACE keys.
    /// </summary>
    public MrzInfo? Mrz { get; set; }

    /// <summary>
    /// Binary content of Data Group 1 (MRZ raw bytes).
    /// </summary>
    public byte[]? Dg1Bytes { get; set; }

    /// <summary>
    /// Binary content of Data Group 2 (Biometric facial image in JPEG 2000 or WSQ format).
    /// </summary>
    public byte[]? Dg2FacePhotoBytes { get; set; }

    /// <summary>
    /// Binary content of Security Object Document (EF.SOD) containing CMS signatures and hash tables.
    /// </summary>
    public byte[]? SodBytes { get; set; }

    /// <summary>
    /// Chip authentication challenge response bytes to detect cloned cards (Active Authentication).
    /// </summary>
    public byte[]? ActiveAuthenticationSignature { get; set; }

    /// <summary>
    /// Additional dynamic parameters for specialized border terminal hardware.
    /// </summary>
    public IDictionary<string, object> Parameters { get; set; } = 
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
