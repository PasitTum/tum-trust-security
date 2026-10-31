using Tum.Trust.Security.Core.Models.Passport;

namespace Tum.Trust.Security.Core.Interfaces;

/// <summary>
/// High-performance ICAO Doc 9303 NFC Logical Data Structure (LDS) Inspector.
/// Inspects and cryptographically validates ePassport Data Groups (DG1, DG2, DG14, DG15, and EF.SOD).
/// </summary>
public interface IIcaoNfcDataGroupInspector
{
    /// <summary>
    /// Inspects an entire container of read Data Groups and verifies Passive Authentication (SOD).
    /// </summary>
    NfcDataGroupInspectionResult Inspect(NfcDataGroupContainer container);

    /// <summary>
    /// Inspects Data Group 1 (MRZ Data) and cross-references it with optical MRZ details.
    /// </summary>
    bool InspectDg1Mrz(byte[] dg1Bytes, MrzInfo expectedMrz);

    /// <summary>
    /// Decodes Data Group 2 (Biometric Face Image ISO/IEC 19794-5) and extracts photo payload.
    /// </summary>
    Dg2FaceBiometricInfo InspectDg2Face(byte[] dg2Bytes);

    /// <summary>
    /// Inspects Data Group 14 (Chip Authentication Public Key Info).
    /// </summary>
    Dg14ChipAuthenticationInfo InspectDg14(byte[] dg14Bytes);

    /// <summary>
    /// Inspects Data Group 15 (Active Authentication ECDSA / RSA Public Key).
    /// </summary>
    Dg15ActiveAuthenticationInfo InspectDg15(byte[] dg15Bytes);

    /// <summary>
    /// Validates the Document Security Object (EF.SOD) against all extracted Data Group hashes.
    /// </summary>
    SodVerificationResult VerifySodPassiveAuthentication(byte[] sodBytes, IDictionary<int, byte[]> dataGroupBytes);
}
