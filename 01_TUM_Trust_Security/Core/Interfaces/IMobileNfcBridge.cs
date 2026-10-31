using System;
using System.Threading;
using System.Threading.Tasks;
using Tum.Trust.Security.Core.Models.Mobile;

namespace Tum.Trust.Security.Core.Interfaces;

/// <summary>
/// Cross-platform mobile NFC bridge contract powering native smartphone ePassport reading
/// across iOS (CoreNFC), Android (NfcAdapter), Flutter, and React Native.
/// </summary>
public interface IMobileNfcBridge
{
    /// <summary>
    /// Event triggered when the NFC session progression advances (e.g., from Searching to Reading).
    /// </summary>
    event EventHandler<NfcSessionStatus>? OnSessionStatusChanged;

    /// <summary>
    /// Event triggered by TUM SmartGuide AR: Emits real-time antenna coupling resonance (dBm)
    /// to trigger micro-haptics on the user's phone for perfect chip alignment.
    /// </summary>
    event EventHandler<double>? OnRfResonanceChanged;

    /// <summary>
    /// Checks whether NFC hardware is physically present and enabled on the host smartphone.
    /// </summary>
    Task<bool> IsNfcAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a secure mobile NFC scanning session using MRZ entropy (Document Number, DOB, Expiry Date)
    /// to establish BAC/PACE secure messaging and download passport Data Groups.
    /// </summary>
    Task<NfcReadSessionResult> StartPassportScanSessionAsync(
        string documentNumber, 
        string dateOfBirth, 
        string dateOfExpiry, 
        CancellationToken cancellationToken = default);
}
