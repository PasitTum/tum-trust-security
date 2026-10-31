using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tum.Trust.Security.Application;
using Tum.Trust.Security.Core.Exceptions;
using Tum.Trust.Security.Core.Interfaces;
using Tum.Trust.Security.Core.Models.Hardware;
using Tum.Trust.Security.Infrastructure.Hardware;
using Xunit;

namespace Tum.Trust.Security.Tests;

public class ThaiIdSmartCardReaderTests
{
    private readonly PcScSmartcardReaderAdapter _adapter;
    private readonly ThaiIdSmartCardReader _reader;
    private const string TestReaderName = "ACS ACR1252U Contactless NFC Reader 0";

    public ThaiIdSmartCardReaderTests()
    {
        _adapter = new PcScSmartcardReaderAdapter();
        _reader = new ThaiIdSmartCardReader(_adapter);
    }

    [Fact]
    public async Task IsThaiIdCardPresentAsync_WhenReaderConnected_ReturnsTrue()
    {
        // Act
        var isPresent = await _reader.IsThaiIdCardPresentAsync(TestReaderName);

        // Assert
        Assert.True(isPresent);
    }

    [Fact]
    public async Task IsThaiIdCardPresentAsync_WithInvalidReader_ReturnsFalse()
    {
        // Act
        var isPresent = await _reader.IsThaiIdCardPresentAsync("NonExistentReader");

        // Assert
        Assert.False(isPresent);
    }

    [Fact]
    public async Task IsThaiIdCardPresentAsync_WithNullOrEmptyReader_ReturnsFalse()
    {
        Assert.False(await _reader.IsThaiIdCardPresentAsync(""));
        Assert.False(await _reader.IsThaiIdCardPresentAsync(null!));
    }

    [Fact]
    public async Task ReadThaiIdCardAsync_ReturnsValidDemographicsAndChecksum()
    {
        // Act
        var cardData = await _reader.ReadThaiIdCardAsync(TestReaderName);

        // Assert
        Assert.NotNull(cardData);
        Assert.Equal("1100500123451", cardData.CitizenId);
        Assert.Equal("นาย", cardData.TitleTh);
        Assert.Equal("สมชาย", cardData.FirstNameTh);
        Assert.Equal("ใจดี", cardData.LastNameTh);
        Assert.Equal("นาย สมชาย ใจดี", cardData.FullNameTh);

        Assert.Equal("Mr.", cardData.TitleEn);
        Assert.Equal("Somchai", cardData.FirstNameEn);
        Assert.Equal("Jaidee", cardData.LastNameEn);
        Assert.Equal("Mr. Somchai Jaidee", cardData.FullNameEn);

        Assert.Equal("25350512", cardData.DateOfBirth);
        Assert.Equal("1", cardData.Gender);
        Assert.Contains("กรุงเทพมหานคร", cardData.Address);
        Assert.Equal("25650115", cardData.IssueDate);
        Assert.Equal("25730512", cardData.ExpirationDate);
        Assert.Equal("สำนักบริหารการทะเบียน กรมการปกครอง", cardData.Issuer);

        // Verify PII is masked
        Assert.DoesNotContain("50012", cardData.MaskedCitizenId);
        Assert.StartsWith("1", cardData.MaskedCitizenId);
        Assert.EndsWith("3451", cardData.MaskedCitizenId);
    }

    [Fact]
    public async Task ReadThaiIdCardAsync_WithPhoto_ReturnsPhotoBytes()
    {
        // Act
        var cardData = await _reader.ReadThaiIdCardAsync(TestReaderName, includePhoto: true);

        // Assert
        Assert.NotNull(cardData.PhotoBytes);
        Assert.True(cardData.PhotoBytes.Length > 4);
        // Verify JPEG magic bytes FF D8
        Assert.Equal(0xFF, cardData.PhotoBytes[0]);
        Assert.Equal(0xD8, cardData.PhotoBytes[1]);
    }

    [Fact]
    public async Task ReadPhotoAsync_ReturnsPhotoBytesDirectly()
    {
        // Act
        var photo = await _reader.ReadPhotoAsync(TestReaderName);

        // Assert
        Assert.NotNull(photo);
        Assert.Equal(0xFF, photo[0]);
        Assert.Equal(0xD8, photo[1]);
    }

