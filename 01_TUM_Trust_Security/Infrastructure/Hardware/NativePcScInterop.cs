using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Tum.Trust.Security.Infrastructure.Hardware;

/// <summary>
/// Cross-platform native bindings for PC/SC (winscard.dll / libpcsclite.so.1).
/// Provides low-level hardware communication for smart card readers on physical kiosks.
/// </summary>
public static class NativePcScInterop
{
    private const string WinSCardDll = "winscard.dll";

    /// <summary>Scope user context</summary>
    public const uint SCARD_SCOPE_USER = 0;
    /// <summary>Scope system context</summary>
    public const uint SCARD_SCOPE_SYSTEM = 2;

    /// <summary>Shared card mode</summary>
    public const uint SCARD_SHARE_SHARED = 2;
    /// <summary>Exclusive card mode</summary>
    public const uint SCARD_SHARE_EXCLUSIVE = 1;
    /// <summary>Direct card mode</summary>
    public const uint SCARD_SHARE_DIRECT = 3;

    /// <summary>T=0 transmission protocol</summary>
    public const uint SCARD_PROTOCOL_T0 = 0x00000001;
    /// <summary>T=1 transmission protocol</summary>
    public const uint SCARD_PROTOCOL_T1 = 0x00000002;
    /// <summary>Raw transmission protocol</summary>
    public const uint SCARD_PROTOCOL_RAW = 0x00000004;

    /// <summary>Card disposition: leave</summary>
    public const uint SCARD_LEAVE_CARD = 0;
    /// <summary>Card disposition: reset</summary>
    public const uint SCARD_RESET_CARD = 1;
    /// <summary>Card disposition: unpower</summary>
    public const uint SCARD_UNPOWER_CARD = 2;
    /// <summary>Card disposition: eject</summary>
    public const uint SCARD_EJECT_CARD = 3;

    /// <summary>State unaware</summary>
    public const uint SCARD_STATE_UNAWARE = 0x00000000;
    /// <summary>State ignore</summary>
    public const uint SCARD_STATE_IGNORE = 0x00000001;
    /// <summary>State changed</summary>
    public const uint SCARD_STATE_CHANGED = 0x00000002;
    /// <summary>State unknown</summary>
    public const uint SCARD_STATE_UNKNOWN = 0x00000004;
    /// <summary>State unavailable</summary>
    public const uint SCARD_STATE_UNAVAILABLE = 0x00000008;
    /// <summary>State empty (no card)</summary>
    public const uint SCARD_STATE_EMPTY = 0x00000010;
    /// <summary>State present (card inserted)</summary>
    public const uint SCARD_STATE_PRESENT = 0x00000020;
    /// <summary>State in use</summary>
    public const uint SCARD_STATE_INUSE = 0x00000100;

    /// <summary>Success return code</summary>
    public const uint SCARD_S_SUCCESS = 0;

    /// <summary>
    /// PC/SC IO Request structure for SCardTransmit.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SCARD_IO_REQUEST
    {
        /// <summary>Protocol in use (T=0 or T=1)</summary>
        public uint dwProtocol;
        /// <summary>Length of PCI structure</summary>
        public uint cbPciLength;
    }

    /// <summary>
    /// PC/SC Reader State structure for SCardGetStatusChange.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct SCARD_READERSTATE
    {
        /// <summary>Name of reader</summary>
        public string szReader;
        /// <summary>User data</summary>
        public IntPtr pvUserData;
        /// <summary>Current state</summary>
        public uint dwCurrentState;
        /// <summary>Event state</summary>
        public uint dwEventState;
        /// <summary>Length of ATR</summary>
        public uint cbAtr;
        /// <summary>ATR byte buffer</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        public byte[] rgbAtr;
    }

    // Windows WinSCard P/Invoke definitions
    [DllImport(WinSCardDll, EntryPoint = "SCardEstablishContext", SetLastError = true)]
    internal static extern int Win_SCardEstablishContext(uint dwScope, IntPtr pvReserved1, IntPtr pvReserved2, out IntPtr phContext);

    [DllImport(WinSCardDll, EntryPoint = "SCardReleaseContext", SetLastError = true)]
    internal static extern int Win_SCardReleaseContext(IntPtr hContext);

    [DllImport(WinSCardDll, EntryPoint = "SCardListReadersA", SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern int Win_SCardListReaders(IntPtr hContext, string? mszGroups, byte[]? mszReaders, ref uint pcchReaders);

    [DllImport(WinSCardDll, EntryPoint = "SCardConnectA", SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern int Win_SCardConnect(IntPtr hContext, string szReader, uint dwShareMode, uint dwPreferredProtocols, out IntPtr phCard, out uint pdwActiveProtocol);

    [DllImport(WinSCardDll, EntryPoint = "SCardDisconnect", SetLastError = true)]
    internal static extern int Win_SCardDisconnect(IntPtr hCard, uint dwDisposition);

    [DllImport(WinSCardDll, EntryPoint = "SCardTransmit", SetLastError = true)]
    internal static extern int Win_SCardTransmit(IntPtr hCard, ref SCARD_IO_REQUEST pioSendPci, byte[] pbSendBuffer, uint cbSendLength, IntPtr pioRecvPci, byte[] pbRecvBuffer, ref uint pcbRecvLength);

    [DllImport(WinSCardDll, EntryPoint = "SCardGetStatusChangeA", SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern int Win_SCardGetStatusChange(IntPtr hContext, uint dwTimeout, [In, Out] SCARD_READERSTATE[] rgReaderStates, uint cReaders);

    /// <summary>
    /// Checks if the PC/SC Smart Card service (WinSCard or pcscd) is available on this system.
    /// Safely returns false if not supported or disabled.
    /// </summary>
    public static bool IsPcscServiceAvailable()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                int ret = Win_SCardEstablishContext(SCARD_SCOPE_USER, IntPtr.Zero, IntPtr.Zero, out IntPtr hContext);
                if (ret == 0 && hContext != IntPtr.Zero)
                {
                    Win_SCardReleaseContext(hContext);
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
