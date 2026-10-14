using Tum.Trust.Security.Core.Models;
using Tum.Trust.Security.Core.Models.Passport;

namespace Tum.Trust.Security.Core.Interfaces;

/// <summary>
/// Universal Sovereign Identity Verification contract for ICAO 9303 ePassports and ISO/IEC 18013-5 Mobile Driving Licenses (mDL).
/// </summary>
public interface IBorderlessTrustProvider : IVerificationProvider
{
    /// <summary>
    /// Verifies an ePassport NFC chip reading (DG1 MRZ, DG2 Face Biometrics, EF.SOD) against national root CAs
    /// and performs edge biometric liveness detection.
    /// </summary>
    /// <param name="passportRequest">The raw APDU streams or parsed Data Groups from the ePassport.</param>
    /// <param name="liveCameraFrame">Optional live camera frame for 1:1 facial biometric matching and anti-deepfake liveness.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="PassportVerificationResult"/> confirming sovereign validity, biometric liveness, and zero-knowledge claims.</returns>
    Task<PassportVerificationResult> VerifyPassportAsync(
        PassportVerificationRequest passportRequest, 
        byte[]? liveCameraFrame = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an ISO/IEC 18013-5 Mobile Driving License (mDL) presented from Apple Wallet or Google Wallet via NFC/BLE.
    /// </summary>
    /// <param name="mDocPayload">The raw CBOR/COSE mDoc payload received from the digital wallet.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification result including decrypted selective disclosure attributes.</returns>
    Task<VerificationResult> VerifyMdlAsync(
        byte[] mDocPayload, 
        CancellationToken cancellationToken = default);
}
