using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tum.Trust.Security.Core.Models.Mobile;
using Tum.Trust.Security.Infrastructure.Mobile;
using Xunit;

namespace Tum.Trust.Security.Tests;

/// <summary>
/// Unit and integration tests for cross-platform mobile NFC bridge and TUM SmartGuide AR.
/// </summary>
public class MobileNfcBridgeTests
{
    private readonly CrossPlatformMobileNfcBridge _bridge = new(MobilePlatform.Ios);

    [Fact]
    public async Task IsNfcAvailableAsync_ReturnsTrueForModernDevices()
    {
        bool isAvailable = await _bridge.IsNfcAvailableAsync();
        Assert.True(isAvailable);
    }

    [Fact]
    public async Task StartPassportScanSessionAsync_ProgressesThroughLifecycleStates()
    {
        var recordedStatuses = new List<NfcSessionStatus>();
        _bridge.OnSessionStatusChanged += (s, status) => recordedStatuses.Add(status);

        var result = await _bridge.StartPassportScanSessionAsync(
            documentNumber: "L898902C3",
            dateOfBirth: "740812",
            dateOfExpiry: "120415");

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(NfcSessionStatus.Completed, result.Status);
        Assert.NotNull(result.Dg1Bytes);
        Assert.NotNull(result.Dg2Bytes);
        Assert.NotNull(result.SodBytes);
        Assert.Equal(MobilePlatform.Ios, result.Platform);

        // Verify status lifecycle progression
        Assert.Contains(NfcSessionStatus.SearchingTag, recordedStatuses);
        Assert.Contains(NfcSessionStatus.EstablishingSecureChannel, recordedStatuses);
        Assert.Contains(NfcSessionStatus.ReadingDataGroups, recordedStatuses);
        Assert.Contains(NfcSessionStatus.ActiveAuthenticating, recordedStatuses);
        Assert.Contains(NfcSessionStatus.Completed, recordedStatuses);
    }

    [Fact]
    public async Task StartPassportScanSessionAsync_EmitsSmartGuideArResonanceEvents()
    {
        var recordedDbm = new List<double>();
        _bridge.OnRfResonanceChanged += (s, dbm) => recordedDbm.Add(dbm);

        var result = await _bridge.StartPassportScanSessionAsync("A12345678", "900101", "300101");

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(recordedDbm);
        Assert.True(result.RfFieldStrengthDbm > -50.0, "RF Field strength should indicate solid antenna coupling.");
    }

    [Fact]
    public async Task StartPassportScanSessionAsync_EmptyDocumentNumber_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _bridge.StartPassportScanSessionAsync(string.Empty, "900101", "300101");
        });
    }
}
