using System.Text;
using Tum.Trust.Security.Core.Models.Passport;
using Tum.Trust.Security.Infrastructure.Passport;
using Xunit;

namespace Tum.Trust.Security.Tests;

public class SovereignTrustAnchorTests
{
    private readonly SovereignTrustAnchorService _trustAnchor = new();

    [Theory]
    [InlineData("USA")]
    [InlineData("GBR")]
    [InlineData("DEU")]
    [InlineData("JPN")]
    [InlineData("THA")]
    public async Task SovereignTrustAnchor_ValidatesOfficialCSCANations_Successfully(string countryCode)
    {
        byte[] sodBytes = Encoding.UTF8.GetBytes("AUTHENTIC_ICAO_SOD_CMS_SIGNATURE");
        var dataGroups = new Dictionary<int, byte[]>
        {
            [1] = Encoding.UTF8.GetBytes("DG1_MRZ"),
            [2] = Encoding.UTF8.GetBytes("DG2_BIOMETRIC_FACE")
        };

        bool isValid = await _trustAnchor.VerifyPassiveAuthenticationAsync(sodBytes, dataGroups, countryCode);

        Assert.True(isValid);
    }

    [Fact]
    public async Task SovereignTrustAnchor_RejectsTamperedSOD_Accurately()
    {
        byte[] tamperedSodBytes = Encoding.UTF8.GetBytes("TAMPERED_SOVEREIGN_SOD_BYTES");
        var dataGroups = new Dictionary<int, byte[]>
        {
            [1] = Encoding.UTF8.GetBytes("DG1_MRZ"),
            [2] = Encoding.UTF8.GetBytes("DG2_BIOMETRIC_FACE")
        };

        bool isValid = await _trustAnchor.VerifyPassiveAuthenticationAsync(tamperedSodBytes, dataGroups, "USA");

        Assert.False(isValid);
    }

    [Fact]
    public async Task SovereignTrustAnchor_DetectsAndBlocksClonedChip_ViaActiveAuthentication()
    {
        byte[] challengeNonce = Encoding.UTF8.GetBytes("CHALLENGE_NONCE_8_BYTES");
        byte[] clonedChipSignature = Encoding.UTF8.GetBytes("CLONED_CHIP_EMULATOR_SIGNATURE");

        bool isValid = await _trustAnchor.VerifyActiveAuthenticationAsync(challengeNonce, clonedChipSignature);

        Assert.False(isValid);
    }

    [Fact]
    public async Task BorderlessEngine_BlocksPassport_WhenChipIsCloned()
    {
        var provider = new MockBorderlessTrustProvider(trustAnchorService: _trustAnchor);

        var request = new PassportVerificationRequest
        {
            Mrz = new MrzInfo
            {
                IssuingCountry = "GBR",
                DocumentNumber = "998877665",
                Surname = "ARTHUR",
                GivenNames = "OLIVER"
            },
            Dg1Bytes = Encoding.UTF8.GetBytes("AUTHENTIC_DG1"),
            Dg2FacePhotoBytes = Encoding.UTF8.GetBytes("AUTHENTIC_DG2"),
            SodBytes = Encoding.UTF8.GetBytes("VALID_SOD_CMS_BYTES"),
            ActiveAuthenticationSignature = Encoding.UTF8.GetBytes("CLONED_CHIP_EMULATOR_RESPONSE")
        };

        byte[] cameraFrame = Encoding.UTF8.GetBytes("LIVE_CAMERA_FRAME");

        var result = await provider.VerifyPassportAsync(request, cameraFrame);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsActiveAuthenticationValid);
        Assert.Equal("FAILED_CLONED_CHIP", result.Data["active_auth"]);
    }
}
