using System.Security.Cryptography;
using Netka.Identity.Core.Models;
using Netka.Identity.Infrastructure.Providers;
using Netka.Identity.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Netka.Identity.SDK.Tests;

public class ThaIdProviderTests
{
    private static IConfiguration CreateConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["IdentityVerification:ThaId:ClientId"] = "test-client-id",
            ["IdentityVerification:ThaId:ClientSecret"] = "test-client-secret",
            ["IdentityVerification:ThaId:ApiKey"] = "test-api-key",
            ["IdentityVerification:ThaId:RedirectUri"] = "https://kiosk.local/callback",
            ["IdentityVerification:ThaId:Scope"] = "openid pid name"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static ThaIdProvider CreateProvider(MockHttpMessageHandler handler)
    {
        byte[] keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);
        string key = Convert.ToBase64String(keyBytes);
        return new ThaIdProvider(
            CreateConfiguration(),
            new HttpClient(handler),
            new AesEncryptionService(key),
            NullLogger<ThaIdProvider>.Instance);
    }

    [Fact]
    public void GetAuthorizationUrl_IncludesPkceAndNonceParametersWhenProvided()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse("{}"));
        var provider = CreateProvider(handler);

        // Act
        string url = provider.GetAuthorizationUrl(
            redirectUri: "https://kiosk.local/callback",
            state: "state-123",
            codeChallenge: "challenge-xyz",
            codeChallengeMethod: "S256",
            nonce: "nonce-999");

        // Assert
        Assert.Contains("response_type=code", url);
        Assert.Contains("client_id=test-client-id", url);
        Assert.Contains("state=state-123", url);
        Assert.Contains("code_challenge=challenge-xyz", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.Contains("nonce=nonce-999", url);
    }

    [Fact]
    public async Task ExchangeCodeAsync_ReturnsUnmodifiedTokenResponseIncludingAllTokens()
    {
        // Arrange
        string dopaTokenJson = """
        {
            "access_token": "thaid-access-token",
            "refresh_token": "thaid-refresh-token",
            "id_token": "thaid-id-token",
            "token_type": "Bearer",
            "expires_in": 3600,
            "pid": "1234567890121"
        }
        """;

        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse(dopaTokenJson));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.ExchangeCodeAsync("auth-code-001", "https://kiosk.local/callback", "verifier-001");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("thaid-access-token", result.Data["access_token"]);
        Assert.Equal("thaid-refresh-token", result.Data["refresh_token"]);
        Assert.Equal("thaid-id-token", result.Data["id_token"]);
        Assert.Equal("1234567890121", result.Data["pid"]);

        Assert.Single(handler.SentRequests);
        Assert.Equal("Basic", handler.SentRequests[0].Headers.Authorization?.Scheme);
        Assert.Contains("grant_type=authorization_code", handler.SentBodies[0]);
        Assert.Contains("code=auth-code-001", handler.SentBodies[0]);
        Assert.Contains("code_verifier=verifier-001", handler.SentBodies[0]);
    }

    [Fact]
    public async Task RefreshTokenAsync_SendsRefreshTokenGrantAndReturnsNewTokens()
    {
        // Arrange
        string refreshResponse = """
        {
            "access_token": "new-access-token",
            "refresh_token": "new-refresh-token",
            "expires_in": 3600
        }
        """;

        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse(refreshResponse));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.RefreshTokenAsync("old-refresh-token");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new-access-token", result.Data["access_token"]);
        Assert.Contains("grant_type=refresh_token", handler.SentBodies[0]);
        Assert.Contains("refresh_token=old-refresh-token", handler.SentBodies[0]);
    }

    [Fact]
    public async Task InspectTokenAsync_ReturnsActiveStatusFromThaId()
    {
        // Arrange
        string inspectJson = """
        {
            "active": true,
            "scope": "openid pid name"
        }
        """;

        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse(inspectJson));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.InspectTokenAsync("token-to-inspect");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(true, result.Data["active"]);
        Assert.Contains("token=token-to-inspect", handler.SentBodies[0]);
    }

    [Fact]
    public async Task RevokeTokenAsync_ReturnsSuccessWhenThaIdRevokesToken()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse("""{"message":"revoked"}"""));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.RevokeTokenAsync("token-to-revoke");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("revoked", result.Data["message"]);
        Assert.Contains("token=token-to-revoke", handler.SentBodies[0]);
    }

    [Fact]
    public async Task GetUserInfoAsync_SendsBearerHeaderAndReturnsCitizenData()
    {
        // Arrange
        string userInfoJson = """
        {
            "sub": "user-sub-1",
            "pid": "1234567890121",
            "name": "สมชาย ใจดี"
        }
        """;

        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse(userInfoJson));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.GetUserInfoAsync("valid-access-token");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("1234567890121", result.Data["pid"]);
        Assert.Equal("สมชาย ใจดี", result.Data["name"]);
        Assert.Equal("Bearer", handler.SentRequests[0].Headers.Authorization?.Scheme);
        Assert.Equal("valid-access-token", handler.SentRequests[0].Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task VerifyAsync_WhenStateDoesNotMatchExpectedState_ReturnsInvalidStateError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.JsonResponse("{}"));
        var provider = CreateProvider(handler);

        var request = new VerificationRequest
        {
            ProviderName = "ThaID",
            AuthorizationCode = "code-123",
            State = "tampered-state",
            AdditionalParameters = new Dictionary<string, object>
            {
                ["expected_state"] = "original-state"
            }
        };

        // Act
        var result = await provider.VerifyAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_STATE", result.ErrorCode);
        Assert.Empty(handler.SentRequests);
    }
}
