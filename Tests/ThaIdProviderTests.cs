using System.Security.Cryptography;
using Tum.Trust.Security.Core.Models;
using Tum.Trust.Security.Infrastructure.Providers;
using Tum.Trust.Security.Infrastructure.Helpers;
using Tum.Trust.Security.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tum.Trust.Security.Tests;

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
    public void GetAuthorizationUrl_WithCustomScope_UsesCustomScope()
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
            nonce: "nonce-999",
            scope: "openid pid address");

        // Assert
        Assert.Contains("scope=openid%20pid%20address", url);
        // Ensure scope is NOT duplicated
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(url, "scope="));
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

    [Fact]
    public async Task EndToEndOAuthRedirectFlow_SimulatesBrowserRedirectCallbackAndPkceVerificationWithAll20Scopes()
    {
        // Arrange: Simulate a stateful ThaID OAuth2 Server (Authorize 302 Redirect -> Token PKCE Check -> UserInfo)
        string? serverStoredChallenge = null;
        const string issuedAuthCode = "thaid-redirect-auth-code-777";

        var handler = new MockHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!;

            // 1. Simulate ThaID Authorization Endpoint (/api/v2/oauth2/auth/) returning HTTP 302 Redirect to Kiosk Callback
            if (uri.AbsolutePath.Contains("/oauth2/auth"))
            {
                var queryParams = ParseQueryString(uri.Query);
                string clientRedirectUri = queryParams["redirect_uri"];
                string clientState = queryParams["state"];
                serverStoredChallenge = queryParams["code_challenge"];

                Assert.Equal("S256", queryParams["code_challenge_method"]);
                Assert.Equal("test-client-id", queryParams["client_id"]);

                string callbackLocation = $"{clientRedirectUri}?code={Uri.EscapeDataString(issuedAuthCode)}&state={Uri.EscapeDataString(clientState)}";
                return MockHttpMessageHandler.RedirectResponse(callbackLocation);
            }

            // 2. Simulate ThaID Token Endpoint (/api/v2/oauth2/token/) validating PKCE code_verifier against stored code_challenge
            if (uri.AbsolutePath.Contains("/oauth2/token"))
            {
                var formBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                var formParams = ParseQueryString(formBody);

                string receivedCode = formParams["code"];
                string receivedVerifier = formParams["code_verifier"];

                if (receivedCode != issuedAuthCode ||
                    serverStoredChallenge is null ||
                    !PkceHelper.VerifyCodeChallenge(receivedVerifier, serverStoredChallenge))
                {
                    return MockHttpMessageHandler.JsonResponse(
                        """{"error":"invalid_grant","error_description":"PKCE verification failed"}""",
                        System.Net.HttpStatusCode.BadRequest);
                }

                return MockHttpMessageHandler.JsonResponse("""
                {
                    "access_token": "thaid-live-access-token",
                    "refresh_token": "thaid-live-refresh-token",
                    "id_token": "thaid-live-id-token",
                    "token_type": "Bearer",
                    "expires_in": 3600,
                    "scope": "openid pid address gender birthdate given_name middle_name family_name name given_name_en middle_name_en family_name_en name_en title title_en ial smartcard_code date_of_expiry date_of_issuance house_address",
                    "pid": "1234567890121",
                    "title": "นาย",
                    "title_en": "Mr.",
                    "given_name": "สมชาย",
                    "middle_name": "",
                    "family_name": "ใจดี",
                    "name": "นาย สมชาย ใจดี",
                    "given_name_en": "Somchai",
                    "middle_name_en": "",
                    "family_name_en": "Jaidee",
                    "name_en": "Mr. Somchai Jaidee",
                    "gender": "male",
                    "birthdate": "1990-01-31",
                    "address": "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี",
                    "smartcard_code": "ME0123456789",
                    "date_of_issuance": "2022-01-31",
                    "date_of_expiry": "2030-01-30",
                    "ial": 2.3,
                    "house_address": {
                        "raw": "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี",
                        "formatted": "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี"
                    }
                }
                """);
            }

            return MockHttpMessageHandler.JsonResponse("{}", System.Net.HttpStatusCode.NotFound);
        });

        var provider = CreateProvider(handler);
        using var browserHttpClient = new HttpClient(handler);

        // Step 1: Kiosk generates PKCE pair and Authorization URL
        string codeVerifier = PkceHelper.GenerateCodeVerifier();
        string codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);
        const string originalState = "kiosk-csrf-state-001";
        const string callbackUri = "https://kiosk.local/callback";

        string authUrl = provider.GetAuthorizationUrl(
            redirectUri: callbackUri,
            state: originalState,
            codeChallenge: codeChallenge,
            codeChallengeMethod: "S256",
            nonce: "nonce-001");

        // Step 2: Simulate Browser navigating to authUrl and receiving HTTP 302 Redirect back to Kiosk Callback
        using var authResponse = await browserHttpClient.GetAsync(authUrl);
        Assert.Equal(System.Net.HttpStatusCode.Found, authResponse.StatusCode);
        Assert.NotNull(authResponse.Headers.Location);

        var callbackUriReceived = authResponse.Headers.Location!;
        var callbackQuery = ParseQueryString(callbackUriReceived.Query);
        string callbackCode = callbackQuery["code"];
        string callbackState = callbackQuery["state"];

        // Step 3: Kiosk Callback exchanges code + PKCE code_verifier via VerifyAsync
        var verifyResult = await provider.VerifyAsync(new VerificationRequest
        {
            ProviderName = "ThaID",
            AuthorizationCode = callbackCode,
            RedirectUri = callbackUri,
            State = callbackState,
            AdditionalParameters = new Dictionary<string, object>
            {
                ["expected_state"] = originalState,
                ["code_verifier"] = codeVerifier
            }
        });

        // Assert
        Assert.True(verifyResult.IsSuccess);
        Assert.Equal("1234567890121", verifyResult.Data["pid"]);
        Assert.Equal("นาย สมชาย ใจดี", verifyResult.Data["name"]);
        Assert.Equal("Mr. Somchai Jaidee", verifyResult.Data["name_en"]);
        Assert.Equal("ME0123456789", verifyResult.Data["smartcard_code"]);
        Assert.Equal("thaid-live-access-token", verifyResult.Data["access_token"]);
        Assert.True(verifyResult.Data.ContainsKey("house_address"));
    }

    [Fact]
    public async Task EndToEndOAuthRedirectFlow_WhenCitizenDeniesConsent_RedirectsWithErrorAccessDeniedAndFailsMissingCode()
    {
        // Arrange: Simulate citizen clicking "Reject / Cancel" in ThaID app -> 302 Redirect with ?error=access_denied
        var handler = new MockHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!;
            if (uri.AbsolutePath.Contains("/oauth2/auth"))
            {
                var queryParams = ParseQueryString(uri.Query);
                string clientRedirectUri = queryParams["redirect_uri"];
                string clientState = queryParams["state"];

                string errorRedirectLocation =
                    $"{clientRedirectUri}?error=access_denied&error_description={Uri.EscapeDataString("User denied consent")}&state={Uri.EscapeDataString(clientState)}";
                return MockHttpMessageHandler.RedirectResponse(errorRedirectLocation);
            }

            return MockHttpMessageHandler.JsonResponse("{}", System.Net.HttpStatusCode.NotFound);
        });

        var provider = CreateProvider(handler);
        using var browserHttpClient = new HttpClient(handler);

        string authUrl = provider.GetAuthorizationUrl(
            redirectUri: "https://kiosk.local/callback",
            state: "kiosk-state-deny-01");

        // Step 1: Browser follows authorization URL and gets 302 Redirect with error=access_denied (no code)
        using var authResponse = await browserHttpClient.GetAsync(authUrl);
        Assert.Equal(System.Net.HttpStatusCode.Found, authResponse.StatusCode);

        var callbackQuery = ParseQueryString(authResponse.Headers.Location!.Query);
        Assert.Equal("access_denied", callbackQuery["error"]);
        Assert.False(callbackQuery.ContainsKey("code"));

        // Step 2: If Callback passes empty/missing code to VerifyAsync, SDK returns MISSING_AUTH_CODE
        var verifyResult = await provider.VerifyAsync(new VerificationRequest
        {
            ProviderName = "ThaID",
            AuthorizationCode = callbackQuery.TryGetValue("code", out var code) ? code : null,
            RedirectUri = "https://kiosk.local/callback",
            State = callbackQuery["state"]
        });

        Assert.False(verifyResult.IsSuccess);
        Assert.Equal("MISSING_AUTH_CODE", verifyResult.ErrorCode);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenCodeExpiredOrInvalidGrant_ReturnsTokenExchangeFailedWithHttp400()
    {
        // Arrange (UC-THAID-03)
        var handler = new MockHttpMessageHandler(_ =>
            MockHttpMessageHandler.JsonResponse(
                """{"error":"invalid_grant","error_description":"Authorization code has expired"}""",
                System.Net.HttpStatusCode.BadRequest));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.ExchangeCodeAsync("expired-code", "https://kiosk.local/callback");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("TOKEN_EXCHANGE_FAILED", result.ErrorCode);
        Assert.Equal(400, result.Error?.HttpStatusCode);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenNetworkTimeoutOccurs_ReturnsNetworkError()
    {
        // Arrange (UC-THAID-04)
        var handler = new MockHttpMessageHandler(_ =>
            throw new HttpRequestException("Simulated connection timeout to imauth.bora.dopa.go.th"));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.ExchangeCodeAsync("valid-code", "https://kiosk.local/callback");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NETWORK_ERROR", result.ErrorCode);
    }

    [Fact]
    public async Task InspectTokenAsync_WhenTokenIsExpired_ReturnsActiveFalse()
    {
        // Arrange (UC-THAID-05)
        var handler = new MockHttpMessageHandler(_ =>
            MockHttpMessageHandler.JsonResponse("""{"active":false}"""));
        var provider = CreateProvider(handler);

        // Act
        var result = await provider.InspectTokenAsync("expired-access-token");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(false, result.Data["active"]);
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return result;
        }

        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    [Fact]
    public async Task BuiltInUseMock_WhenEnabled_PerformsLoopbackRedirectAndReturnsAll20ScopesWithoutHttpCalls()
    {
        // Arrange: Handler throws if any real HTTP call is attempted; ClientId/ClientSecret are empty
        var handler = new MockHttpMessageHandler(_ =>
            throw new InvalidOperationException("HTTP should not be called when UseMock = true"));
        var options = new ThaIdOptions
        {
            UseMock = true,
            MockScenario = "success",
            RedirectUri = "https://kiosk.local/api/auth/thaid/callback"
        };
        var provider = new ThaIdProvider(
            options,
            new HttpClient(handler),
            new AesEncryptionService(Convert.ToBase64String(new byte[32])),
            NullLogger<ThaIdProvider>.Instance);

        // Step 1: GetAuthorizationUrl returns Loopback Callback URL directly (suitable for QR Code or Browser Redirect)
        string loopbackUrl = provider.GetAuthorizationUrl(
            redirectUri: "https://kiosk.local/api/auth/thaid/callback",
            state: "mock-state-999",
            codeChallenge: null,
            codeChallengeMethod: "S256",
            nonce: "nonce-123");

        Assert.Equal(
            "https://kiosk.local/api/auth/thaid/callback?code=mock-auth-code-001&state=mock-state-999",
            loopbackUrl);

        // Step 2: ExchangeCodeAsync returns all 20 scopes in-memory
        var result = await provider.ExchangeCodeAsync("mock-auth-code-001");
        Assert.True(result.IsSuccess);
        Assert.Equal("1100501234567", result.Data["pid"]);
        // Verify mock PID passes Modulo 11 check digit so DOPA face verify can accept it
        Assert.True(result.Data["pid"].ToString().IsValidThaiNationalId());
        Assert.Equal("นาย สมชาย ใจดี", result.Data["name"]);
        Assert.Equal("Mr. Somchai Jaidee", result.Data["name_en"]);
        Assert.Equal("1001012345678901", result.Data["smartcard_code"]);
        Assert.True(result.Data.ContainsKey("address"));
        Assert.True(result.Data.ContainsKey("house_address"));

        // Step 3: Simulate UC-THAID-03 (Expired Code) and UC-THAID-04 (Timeout) in UseMock mode
        var expiredResult = await provider.ExchangeCodeAsync("mock-expired-code");
        Assert.False(expiredResult.IsSuccess);
        Assert.Equal("TOKEN_EXCHANGE_FAILED", expiredResult.ErrorCode);

        var timeoutResult = await provider.ExchangeCodeAsync("mock-timeout");
        Assert.False(timeoutResult.IsSuccess);
        Assert.Equal("NETWORK_ERROR", timeoutResult.ErrorCode);

        // Step 4: Discovery, JWKS, and ValidateIdTokenAsync in UseMock mode
        var discoveryResult = await provider.GetOpenIdConfigurationAsync();
        Assert.True(discoveryResult.IsSuccess);
        Assert.Equal("https://imauth.bora.dopa.go.th", discoveryResult.Data["issuer"]);

        var jwksResult = await provider.GetJwksAsync();
        Assert.True(jwksResult.IsSuccess);
        Assert.True(jwksResult.Data.ContainsKey("keys"));

        // 4a. ValidateIdTokenAsync in mock mode with matching nonce succeeds
        var idTokenResult = await provider.ValidateIdTokenAsync(
            result.Data["id_token"].ToString()!,
            expectedNonce: "nonce-123");
        Assert.True(idTokenResult.IsSuccess);
        Assert.Equal("1100501234567", idTokenResult.Data["pid"]);
        Assert.Equal("nonce-123", idTokenResult.Data["nonce"]);

        // 4b. ValidateIdTokenAsync in mock mode with mismatched nonce fails with INVALID_NONCE
        var wrongNonceResult = await provider.ValidateIdTokenAsync(
            result.Data["id_token"].ToString()!,
            expectedNonce: "wrong-tampered-nonce");
        Assert.False(wrongNonceResult.IsSuccess);
        Assert.Equal("INVALID_NONCE", wrongNonceResult.ErrorCode);
    }

    [Fact]
    public async Task GetOpenIdConfigurationAndJwks_AndValidateIdTokenWithEs256Signature_Succeeds()
    {
        // Generate an ephemeral P-256 ECDSA key pair to sign a real ES256 JWT id_token
        using var ecdsa = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        var ecParams = ecdsa.ExportParameters(includePrivateParameters: false);

        static string ToBase64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var xB64 = ToBase64Url(ecParams.Q.X!);
        var yB64 = ToBase64Url(ecParams.Q.Y!);

        var jwksJson = $$"""
        {
          "keys": [
            {
              "kty": "EC",
              "crv": "P-256",
              "use": "sig",
              "alg": "ES256",
              "kid": "test-es256-key",
              "x": "{{xB64}}",
              "y": "{{yB64}}"
            }
          ]
        }
        """;

        string? capturedApiKeyHeader = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.Headers.TryGetValues("x-custom-gateway-key", out var values))
            {
                capturedApiKeyHeader = values.FirstOrDefault();
            }

            if (req.RequestUri!.AbsolutePath.EndsWith("openid-configuration", StringComparison.OrdinalIgnoreCase))
            {
                return MockHttpMessageHandler.JsonResponse("""{"issuer":"https://imauth.bora.dopa.go.th","jwks_uri":"https://imauth.bora.dopa.go.th/jwks/"}""");
            }

            return MockHttpMessageHandler.JsonResponse(jwksJson);
        });

        var options = new ThaIdOptions
        {
            ClientId = "test-thaid-client-id",
            ClientSecret = "test-thaid-client-secret",
            Issuer = "https://imauth.bora.dopa.go.th",
            DiscoveryUrl = "https://imauth.bora.dopa.go.th/.well-known/openid-configuration",
            JwksUrl = "https://imauth.bora.dopa.go.th/jwks/",
            ApiKey = "secret-gateway-key-999",
            ApiKeyHeader = "x-custom-gateway-key"
        };

        var provider = new ThaIdProvider(
            options,
            new HttpClient(handler),
            new AesEncryptionService(Convert.ToBase64String(new byte[32])),
            NullLogger<ThaIdProvider>.Instance);

        // 1. Test GetOpenIdConfigurationAsync + custom ApiKeyHeader
        var disc = await provider.GetOpenIdConfigurationAsync();
        Assert.True(disc.IsSuccess);
        Assert.Equal("https://imauth.bora.dopa.go.th", disc.Data["issuer"]);
        Assert.Equal("secret-gateway-key-999", capturedApiKeyHeader);

        // 2. Build & sign a real ES256 JWT
        var headerB64 = ToBase64Url(System.Text.Encoding.UTF8.GetBytes("""{"alg":"ES256","typ":"JWT","kid":"test-es256-key"}"""));
        var exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();
        var payloadB64 = ToBase64Url(System.Text.Encoding.UTF8.GetBytes(
            $$"""{"iss":"https://imauth.bora.dopa.go.th","aud":"test-thaid-client-id","exp":{{exp}},"nonce":"my-nonce-001","pid":"1100501234567","given_name":"สมชาย"}"""));
        var signingInput = $"{headerB64}.{payloadB64}";
        var sigBytes = ecdsa.SignData(System.Text.Encoding.ASCII.GetBytes(signingInput), System.Security.Cryptography.HashAlgorithmName.SHA256);
        var jwt = $"{signingInput}.{ToBase64Url(sigBytes)}";

        // 3. ValidateIdTokenAsync succeeds with valid ES256 signature and matching nonce
        var validResult = await provider.ValidateIdTokenAsync(jwt, expectedNonce: "my-nonce-001");
        Assert.True(validResult.IsSuccess);
        Assert.Equal("1100501234567", validResult.Data["pid"]);
        Assert.Equal("สมชาย", validResult.Data["given_name"]);

        // 4. ValidateIdTokenAsync fails when nonce mismatches
        var wrongNonceResult = await provider.ValidateIdTokenAsync(jwt, expectedNonce: "wrong-nonce");
        Assert.False(wrongNonceResult.IsSuccess);
        Assert.Equal("INVALID_NONCE", wrongNonceResult.ErrorCode);

        // 5. ValidateIdTokenAsync fails when signature is tampered
        var tamperedJwt = $"{signingInput}.{ToBase64Url(new byte[64])}";
        var badSigResult = await provider.ValidateIdTokenAsync(tamperedJwt, expectedNonce: "my-nonce-001");
        Assert.False(badSigResult.IsSuccess);
        Assert.Equal("INVALID_ID_TOKEN_SIGNATURE", badSigResult.ErrorCode);
    }
}
