namespace Tum.Trust.Security.Core.Models.Mobile;

/// <summary>
/// Telemetry and payload returned from an authentic mobile ePassport NFC scan session.
/// Incorporates TUM SmartGuide AR antenna resonance indicators.
/// </summary>
public class NfcReadSessionResult
{
    /// <summary>
    /// Indicates whether the complete NFC chip reading succeeded.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Current terminal lifecycle status of the session.
    /// </summary>
    public NfcSessionStatus Status { get; set; } = NfcSessionStatus.Idle;

    /// <summary>
    /// Total duration of the NFC contact session in milliseconds.
    /// </summary>
    public long SessionDurationMs { get; set; }

    /// <summary>
    /// Binary content of Data Group 1 (MRZ).
    /// </summary>
    public byte[]? Dg1Bytes { get; set; }

    /// <summary>
    /// Binary content of Data Group 2 (Biometric Photo).
    /// </summary>
    public byte[]? Dg2Bytes { get; set; }

    /// <summary>
    /// Security Object Document (EF.SOD).
    /// </summary>
    public byte[]? SodBytes { get; set; }

    /// <summary>
    /// Chip active authentication challenge signature.
    /// </summary>
    public byte[]? ActiveAuthenticationSignature { get; set; }

    /// <summary>
    /// Target mobile platform on which this reading was executed.
    /// </summary>
    public MobilePlatform Platform { get; set; } = MobilePlatform.Simulator;

    /// <summary>
    /// TUM SmartGuide AR: Measured RF signal strength (dBm) between phone antenna and passport coil.
    /// Used to provide real-time haptic alignment cues to eliminate user placement errors.
    /// </summary>
    public double RfFieldStrengthDbm { get; set; } = -45.0;

    /// <summary>
    /// Diagnostic summary or error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
