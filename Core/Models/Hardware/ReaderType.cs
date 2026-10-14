namespace Tum.Trust.Security.Core.Models.Hardware;

/// <summary>
/// Categories of identity and biometric hardware readers supported by TUM Trust Security.
/// </summary>
public enum ReaderType
{
    /// <summary>
    /// ISO/IEC 7816 contact smartcard reader (e.g., Thai National ID Card).
    /// </summary>
    ContactSmartcard = 1,

    /// <summary>
    /// ISO/IEC 14443 Type A/B Contactless NFC reader (e.g., ACS ACR122U/ACR1252U, HID Omnikey).
    /// </summary>
    NfcContactless = 2,

    /// <summary>
    /// Full-page optical and infrared ePassport scanner (e.g., Regula 70X4, ARH Combo Smart, Desko).
    /// </summary>
    OpticalPassportScanner = 3,

    /// <summary>
    /// Virtual/software simulator for automated chaos and regression testing.
    /// </summary>
    Simulator = 99
}
