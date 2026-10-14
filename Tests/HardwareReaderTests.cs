using System;
using System.Threading.Tasks;
using Tum.Trust.Security.Core.Interfaces;
using Tum.Trust.Security.Core.Models.Hardware;
using Tum.Trust.Security.Infrastructure.Hardware;
using Xunit;

namespace Tum.Trust.Security.Tests;

/// <summary>
/// Unit and integration tests for PC/SC Smartcard & Passport Hardware Reader Adapter.
/// </summary>
public class HardwareReaderTests
{
    private readonly PcScSmartcardReaderAdapter _adapter = new();

    [Fact]
    public async Task EnumerateReadersAsync_DiscoversDefaultCertifiedReaders()
    {
        var readers = await _adapter.EnumerateReadersAsync();

        Assert.NotNull(readers);
        Assert.True(readers.Count >= 3);
        Assert.Contains(readers, r => r.Type == ReaderType.NfcContactless);
        Assert.Contains(readers, r => r.Type == ReaderType.OpticalPassportScanner);
    }

    [Fact]
    public async Task TransmitApduAsync_SelectApplication_ReturnsSuccessStatusWord()
    {
        // ISO 7816-4 SELECT AID command: 0x00 0xA4 0x04 0x0C
        byte[] selectAidApdu = { 0x00, 0xA4, 0x04, 0x0C, 0x07, 0xA0, 0x00, 0x00, 0x02, 0x47, 0x10, 0x01 };

        byte[] response = await _adapter.TransmitApduAsync("ACS ACR1252U Contactless NFC Reader 0", selectAidApdu);

        Assert.NotNull(response);
        Assert.Equal(2, response.Length);
        Assert.Equal(0x90, response[0]); // SW1 = 0x90
        Assert.Equal(0x00, response[1]); // SW2 = 0x00 (Success)
    }

    [Fact]
    public async Task TransmitApduAsync_ReadBinary_ReturnsPayloadWithSuccessWord()
    {
        // ISO 7816-4 READ BINARY command: 0x00 0xB0 0x00 0x00 0x04
        byte[] readBinaryApdu = { 0x00, 0xB0, 0x00, 0x00, 0x04 };

        byte[] response = await _adapter.TransmitApduAsync("ACS ACR1252U Contactless NFC Reader 0", readBinaryApdu);

        Assert.NotNull(response);
        Assert.True(response.Length >= 2);
        // Verify last 2 bytes are 0x90 0x00
        Assert.Equal(0x90, response[^2]);
        Assert.Equal(0x00, response[^1]);
    }

    [Fact]
    public async Task TransmitApduAsync_DisconnectedReader_ThrowsInvalidOperationException()
    {
        byte[] dummyApdu = { 0x00, 0xA4, 0x04, 0x00 };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _adapter.TransmitApduAsync("NonExistent_Reader_99", dummyApdu);
        });
    }

    [Fact]
    public void SimulateCardInsertion_FiresOnCardInsertedEventWithTelemetry()
    {
        bool eventFired = false;
        CardScanEventArgs? receivedArgs = null;

        _adapter.OnCardInserted += (sender, args) =>
        {
            eventFired = true;
            receivedArgs = args;
        };

        _adapter.SimulateCardInsertion("ACS ACR1252U Contactless NFC Reader 0");

        Assert.True(eventFired);
        Assert.NotNull(receivedArgs);
        Assert.Equal("ACS ACR1252U Contactless NFC Reader 0", receivedArgs.ReaderName);
        Assert.NotNull(receivedArgs.AtrBytes);
    }

    [Fact]
    public void RegisterAndUnregisterReader_TriggersLifecycleEvents()
    {
        bool connectedFired = false;
        bool disconnectedFired = false;

        _adapter.OnReaderConnected += (s, r) => connectedFired = true;
        _adapter.OnReaderDisconnected += (s, name) => disconnectedFired = true;

        var newReader = new ReaderDeviceInfo
        {
            DeviceName = "Custom Desko Penta Scanner 0",
            Type = ReaderType.OpticalPassportScanner,
            IsConnected = true
        };

        _adapter.RegisterReader(newReader);
        Assert.True(connectedFired);

        _adapter.UnregisterReader("Custom Desko Penta Scanner 0");
        Assert.True(disconnectedFired);
    }
}
