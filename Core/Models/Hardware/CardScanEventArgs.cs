using System;

namespace Tum.Trust.Security.Core.Models.Hardware;

/// <summary>
/// Event arguments delivered when a physical smartcard or ePassport is presented to a hardware reader.
/// </summary>
public class CardScanEventArgs : EventArgs
{
    /// <summary>
    /// The name of the reader device that captured the document.
    /// </summary>
    public string ReaderName { get; set; } = string.Empty;

    /// <summary>
    /// Answer-to-Reset (ATR) or historical bytes from the contactless chip.
    /// </summary>
    public byte[]? AtrBytes { get; set; }

    /// <summary>
    /// Optical or NFC Data Group 1 (MRZ) bytes, if extracted by the reader hardware.
    /// </summary>
    public byte[]? Dg1Bytes { get; set; }

    /// <summary>
    /// NFC Data Group 2 (Facial image) raw bytes, if extracted.
    /// </summary>
    public byte[]? Dg2Bytes { get; set; }

    /// <summary>
    /// Security Object Document (EF.SOD) raw bytes.
    /// </summary>
    public byte[]? SodBytes { get; set; }

    /// <summary>
    /// UTC timestamp of the scan event.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
