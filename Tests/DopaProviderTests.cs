using System.Security.Cryptography;
using Netka.Identity.Infrastructure.Providers;
using Netka.Identity.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Netka.Identity.SDK.Tests;

public class DopaProviderTests
{
    private static IConfiguration CreateConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["IdentityVerification:Dopa:LaserUrl"] = "https://dopa.local/CheckCardByLaser",
            ["IdentityVerification:Dopa:ChipUrl"] = "https://dopa.local/CheckCardByCID",
            ["IdentityVerification:Dopa:ApiKey"] = "dopa-api-key"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static DopaProvider CreateProvider(MockHttpMessageHandler handler)
    {
        byte[] keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);
        string key = Convert.ToBase64String(keyBytes);
        return new DopaProvider(
            CreateConfiguration(),
            new HttpClient(handler),
            new AesEncryptionService(key),
            NullLogger<DopaProvider>.Instance);
    }

    [Fact]
    public async Task VerifyByLaserAsync_WithInvalidPidChecksum_ReturnsInvalidPidFormatWithoutCallingApi()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse("{}"));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.VerifyByLaserAsync(
            pid: "1234567890122", // Invalid Modulo 11 checksum
            firstName: "สมชาย",
            lastName: "ใจดี",
            birthDate: "25300101",
            laserId: "ME0123456789");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_PID_FORMAT", result.ErrorCode);
        Assert.Empty(handler.SentRequests);
    }

    [Fact]
    public async Task VerifyByLaserAsync_WithValidCardData_SendsExpectedFieldsAndReturnsUnmodifiedResponse()
    {
        // Arrange
        string dopaResponse = """
        {
            "IsError": false,
            "Code": 0,
            "Desc": "สถานะปกติ"
        }
        """;

        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse(dopaResponse));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.VerifyByLaserAsync(
            pid: "1234567890121",
            firstName: "สมชาย",
            lastName: "ใจดี",
            birthDate: "25300101",
            laserId: "ME0123456789");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(false, result.Data["IsError"]);
        Assert.Equal(0L, result.Data["Code"]);
        Assert.Equal("สถานะปกติ", result.Data["Desc"]);
        Assert.False(result.Data.ContainsKey("pid")); // Ensures SDK does not artificially inject extra keys

        Assert.Single(handler.SentRequests);
        Assert.Equal("https://dopa.local/CheckCardByLaser", handler.SentRequests[0].RequestUri?.ToString());
        Assert.Contains("\"PID\":\"1234567890121\"", handler.SentBodies[0]);
        Assert.Contains("\"Laser\":\"ME0123456789\"", handler.SentBodies[0]);
    }

    [Fact]
    public async Task VerifyByChipAsync_WithValidChipData_SendsExpectedFieldsAndReturnsUnmodifiedResponse()
    {
        // Arrange
        string dopaResponse = """
        {
            "IsError": false,
            "Code": 0,
            "Desc": "สถานะปกติ"
        }
        """;

        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse(dopaResponse));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.VerifyByChipAsync(
            pid: "1234567890121",
            chipNo: "1234567890123456",
            bp1No: "123456789012");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("สถานะปกติ", result.Data["Desc"]);
        Assert.Single(handler.SentRequests);
        Assert.Equal("https://dopa.local/CheckCardByCID", handler.SentRequests[0].RequestUri?.ToString());
        Assert.Contains("\"PID\":\"1234567890121\"", handler.SentBodies[0]);
        Assert.Contains("\"ChipNo\":\"1234567890123456\"", handler.SentBodies[0]);
        Assert.Contains("\"bp1no\":\"123456789012\"", handler.SentBodies[0]);
    }

    [Fact]
    public async Task VerifyByLaserAsync_WhenDopaReportsIsErrorTrue_ReturnsFailureResultWithDataPreserved()
    {
        // Arrange
        string dopaRejectedResponse = """
        {
            "IsError": true,
            "ErrorMessage": "ไม่พบเลข Laser จาก PID นี้"
        }
        """;

        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse(dopaRejectedResponse));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.VerifyByLaserAsync(
            pid: "1234567890121",
            firstName: "สมชาย",
            lastName: "ใจดี",
            birthDate: "25300101",
            laserId: "ME9999999999");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CARD_VERIFICATION_REJECTED", result.ErrorCode);
        Assert.Equal("ไม่พบเลข Laser จาก PID นี้", result.Message);
        Assert.Equal(true, result.Data["IsError"]);
    }

    [Fact]
    public async Task VerifyByLaserAsync_WithMissingLaserCode_ReturnsMissingParametersWithoutCallingApi()
    {
        // Arrange (UC-DOPA-03)
        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse("{}"));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.VerifyByLaserAsync(
            pid: "1234567890121",
            firstName: "สมชาย",
            lastName: "ใจดี",
            birthDate: "25300101",
            laserId: "");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("MISSING_PARAMETERS", result.ErrorCode);
        Assert.Empty(handler.SentRequests);
    }

    [Fact]
    public async Task VerifyByLaserAsync_WhenDopaServerReturnsHttp503_ReturnsDopaApiFailedError()
    {
        // Arrange (UC-DOPA-05)
        var handler = new MockHttpMessageHandler(_ =>
            MockHttpMessageHandler.JsonResponse(
                """{"message":"Service Unavailable"}""",
                System.Net.HttpStatusCode.ServiceUnavailable));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.VerifyByLaserAsync(
            pid: "1234567890121",
            firstName: "สมชาย",
            lastName: "ใจดี",
            birthDate: "25300101",
            laserId: "ME0123456789");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("DOPA_API_FAILED", result.ErrorCode);
        Assert.Equal(503, result.Error?.HttpStatusCode);
    }

    [Fact]
    public async Task VerifyByChipAsync_WhenNetworkTimeoutOccurs_ReturnsNetworkError()
    {
        // Arrange (UC-DOPA-06)
        var handler = new MockHttpMessageHandler(_ =>
            throw new HttpRequestException("Simulated connection timeout to DOPA Gateway"));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.VerifyByChipAsync(
            pid: "1234567890121",
            chipNo: "1234567890123456",
            bp1No: "123456789012");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NETWORK_ERROR", result.ErrorCode);
    }
}
