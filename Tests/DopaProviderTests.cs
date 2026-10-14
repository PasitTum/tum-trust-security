using System.Security.Cryptography;
using Tum.Trust.Security.Core.Models;
using Tum.Trust.Security.Infrastructure.Providers;
using Tum.Trust.Security.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tum.Trust.Security.Tests;

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

    [Fact]
    public async Task BuiltInUseMock_WhenEnabled_SimulatesAllDopaUseCasesWithoutHttpCalls()
    {
        // Arrange: Handler throws if any HTTP call is attempted; LaserUrl/ChipUrl are not configured
        var handler = new MockHttpMessageHandler(_ =>
            throw new InvalidOperationException("HTTP should not be called when UseMock = true"));
        var options = new DopaOptions { UseMock = true };
        var provider = new DopaProvider(
            options,
            new HttpClient(handler),
            new AesEncryptionService(Convert.ToBase64String(new byte[32])),
            NullLogger<DopaProvider>.Instance);

        // UC-DOPA-01: Normal Card Status
        var okResult = await provider.VerifyByLaserAsync("1234567890121", "สมชาย", "ใจดี", "25330131", "ME0123456789");
        Assert.True(okResult.IsSuccess);
        Assert.Equal(0, okResult.Data["Code"]);
        Assert.Equal("สถานะปกติ", okResult.Data["Desc"]);

        // UC-DOPA-04: Card Rejected (ME9999999999)
        var rejectedResult = await provider.VerifyByLaserAsync("1234567890121", "สมชาย", "ใจดี", "25330131", "ME9999999999");
        Assert.False(rejectedResult.IsSuccess);
        Assert.Equal("CARD_VERIFICATION_REJECTED", rejectedResult.ErrorCode);

        // UC-DOPA-05: DOPA API HTTP 503 (ME5000000000)
        var unavailableResult = await provider.VerifyByLaserAsync("1234567890121", "สมชาย", "ใจดี", "25330131", "ME5000000000");
        Assert.False(unavailableResult.IsSuccess);
        Assert.Equal("DOPA_API_FAILED", unavailableResult.ErrorCode);
        Assert.Equal(503, unavailableResult.Error?.HttpStatusCode);

        // UC-DOPA-06: Network Timeout (METIMEOUT000)
        var timeoutResult = await provider.VerifyByLaserAsync("1234567890121", "สมชาย", "ใจดี", "25330131", "METIMEOUT000");
        Assert.False(timeoutResult.IsSuccess);
        Assert.Equal("NETWORK_ERROR", timeoutResult.ErrorCode);

        // Face Match Success & Mismatch in UseMock = true mode
        var faceSuccess = await provider.VerifyByFaceAsync("1234567890121", "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/");
        Assert.True(faceSuccess.IsSuccess);
        Assert.Equal(true, faceSuccess.Data["match"]);

        var faceMismatch = await provider.VerifyByFaceAsync("1234567890121", "data:image/jpeg;base64,MISMATCH_IMAGE_DATA");
        Assert.False(faceMismatch.IsSuccess);
        Assert.Equal("FACE_VERIFICATION_REJECTED", faceMismatch.ErrorCode);
        Assert.Equal(false, faceMismatch.Data["match"]);

        // RequestAccessTokenAsync in UseMock = true mode
        var mockToken = await provider.RequestAccessTokenAsync();
        Assert.True(mockToken.IsSuccess);
        Assert.Equal("mock-dopa-access-token-2026", mockToken.Data["access_token"]);
    }

    [Fact]
    public async Task VerifyByFaceAsync_WithOAuthClientCredentialsToken_AcquiresTokenStripsBase64PrefixAndEvaluatesSimilarity()
    {
        int tokenCallCount = 0;
        string? capturedAuthHeader = null;
        string? capturedBody = null;

        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/oauth/token", StringComparison.OrdinalIgnoreCase))
            {
                tokenCallCount++;
                return MockHttpMessageHandler.JsonResponse("""{"access_token":"dynamic-dopa-jwt-token","token_type":"Bearer","expires_in":3600}""");
            }

            capturedAuthHeader = req.Headers.Authorization?.ToString();
            capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return MockHttpMessageHandler.JsonResponse("""{"result_code":"0","result_text":"ใบหน้าตรงกับฐานข้อมูล","similarity":0.92,"confidence":92.0,"match":true}""");
        });

        var options = new DopaOptions
        {
            FaceUrl = "https://dopa-gateway.local/api/v1/face/verify",
            FaceThreshold = 0.80f,
            TokenUrl = "https://dopa-gateway.local/oauth/token",
            ClientId = "dopa-client-id",
            ClientSecret = "dopa-client-secret"
        };

        var provider = new DopaProvider(
            options,
            new HttpClient(handler),
            new AesEncryptionService(Convert.ToBase64String(new byte[32])),
            NullLogger<DopaProvider>.Instance);

        // Act 1: Call VerifyByFaceAsync with data:image/jpeg;base64, prefix
        var result1 = await provider.VerifyByFaceAsync(
            pid: "1234567890121",
            faceImageBase64: "data:image/jpeg;base64,RAW_BASE64_FACE_BYTES");

        // Act 2: Call VerifyByFaceAsync a second time to verify token caching (tokenCallCount remains 1)
        var result2 = await provider.VerifyByFaceAsync(
            pid: "1234567890121",
            faceImageBase64: "RAW_BASE64_FACE_BYTES");

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(1, tokenCallCount);
        Assert.Equal("Bearer dynamic-dopa-jwt-token", capturedAuthHeader);
        Assert.NotNull(capturedBody);
        Assert.Contains("\"image\":\"RAW_BASE64_FACE_BYTES\"", capturedBody);
        Assert.DoesNotContain("data:image/jpeg;base64,", capturedBody);
        Assert.Equal(true, result1.Data["match"]);
    }

    [Fact]
    public async Task VerifyByLaserAsync_InDgaGdxMode_RequestsTokenAndVerifiesCardWithHeadersAndQueryParams()
    {
        // Arrange
        int authCallCount = 0;
        int verifyCallCount = 0;
        string? capturedConsumerKey = null;
        string? capturedTokenHeader = null;
        string? capturedVerifyUri = null;

        var handler = new MockHttpMessageHandler(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;
            if (uri.Contains("/ws/auth/validate"))
            {
                authCallCount++;
                return MockHttpMessageHandler.JsonResponse("""{"Result": "test-dga-uuid-token-60min"}""");
            }

            if (uri.Contains("/ws/dopa/verification/personal"))
            {
                verifyCallCount++;
                capturedVerifyUri = uri;
                if (req.Headers.TryGetValues("Consumer-Key", out var ckValues))
                {
                    capturedConsumerKey = ckValues.FirstOrDefault();
                }
                if (req.Headers.TryGetValues("Token", out var tokenValues))
                {
                    capturedTokenHeader = tokenValues.FirstOrDefault();
                }
                return MockHttpMessageHandler.JsonResponse("""{"Result": true}""");
            }

            return MockHttpMessageHandler.JsonResponse("{}", System.Net.HttpStatusCode.NotFound);
        });

        var options = new DopaOptions
        {
            Mode = DopaGatewayMode.DgaGdx,
            ConsumerKey = "MY_CONSUMER_KEY_123",
            ConsumerSecret = "MY_CONSUMER_SECRET_456",
            AgentId = "1234567890121",
            DgaGdxBaseUrl = "https://api.egov.go.th"
        };

        var provider = new DopaProvider(
            options,
            new HttpClient(handler),
            new AesEncryptionService(Convert.ToBase64String(new byte[32])),
            NullLogger<DopaProvider>.Instance);

        // Act - Call twice to verify token is cached (authCallCount remains 1)
        var result1 = await provider.VerifyByLaserAsync(
            pid: "1234567890121",
            firstName: "สมชาย",
            lastName: "ใจดี",
            birthDate: "25381229",
            laserId: "JT1234567890");

        var result2 = await provider.VerifyByLaserAsync(
            pid: "1234567890121",
            firstName: "สมชาย",
            lastName: "ใจดี",
            birthDate: "25381229",
            laserId: "JT1234567890");

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(1, authCallCount);
        Assert.Equal(2, verifyCallCount);
        Assert.Equal("MY_CONSUMER_KEY_123", capturedConsumerKey);
        Assert.Equal("test-dga-uuid-token-60min", capturedTokenHeader);
        Assert.NotNull(capturedVerifyUri);
        Assert.Contains("CitizenID=1234567890121", capturedVerifyUri);
        Assert.Contains("FirstName=", capturedVerifyUri);
        Assert.Contains("BEBirthDate=25381229", capturedVerifyUri);
        Assert.Contains("LaserCode=JT1234567890", capturedVerifyUri);
        Assert.Equal(true, result1.Data["Result"]);
    }

    [Fact]
    public async Task VerifyByLaserAsync_InDgaGdxMode_WhenDgaReturnsCode4_MapsToFriendlyThaiMessage()
    {
        // Arrange - DGA returns HTTP 500 with [4] error
        var handler = new MockHttpMessageHandler(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;
            if (uri.Contains("/ws/auth/validate"))
            {
                return MockHttpMessageHandler.JsonResponse("""{"Result": "valid-token"}""");
            }

            return MockHttpMessageHandler.JsonResponse(
                """{"message": "[4] สถานะไม่ปกติ => ไม่พบเลข Laser จาก PID นี้"}""",
                System.Net.HttpStatusCode.InternalServerError);
        });

        var options = new DopaOptions
        {
            Mode = DopaGatewayMode.DgaGdx,
            ConsumerKey = "KEY",
            ConsumerSecret = "SECRET",
            AgentId = "1234567890121"
        };

        var provider = new DopaProvider(
            options,
            new HttpClient(handler),
            new AesEncryptionService(Convert.ToBase64String(new byte[32])),
            NullLogger<DopaProvider>.Instance);

        // Act
        var result = await provider.VerifyByLaserAsync(
            pid: "1234567890121",
            firstName: "สมชาย",
            lastName: "ใจดี",
            birthDate: "25381229",
            laserId: "JT0000000000");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("DGA_GDX_API_FAILED", result.ErrorCode);
        Assert.Contains("ไม่พบเลข Laser", result.Message);
        Assert.Contains("Code [4]", result.Message);
    }

    [Fact]
    public async Task HealthCheckAsync_InMockMode_ReturnsSuccessUpStatus()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse("{}"));
        var options = new DopaOptions { UseMock = true };
        var provider = new DopaProvider(
            options,
            new HttpClient(handler),
            new AesEncryptionService(Convert.ToBase64String(new byte[32])),
            NullLogger<DopaProvider>.Instance);

        // Act
        var result = await provider.HealthCheckAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("UP", result.Data["status"]);
    }

    [Fact]
    public async Task HealthCheckAsync_InDgaGdxMode_ValidatesTokenSuccessfully()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.RequestUri?.ToString().Contains("/ws/auth/validate") == true)
            {
                return MockHttpMessageHandler.JsonResponse("""{"Result": "healthy-token"}""");
            }
            return MockHttpMessageHandler.JsonResponse("{}", System.Net.HttpStatusCode.NotFound);
        });

        var options = new DopaOptions
        {
            Mode = DopaGatewayMode.DgaGdx,
            ConsumerKey = "KEY",
            ConsumerSecret = "SECRET"
        };

        var provider = new DopaProvider(
            options,
            new HttpClient(handler),
            new AesEncryptionService(Convert.ToBase64String(new byte[32])),
            NullLogger<DopaProvider>.Instance);

        // Act
        var result = await provider.HealthCheckAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("UP", result.Data["status"]);
        Assert.Equal("DgaGdx", result.Data["mode"]);
    }
}
