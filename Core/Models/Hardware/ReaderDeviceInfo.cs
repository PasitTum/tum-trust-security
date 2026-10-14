namespace Tum.Trust.Security.Core.Models.Hardware;

/// <summary>
/// Telemetry and descriptor metadata for a connected physical or virtual hardware reader.
/// </summary>
public class ReaderDeviceInfo
{
    /// <summary>
    /// Human-readable device identifier or system driver name.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Classification type of the hardware device.
    /// </summary>
    public ReaderType Type { get; set; } = ReaderType.NfcContactless;

    /// <summary>
    /// Indicates whether the physical hardware is currently connected and responsive.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// USB Vendor ID (VID) if available.
    /// </summary>
    public string? VendorId { get; set; }

    /// <summary>
    /// USB Product ID (PID) if available.
    /// </summary>
    public string? ProductId { get; set; }

    /// <summary>
    /// Firmware revision or device driver version.
    /// </summary>
    public string? FirmwareVersion { get; set; }
}