    [Fact]
    public async Task ReadThaiIdCardAsync_WithEmptyReader_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _reader.ReadThaiIdCardAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _reader.ReadThaiIdCardAsync(null!));
    }

    [Fact]
    public async Task ReadThaiIdCardAsync_WhenAppletSelectionFails_ThrowsVerificationException()
    {
        // Arrange: Custom handler simulates fail status word for SELECT APDU
        _adapter.SetCustomApduHandler((reader, cmd) =>
        {
            if (cmd[0] == 0x00 && cmd[1] == 0xA4)
            {
                return new byte[] { 0x6A, 0x82 }; // File/Applet not found
            }
            return null;
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<VerificationException>(() => _reader.ReadThaiIdCardAsync(TestReaderName));
        Assert.Equal("SMARTCARD_SELECTION_FAILED", ex.ErrorCode);
    }

    [Fact]
    public async Task ReadThaiIdCardAsync_WhenCidChecksumInvalid_ThrowsIdentityValidationException_WithMaskedPid()
    {
        // Arrange: Inject invalid citizen ID checksum
        _adapter.SetCustomApduHandler((reader, cmd) =>
        {
            if (cmd[0] == 0x80 && cmd[1] == 0xB0 && cmd[3] == 0x04)
            {
                byte[] badCid = System.Text.Encoding.ASCII.GetBytes("1100500123459"); // Invalid modulo 11
                byte[] res = new byte[badCid.Length + 2];
                Buffer.BlockCopy(badCid, 0, res, 0, badCid.Length);
                res[^2] = 0x90;
                res[^1] = 0x00;
                return res;
            }
            return null;
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<IdentityValidationException>(() => _reader.ReadThaiIdCardAsync(TestReaderName));
        Assert.Equal("INVALID_CITIZEN_ID_CHECKSUM", ex.ErrorCode);
        Assert.DoesNotContain("1100500123459", ex.Message);
        Assert.Contains("1********3459", ex.Message);
    }

    [Fact]
    public async Task ReadThaiIdCardAsync_Performance_Sub800msLatency()
    {
        // Act
        var sw = Stopwatch.StartNew();
        var cardData = await _reader.ReadThaiIdCardAsync(TestReaderName, includePhoto: true);
        sw.Stop();

        // Assert
        Assert.NotNull(cardData);
        Assert.True(sw.ElapsedMilliseconds < 800, $"Expected latency < 800ms, actual was {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task WaitForCardAndReadAsync_WhenCardAlreadyPresent_ReturnsImmediately()
    {
        // Act
        var cardData = await _reader.WaitForCardAndReadAsync(TestReaderName, TimeSpan.FromSeconds(2));

        // Assert
        Assert.NotNull(cardData);
        Assert.Equal("1100500123451", cardData.CitizenId);
    }

    [Fact]
    public async Task WaitForCardAndReadAsync_WhenCardInsertedLater_CompletesSuccessfully()
    {
        const string DelayedReader = "HID Global OMNIKEY 5422 CL 0";
        bool appletEnabled = false;

        // Simulate card not present initially, then inserted after 50ms
        _adapter.SetCustomApduHandler((reader, cmd) =>
        {
            if (!appletEnabled && cmd[0] == 0x00 && cmd[1] == 0xA4)
            {
                return new byte[] { 0x6E, 0x00 }; // CLA not supported / No card
            }
            return null;
        });

        var waitTask = _reader.WaitForCardAndReadAsync(DelayedReader, TimeSpan.FromSeconds(3));

        // Fire card insertion event after slight delay
        await Task.Delay(50);
        appletEnabled = true;
        _adapter.SimulateCardInsertion(DelayedReader);

        var cardData = await waitTask;

        // Assert
        Assert.NotNull(cardData);
        Assert.Equal("1100500123451", cardData.CitizenId);
    }

    [Fact]
    public async Task WaitForCardAndReadAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        const string TargetReader = "Regula 7024 Full-Page Passport Reader 0";
        _adapter.SetCustomApduHandler((reader, cmd) => new byte[] { 0x6E, 0x00 });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _reader.WaitForCardAndReadAsync(TargetReader, TimeSpan.FromSeconds(5), cancellationToken: cts.Token));
    }

    [Fact]
    public async Task OnThaiIdCardRead_EventFiresWhenCardRead()
    {
        ThaiIdCardData? receivedData = null;
        _reader.OnThaiIdCardRead += (sender, data) => receivedData = data;

        await _reader.ReadThaiIdCardAsync(TestReaderName);

        Assert.NotNull(receivedData);
        Assert.Equal("1100500123451", receivedData.CitizenId);
    }

    [Fact]
    public void NativePcScInterop_IsPcscServiceAvailable_DoesNotThrow()
    {
        // Safe interop invocation
        bool isAvailable = NativePcScInterop.IsPcscServiceAvailable();
        // Returns boolean without unhandled access violation or crash
        Assert.True(isAvailable || !isAvailable);
    }

    [Fact]
    public void DependencyInjection_RegistersIThaiIdSmartCardReader_ResolvesProperly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTumTrustSecurity();

        using var sp = services.BuildServiceProvider();
        var reader = sp.GetService<IThaiIdSmartCardReader>();

        Assert.NotNull(reader);
        Assert.IsType<ThaiIdSmartCardReader>(reader);
    }
}
