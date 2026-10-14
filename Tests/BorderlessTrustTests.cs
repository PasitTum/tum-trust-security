using System.Text;
using Netka.Identity.Core.Models.Passport;
using Netka.Identity.Infrastructure.Passport;
using Xunit;

namespace Netka.Identity.SDK.Tests;

public class BorderlessTrustTests
{
    [Fact]
    public void MrzParser_CalculatesCheckDigit_Correctly()
    {
        // Standard ICAO 9303 test vector
        // Number: "L898902C3", weight 7-3-1
        // L=21*7=147, 8*3=24, 9*1=9, 8*7=56, 9*3=27, 0*1=0, 2*7=14, C=12*3=36, 3*1=3 -> Sum=316 -> mod 10 = 6
        int checkDigit = MrzParser.CalculateCheckDigit("L898902C3");
        Assert.Equal(6, checkDigit);
    }

    [Fact]
    public void MrzParser_ParsesValidTd3Passport_Successfully()
    {
        // Sample standard ICAO TD3 Passport MRZ (44 characters per line)
        string line1 = "P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<";
        string line2 = "L898902C36UTO7408122F1204159ZE184226B<<<<<10";

        var mrz = MrzParser.ParseTd3(line1, line2);

        Assert.Equal("P", mrz.DocumentType);
        Assert.Equal("UTO", mrz.IssuingCountry);
        Assert.Equal("ERIKSSON", mrz.Surname);
        Assert.Equal("ANNA MARIA", mrz.GivenNames);
        Assert.Equal("L898902C3", mrz.DocumentNumber);
        Assert.Equal("UTO", mrz.Nationality);
        Assert.Equal("740812", mrz.DateOfBirth);
        Assert.Equal("F", mrz.Sex);
        Assert.Equal("120415", mrz.DateOfExpiry);
        Assert.True(mrz.IsChecksumValid);
    }

    [Fact]
    public async Task MockBorderlessTrustProvider_VerifiesPassport_WithZeroKnowledgeToken()
    {
        var provider = new MockBorderlessTrustProvider();
        var request = new PassportVerificationRequest
        {
            Mrz = new MrzInfo
            {
                IssuingCountry = "USA",
                DocumentNumber = "E12345678",
                Surname = "SMITH",
                GivenNames = "JOHN",
                IsChecksumValid = true
            },
            Dg1Bytes = Encoding.UTF8.GetBytes("DG1_MOCK_BYTES"),
            Dg2FacePhotoBytes = Encoding.UTF8.GetBytes("DG2_JPEG2000_FACE_BYTES"),
            SodBytes = Encoding.UTF8.GetBytes("EF_SOD_SIGNATURE_BYTES")
        };

        byte[] liveCameraFrame = Encoding.UTF8.GetBytes("CAMERA_RGB_FRAME");

        var result = await provider.VerifyPassportAsync(request, liveCameraFrame);

        Assert.True(result.IsSuccess);
        Assert.Equal("USA", result.IssuingCountry);
        Assert.True(result.IsPassiveAuthenticationValid);
        Assert.True(result.IsActiveAuthenticationValid);
        Assert.True(result.IsLiveHuman);
        Assert.True(result.BiometricMatchScore > 0.95);
        Assert.NotNull(result.ZeroKnowledgeTrustToken);
        Assert.StartsWith("ntk_zkp_", result.ZeroKnowledgeTrustToken);
    }

    [Fact]
    public async Task MockBorderlessTrustProvider_VerifiesMdl_Successfully()
    {
        var provider = new MockBorderlessTrustProvider();
        byte[] mockMdocPayload = Encoding.UTF8.GetBytes("CBOR_MDOC_PAYLOAD");

        var result = await provider.VerifyMdlAsync(mockMdocPayload);

        Assert.True(result.IsSuccess);
        Assert.Equal("org.iso.18013.5.mDL", result.Data["doc_type"]);
    }
}
