using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Tum.Trust.KioskService.Recovery;
using Tum.Trust.Security.Core.Models.Hardware;
using Tum.Trust.Security.Infrastructure.Hardware;
using Xunit;

namespace Tum.Trust.Security.Tests;

/// <summary>
/// Unit and resilience tests for the Kiosk USB Auto-Recovery Engine.
/// Validates automatic detection and restoration when USB cables are disconnected and re-attached.
/// </summary>
public class KioskUsbAutoRecoveryTests
{
    private readonly PcScSmartcardReaderAdapter _adapter;
    private readonly UsbAutoRecoveryEngine _engine;
    private const string TargetReader = "ACS ACR1252U Contactless NFC Reader 0";

    public KioskUsbAutoRecoveryTests()
    {
        _adapter = new PcScSmartcardReaderAdapter();
        _engine = new UsbAutoRecoveryEngine(NullLogger<UsbAutoRecoveryEngine>.Instance, _adapter);
    }

    [Fact]
    public void InitialState_SupervisesDefaultCertifiedPeripherals()
    {
        var snapshot = _engine.GetHealthSnapshot();

        Assert.NotNull(snapshot);
        Assert.True(snapshot.Count >= 3);
        Assert.Contains(snapshot, d => d.DeviceName == TargetReader && d.State == PeripheralConnectionState.Online);
        Assert.Contains(snapshot, d => d.DeviceType == ReaderType.OpticalPassportScanner);
    }

    [Fact]
    public void SimulateUsbDisconnect_TransitionsStateToUnplugged()
    {
        bool statusChangedFired = false;
        PeripheralRecoveryEventArgs? receivedArgs = null;

        _engine.OnDeviceStatusChanged += (s, e) =>
        {
            statusChangedFired = true;
            receivedArgs = e;
        };

        // Act
        _engine.SimulateUsbDisconnect(TargetReader);

        // Assert
        Assert.True(statusChangedFired);
        Assert.NotNull(receivedArgs);
        Assert.Equal(PeripheralConnectionState.Unplugged, receivedArgs.CurrentState);

        var snapshot = _engine.GetHealthSnapshot();
        var dev = snapshot.First(d => d.DeviceName == TargetReader);
        Assert.Equal(PeripheralConnectionState.Unplugged, dev.State);
        Assert.Equal(1, dev.DisconnectCount);
    }

    [Fact]
    public async Task SimulateUsbReconnect_RecoversWithinSlaThreshold()
    {
        bool recoveryFired = false;
        PeripheralRecoveryEventArgs? recoveryArgs = null;

        _engine.OnRecoveryCompleted += (s, e) =>
        {
            recoveryFired = true;
            recoveryArgs = e;
        };

        // 1. Disconnect
        _engine.SimulateUsbDisconnect(TargetReader);

        // 2. Reconnect
        var sw = Stopwatch.StartNew();
        _engine.SimulateUsbReconnect(TargetReader);
        sw.Stop();

        // Allow any background callbacks
        await Task.Delay(50);

        // Assert
        Assert.True(recoveryFired, "RecoveryCompleted event must fire upon USB re-attachment.");
        Assert.NotNull(recoveryArgs);
        Assert.Equal(PeripheralConnectionState.Online, recoveryArgs.CurrentState);
        Assert.True(sw.ElapsedMilliseconds < 1500, $"Recovery took {sw.ElapsedMilliseconds}ms, exceeding 1500ms SLA.");

        var snapshot = _engine.GetHealthSnapshot();
        var dev = snapshot.First(d => d.DeviceName == TargetReader);
        Assert.Equal(PeripheralConnectionState.Online, dev.State);
        Assert.Equal(1, dev.SuccessfulRecoveryCount);
    }

    [Fact]
    public void ReportFailure_TransitionsDeviceToUnpluggedAndTriggersRetry()
    {
        string failingReader = "Unplugged_Reader_99";
        var ex = new InvalidOperationException("SCARD_E_COMM_DATA_LOST: USB token communication interrupted.");
        _engine.ReportFailure(failingReader, ex);

        var snapshot = _engine.GetHealthSnapshot();
        var dev = snapshot.First(d => d.DeviceName == failingReader);

        Assert.Equal(1, dev.DisconnectCount);
        Assert.NotNull(dev.LastError);
        Assert.Contains("SCARD_E_COMM_DATA_LOST", dev.LastError);
    }
}
