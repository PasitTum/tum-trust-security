using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tum.Trust.Security.Core.Models.Hardware;

namespace Tum.Trust.Security.Core.Interfaces;

/// <summary>
/// Hardware abstraction contract for managing physical smartcard readers, NFC contactless peripherals,
/// and full-page optical passport inspection systems.
/// </summary>
public interface IHardwareReaderManager
{
    /// <summary>
    /// Event triggered when an ePassport or smartcard is placed on/inserted into a reader.
    /// </summary>
    event EventHandler<CardScanEventArgs>? OnCardInserted;

    /// <summary>
    /// Event triggered when a smartcard or ePassport is removed from a reader.
    /// </summary>
    event EventHandler<string>? OnCardRemoved;

    /// <summary>
    /// Event triggered when a new physical reader device is plugged in via USB/Serial.
    /// </summary>
    event EventHandler<ReaderDeviceInfo>? OnReaderConnected;

    /// <summary>
    /// Event triggered when a reader device is unplugged or disconnected.
    /// </summary>
    event EventHandler<string>? OnReaderDisconnected;

    /// <summary>
    /// Discovers and enumerates all available physical and virtual smartcard/passport readers.
    /// </summary>
    Task<IReadOnlyList<ReaderDeviceInfo>> EnumerateReadersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an Application Protocol Data Unit (APDU) command to a connected card chip conforming to ISO/IEC 7816-4.
    /// </summary>
    /// <param name="readerName">Name of the target reader device.</param>
    /// <param name="apduCommand">Raw APDU command bytes (CLA, INS, P1, P2, Lc, Data, Le).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>APDU response bytes ending with 2-byte status word (SW1, SW2, e.g., 0x90 0x00 for Success).</returns>
    Task<byte[]> TransmitApduAsync(string readerName, byte[] apduCommand, CancellationToken cancellationToken = default);
}
