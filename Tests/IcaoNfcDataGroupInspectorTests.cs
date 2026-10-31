using System.Security.Cryptography;
using System.Text;
using Tum.Trust.Security.Core.Models.Passport;
using Tum.Trust.Security.Infrastructure.Passport;
using Xunit;

namespace Tum.Trust.Security.Tests;

public class IcaoNfcDataGroupInspectorTests
{
    private readonly IcaoNfcDataGroupInspector _inspector = new();

    [Fact]
    public void InspectDg1Mrz_MatchingDocument_ReturnsTrue()
    {
        var mrz = new MrzInfo
        {
            DocumentNumber = "AA1234567",
            DateOfBirth = "850101"
        };

        byte[] dg1 = Encoding.ASCII.GetBytes("P<THA<<SOMCHAI<<<<<<<<<<<<<<<AA12345678THA8501014M2801015<<<<<<<4");

        bool result = _inspector.InspectDg1Mrz(dg1, mrz);

        Assert.True(result);
    }

    [Fact]
    public void InspectDg1Mrz_MismatchedDocument_ReturnsFalse()
    {
        var mrz = new MrzInfo
        {
            DocumentNumber = "BB9999999",
            DateOfBirth = "850101"
        };

        byte[] dg1 = Encoding.ASCII.GetBytes("P<THA<<SOMCHAI<<<<<<<<<<<<<<<AA12345678THA8501014M2801015<<<<<<<4");

        bool result = _inspector.InspectDg1Mrz(dg1, mrz);

        Assert.False(result);
    }

    [Fact]
    public void InspectDg2Face_WithJpegPayload_ExtractsValidBiometricImage()
    {
        // Mock DG2 containing JPEG magic bytes: 0xFF, 0xD8, 0xFF, 0xE0 ...
        byte[] jpegMock = new byte[] { 0x75, 0x20, 0x7F, 0x61, 0x1C, 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00 };

        var faceInfo = _inspector.InspectDg2Face(jpegMock);

        Assert.True(faceInfo.IsValid);
        Assert.Equal("JPEG", faceInfo.ImageFormat);
        Assert.NotNull(faceInfo.ImageBytes);
        Assert.Equal(0xFF, faceInfo.ImageBytes![0]);
        Assert.Equal(0xD8, faceInfo.ImageBytes[1]);
        Assert.Equal(480, faceInfo.ImageWidth);
        Assert.Equal(640, faceInfo.ImageHeight);
    }

    [Fact]
    public void InspectDg14_ReturnsChipAuthenticationInfo()
    {
        byte[] mockDg14 = new byte[] { 0x6E, 0x10, 0x04, 0x00, 0x7F, 0x00, 0x07 };

        var chipInfo = _inspector.InspectDg14(mockDg14);

        Assert.True(chipInfo.IsPresent);
        Assert.Contains("ECDH", chipInfo.ProtocolName);
        Assert.Equal(256, chipInfo.KeyLengthBits);
    }

    [Fact]
    public void InspectDg15_ReturnsActiveAuthenticationKeyInfo()
    {
        byte[] mockDg15 = new byte[] { 0x6F, 0x20, 0x30, 0x1E, 0x02, 0x01, 0x00 };

        var aaInfo = _inspector.InspectDg15(mockDg15);

        Assert.True(aaInfo.IsPresent);
        Assert.Equal("RSA", aaInfo.KeyType);
        Assert.Equal(2048, aaInfo.ModulusLengthBits);
    }

    [Fact]
    public void VerifySodPassiveAuthentication_ValidHashes_Succeeds()
    {
        byte[] dg1 = Encoding.ASCII.GetBytes("DG1_CONTENT_MOCK_TEST_PAYLOAD");
        byte[] dg2 = Encoding.ASCII.GetBytes("DG2_CONTENT_MOCK_TEST_PAYLOAD");

        using var sha256 = SHA256.Create();
        byte[] h1 = sha256.ComputeHash(dg1);
        byte[] h2 = sha256.ComputeHash(dg2);

        // Build mock SOD containing h1 and h2
        var sodList = new List<byte>();
        sodList.AddRange(new byte[] { 0x30, 0x82, 0x01, 0x00 }); // CMS header prefix
        sodList.AddRange(h1);
        sodList.AddRange(h2);
        byte[] sodBytes = sodList.ToArray();

        var dgMap = new Dictionary<int, byte[]>
        {
            { 1, dg1 },
            { 2, dg2 }
        };

        var result = _inspector.VerifySodPassiveAuthentication(sodBytes, dgMap);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.VerifiedDataGroupsCount);
        Assert.Empty(result.Discrepancies);
        Assert.True(result.DataGroupHashMatches[1]);
        Assert.True(result.DataGroupHashMatches[2]);
    }

    [Fact]
    public void VerifySodPassiveAuthentication_TamperedDg_FailsAndReportsDiscrepancy()
    {
        byte[] dg1 = Encoding.ASCII.GetBytes("ORIGINAL_DG1");
        using var sha256 = SHA256.Create();
        byte[] h1 = sha256.ComputeHash(dg1);

        byte[] sodBytes = new byte[64];
        Array.Copy(h1, 0, sodBytes, 16, h1.Length);

        // Client supplies tampered DG1
        byte[] tamperedDg1 = Encoding.ASCII.GetBytes("TAMPERED_DG1_PAYLOAD");

        var dgMap = new Dictionary<int, byte[]>
        {
            { 1, tamperedDg1 }
        };

        var result = _inspector.VerifySodPassiveAuthentication(sodBytes, dgMap);

        Assert.False(result.IsValid);
        Assert.Contains(result.Discrepancies, d => d.Contains("Data Group 1"));
    }

    [Fact]
    public void Inspect_FullContainer_ExecutesEndToEndPipeline()
    {
        byte[] dg1 = Encoding.ASCII.GetBytes("P<THA<<SOMCHAI<<<<<<<<<<<<<<<AA12345678THA8501014M2801015<<<<<<<4");
        byte[] dg2 = new byte[] { 0x75, 0x20, 0x7F, 0x61, 0x1C, 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00 };
        byte[] dg14 = new byte[] { 0x6E, 0x10 };
        byte[] dg15 = new byte[] { 0x6F, 0x20 };

        using var sha256 = SHA256.Create();
        var sodList = new List<byte>(new byte[] { 0x30, 0x50 });
        sodList.AddRange(sha256.ComputeHash(dg1));
        sodList.AddRange(sha256.ComputeHash(dg2));
        sodList.AddRange(sha256.ComputeHash(dg14));
        sodList.AddRange(sha256.ComputeHash(dg15));
        byte[] sod = sodList.ToArray();

        var container = new NfcDataGroupContainer
        {
            OpticalMrz = new MrzInfo { DocumentNumber = "AA1234567", DateOfBirth = "850101" },
            DataGroups = new Dictionary<int, byte[]>
            {
                { 1, dg1 },
                { 2, dg2 },
                { 14, dg14 },
                { 15, dg15 }
            },
            SodBytes = sod
        };

        var result = _inspector.Inspect(container);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsDg1MrzMatch);
        Assert.True(result.HasFaceBiometrics);
        Assert.True(result.HasChipAuthentication);
        Assert.True(result.HasActiveAuthentication);
        Assert.True(result.IsPassiveAuthenticationValid);
    }
}
