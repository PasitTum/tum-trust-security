namespace Tum.Trust.Security.Core.Models.Mobile;

/// <summary>
/// Execution state of a mobile NFC ePassport scanning session.
/// </summary>
public enum NfcSessionStatus
{
    /// <summary>
    /// Waiting for user activation.
    /// </summary>
    Idle = 0,

    /// <summary>
    /// Polling for ISO 14443 contactless tag in RF field.
    /// </summary>
    SearchingTag = 1,

    /// <summary>
    /// Performing Basic Access Control (BAC) or PACE key exchange.
    /// </summary>
    EstablishingSecureChannel = 2,

    /// <summary>
    /// Reading Logical Data Groups (DG1, DG2, DG14, DG15).
    /// </summary>
    ReadingDataGroups = 3,

    /// <summary>
    /// Performing Active Authentication challenge to defeat cloned chips.
    /// </summary>
    ActiveAuthenticating = 4,

    /// <summary>
    /// Session concluded successfully.
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Session aborted or tag connection lost.
    /// </summary>
    Failed = 6
}
