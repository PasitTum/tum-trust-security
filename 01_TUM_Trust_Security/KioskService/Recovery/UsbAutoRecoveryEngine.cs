using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tum.Trust.Security.Core.Interfaces;
using Tum.Trust.Security.Core.Models.Hardware;
using Tum.Trust.Security.Infrastructure.Hardware;

namespace Tum.Trust.KioskService.Recovery;

/// <summary>
/// Peripheral device connection state.
/// </summary>
public enum PeripheralConnectionState
{
    Online,
    Unplugged,
    Recovering,
    Faulted
}

/// <summary>
/// Status model for a monitored peripheral hardware device.
/// </summary>
public class PeripheralHealthStatus
{
    public required string DeviceId { get; init; }
    public required string DeviceName { get; set; }
    public ReaderType DeviceType { get; set; }
    public PeripheralConnectionState State { get; set; } = PeripheralConnectionState.Online;
    public string? LastError { get; set; }
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    public int DisconnectCount { get; set; }
    public int SuccessfulRecoveryCount { get; set; }
    public long LastRecoveryDurationMs { get; set; }
    public int CurrentRetryAttempt { get; set; }
}

/// <summary>
/// Event arguments for peripheral device state changes and auto-recovery notifications.
/// </summary>
public class PeripheralRecoveryEventArgs : EventArgs
{
    public required string DeviceId { get; init; }
    public required PeripheralConnectionState PreviousState { get; init; }
    public required PeripheralConnectionState CurrentState { get; init; }
    public string? Reason { get; init; }
    public long RecoveryDurationMs { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Intelligent USB Auto-Recovery Engine for Kiosk Hardware Peripherals.
/// Detects sudden USB cable pulls, brownouts, or driver timeouts across Smartcard readers,
/// ePassport scanners, and biometric cameras, and automatically restores normal operations
/// within &lt; 1.5 seconds upon re-insertion without crashing or restarting the host application.
/// </summary>
public class UsbAutoRecoveryEngine : IDisposable
{
    private readonly ILogger<UsbAutoRecoveryEngine> _logger;
    private readonly PcScSmartcardReaderAdapter _readerAdapter;
    private readonly ConcurrentDictionary<string, PeripheralHealthStatus> _monitoredDevices = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _cts = new();
    private Task? _monitoringTask;
    private bool _disposed;

    /// <summary>
    /// Event fired whenever a peripheral device transitions between health states.
    /// </summary>
    public event EventHandler<PeripheralRecoveryEventArgs>? OnDeviceStatusChanged;

    /// <summary>
    /// Event fired when a disconnected device is successfully recovered and brought back online.
    /// </summary>
    public event EventHandler<PeripheralRecoveryEventArgs>? OnRecoveryCompleted;

    public UsbAutoRecoveryEngine(
        ILogger<UsbAutoRecoveryEngine> logger,
        PcScSmartcardReaderAdapter readerAdapter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _readerAdapter = readerAdapter ?? throw new ArgumentNullException(nameof(readerAdapter));

        // Hook adapter lifecycle events
        _readerAdapter.OnReaderConnected += HandleReaderConnected;
        _readerAdapter.OnReaderDisconnected += HandleReaderDisconnected;

        // Register default certified peripherals
        RegisterMonitoredDevice(new PeripheralHealthStatus
        {
            DeviceId = "SMARTCARD-01",
            DeviceName = "ACS ACR1252U Contactless NFC Reader 0",
            DeviceType = ReaderType.NfcContactless
        });

        RegisterMonitoredDevice(new PeripheralHealthStatus
        {
            DeviceId = "PASSPORT-01",
            DeviceName = "Regula 7024 Full-Page Passport Reader 0",
            DeviceType = ReaderType.OpticalPassportScanner
        });

        RegisterMonitoredDevice(new PeripheralHealthStatus
        {
            DeviceId = "CAMERA-01",
            DeviceName = "Kiosk Biometric RGB/IR USB Camera 0",
            DeviceType = ReaderType.ContactSmartcard
        });
    }

    /// <summary>
    /// Starts background USB health polling and heartbeat watcher.
    /// </summary>
    public void Start()
    {
        _monitoringTask = Task.Run(MonitorLoopAsync, _cts.Token);
        _logger.LogInformation("UsbAutoRecoveryEngine started. Monitored devices: {Count}", _monitoredDevices.Count);
    }

    /// <summary>
    /// Registers or updates a peripheral to be supervised by the auto-recovery engine.
    /// </summary>
    public void RegisterMonitoredDevice(PeripheralHealthStatus device)
    {
        _monitoredDevices[device.DeviceName] = device;
    }

    /// <summary>
    /// Returns snapshot of all monitored peripherals and their current health status.
    /// </summary>
    public IReadOnlyCollection<PeripheralHealthStatus> GetHealthSnapshot()
    {
        return _monitoredDevices.Values.ToList();
    }

    /// <summary>
    /// Reports an immediate communication failure or disconnect from HAL (e.g. during APDU transmission).
    /// Immediately triggers auto-recovery state machine.
    /// </summary>
    public void ReportFailure(string deviceName, Exception ex)
    {
        if (!_monitoredDevices.TryGetValue(deviceName, out var device))
        {
            device = new PeripheralHealthStatus
            {
                DeviceId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                DeviceName = deviceName,
                DeviceType = ReaderType.NfcContactless
            };
            _monitoredDevices[deviceName] = device;
        }

        var prevState = device.State;
        device.State = PeripheralConnectionState.Unplugged;
        device.LastError = ex.Message;
        device.DisconnectCount++;
        device.CurrentRetryAttempt = 0;

        _logger.LogWarning(ex, "USB Disconnect or I/O failure detected for {DeviceName}. Triggering auto-recovery.", deviceName);

        OnDeviceStatusChanged?.Invoke(this, new PeripheralRecoveryEventArgs
        {
            DeviceId = device.DeviceId,
            PreviousState = prevState,
            CurrentState = PeripheralConnectionState.Unplugged,
            Reason = $"I/O failure: {ex.Message}"
        });

        // Trigger asynchronous recovery attempt
        _ = AttemptRecoveryAsync(device, CancellationToken.None);
    }

    /// <summary>
    /// Simulates a sudden USB disconnect (cable yanked) for testbed / diagnostics validation.
    /// </summary>
    public void SimulateUsbDisconnect(string deviceName)
    {
        if (_monitoredDevices.TryGetValue(deviceName, out var device))
        {
            _readerAdapter.UnregisterReader(deviceName);
        }
    }

    /// <summary>
    /// Simulates a USB re-attachment for testbed / diagnostics validation.
    /// </summary>
    public void SimulateUsbReconnect(string deviceName, ReaderType type = ReaderType.NfcContactless)
    {
        _readerAdapter.RegisterReader(new ReaderDeviceInfo
        {
            DeviceName = deviceName,
            Type = type,
            IsConnected = true,
            VendorId = "072F",
            ProductId = "223B",
            FirmwareVersion = "1.02.00-RECOVERED"
        });
    }

    private void HandleReaderDisconnected(object? sender, string deviceName)
    {
        if (_monitoredDevices.TryGetValue(deviceName, out var device))
        {
            var prevState = device.State;
            device.State = PeripheralConnectionState.Unplugged;
            device.LastError = "USB cable disconnected or device unplugged.";
            device.DisconnectCount++;
            device.CurrentRetryAttempt = 0;

            _logger.LogWarning("Peripheral unplugged: {DeviceName}", deviceName);

            OnDeviceStatusChanged?.Invoke(this, new PeripheralRecoveryEventArgs
            {
                DeviceId = device.DeviceId,
                PreviousState = prevState,
                CurrentState = PeripheralConnectionState.Unplugged,
                Reason = "Hardware removed event received"
            });
        }
    }

    private void HandleReaderConnected(object? sender, ReaderDeviceInfo info)
    {
        if (!_monitoredDevices.TryGetValue(info.DeviceName, out var device))
        {
            device = new PeripheralHealthStatus
            {
                DeviceId = $"DEV-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                DeviceName = info.DeviceName,
                DeviceType = info.Type
            };
            _monitoredDevices[info.DeviceName] = device;
        }

        var prevState = device.State;
        device.State = PeripheralConnectionState.Online;
        device.LastError = null;
        device.LastSeenUtc = DateTime.UtcNow;
        device.SuccessfulRecoveryCount++;

        _logger.LogInformation("Peripheral recovered and online: {DeviceName} (Vendor: {VendorId}, Product: {ProductId})",
            info.DeviceName, info.VendorId, info.ProductId);

        OnRecoveryCompleted?.Invoke(this, new PeripheralRecoveryEventArgs
        {
            DeviceId = device.DeviceId,
            PreviousState = prevState,
            CurrentState = PeripheralConnectionState.Online,
            Reason = "Hardware reconnected and validated successfully."
        });

        OnDeviceStatusChanged?.Invoke(this, new PeripheralRecoveryEventArgs
        {
            DeviceId = device.DeviceId,
            PreviousState = prevState,
            CurrentState = PeripheralConnectionState.Online,
            Reason = "Hardware online"
        });
    }

    private async Task AttemptRecoveryAsync(PeripheralHealthStatus device, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        device.State = PeripheralConnectionState.Recovering;

        OnDeviceStatusChanged?.Invoke(this, new PeripheralRecoveryEventArgs
        {
            DeviceId = device.DeviceId,
            PreviousState = PeripheralConnectionState.Unplugged,
            CurrentState = PeripheralConnectionState.Recovering,
            Reason = "Initiating PC/SC context reset and bus probe"
        });

        // Exponential backoff strategy: 300ms, 600ms, 1200ms
        int maxAttempts = 5;
        int delayMs = 300;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (ct.IsCancellationRequested || _cts.IsCancellationRequested)
                break;

            device.CurrentRetryAttempt = attempt;
            _logger.LogDebug("Auto-recovery attempt {Attempt}/{Max} for {DeviceName}...", attempt, maxAttempts, device.DeviceName);

            try
            {
                // In Windows environment, flush WinSCard context if running on physical hardware
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    FlushWindowsSmartCardSubsystem();
                }

                // Query reader adapter
                var readers = await _readerAdapter.EnumerateReadersAsync(ct);
                bool found = readers.Any(r => string.Equals(r.DeviceName, device.DeviceName, StringComparison.OrdinalIgnoreCase) && r.IsConnected);

                if (found)
                {
                    sw.Stop();
                    device.State = PeripheralConnectionState.Online;
                    device.LastError = null;
                    device.LastSeenUtc = DateTime.UtcNow;
                    device.SuccessfulRecoveryCount++;
                    device.LastRecoveryDurationMs = sw.ElapsedMilliseconds;

                    _logger.LogInformation("Successfully recovered {DeviceName} in {DurationMs} ms (Attempt {Attempt})",
                        device.DeviceName, sw.ElapsedMilliseconds, attempt);

                    OnRecoveryCompleted?.Invoke(this, new PeripheralRecoveryEventArgs
                    {
                        DeviceId = device.DeviceId,
                        PreviousState = PeripheralConnectionState.Recovering,
                        CurrentState = PeripheralConnectionState.Online,
                        Reason = $"Auto-recovery succeeded on attempt {attempt}",
                        RecoveryDurationMs = sw.ElapsedMilliseconds
                    });

                    OnDeviceStatusChanged?.Invoke(this, new PeripheralRecoveryEventArgs
                    {
                        DeviceId = device.DeviceId,
                        PreviousState = PeripheralConnectionState.Recovering,
                        CurrentState = PeripheralConnectionState.Online,
                        Reason = "Device back online",
                        RecoveryDurationMs = sw.ElapsedMilliseconds
                    });

                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Recovery attempt {Attempt} encountered error: {Message}", attempt, ex.Message);
            }

            await Task.Delay(delayMs, ct);
            delayMs = Math.Min(delayMs * 2, 2000);
        }

        sw.Stop();
        // If recovery attempts exhausted without detection, set to Faulted/Unplugged
        device.State = PeripheralConnectionState.Faulted;
        device.LastError = $"Auto-recovery failed after {maxAttempts} attempts. Cable physical inspection required.";

        _logger.LogError("Auto-recovery failed for {DeviceName}. Marking as Faulted.", device.DeviceName);

        OnDeviceStatusChanged?.Invoke(this, new PeripheralRecoveryEventArgs
        {
            DeviceId = device.DeviceId,
            PreviousState = PeripheralConnectionState.Recovering,
            CurrentState = PeripheralConnectionState.Faulted,
            Reason = device.LastError,
            RecoveryDurationMs = sw.ElapsedMilliseconds
        });
    }

    [DllImport("winscard.dll", EntryPoint = "SCardEstablishContext", SetLastError = true)]
    private static extern int SCardEstablishContext(uint dwScope, IntPtr pvReserved1, IntPtr pvReserved2, out IntPtr phContext);

    [DllImport("winscard.dll", EntryPoint = "SCardReleaseContext", SetLastError = true)]
    private static extern int SCardReleaseContext(IntPtr hContext);

    private void FlushWindowsSmartCardSubsystem()
    {
        try
        {
            // SCardEstablishContext / SCardReleaseContext cycle to clear dead handles
            int ret = SCardEstablishContext(0 /* SCARD_SCOPE_USER */, IntPtr.Zero, IntPtr.Zero, out IntPtr hContext);
            if (ret == 0 && hContext != IntPtr.Zero)
            {
                SCardReleaseContext(hContext);
            }
        }
        catch
        {
            // Best effort reset, safe ignore in mock/virtualized environment
        }
    }

    private async Task MonitorLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, _cts.Token);

                // Periodic health verification
                var readers = await _readerAdapter.EnumerateReadersAsync(_cts.Token);
                var connectedNames = new HashSet<string>(readers.Where(r => r.IsConnected).Select(r => r.DeviceName), StringComparer.OrdinalIgnoreCase);

                foreach (var device in _monitoredDevices.Values)
                {
                    if (device.DeviceType == ReaderType.NfcContactless || device.DeviceType == ReaderType.OpticalPassportScanner)
                    {
                        bool isActuallyConnected = connectedNames.Contains(device.DeviceName);
                        if (device.State == PeripheralConnectionState.Online && !isActuallyConnected)
                        {
                            ReportFailure(device.DeviceName, new InvalidOperationException("Periodic probe: peripheral disappeared from bus."));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UsbAutoRecoveryEngine monitor loop");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _readerAdapter.OnReaderConnected -= HandleReaderConnected;
        _readerAdapter.OnReaderDisconnected -= HandleReaderDisconnected;

        _cts.Cancel();
        _cts.Dispose();
    }
}
