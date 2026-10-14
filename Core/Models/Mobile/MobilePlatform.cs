namespace Tum.Trust.Security.Core.Models.Mobile;

/// <summary>
/// Target mobile runtime platforms supported by TUM Trust Security cross-platform bridge.
/// </summary>
public enum MobilePlatform
{
    /// <summary>
    /// Apple iOS (CoreNFC Framework - iPhone 7 and newer).
    /// </summary>
    Ios = 1,

    /// <summary>
    /// Google Android (android.nfc.tech.IsoDep / NfcAdapter).
    /// </summary>
    Android = 2,

    /// <summary>
    /// Google Flutter Cross-Platform Plugin.
    /// </summary>
    Flutter = 3,

    /// <summary>
    /// Meta React Native Native Module.
    /// </summary>
    ReactNative = 4,

    /// <summary>
    /// In-memory headless simulator for CI/CD and unit testing.
    /// </summary>
    Simulator = 99
}
