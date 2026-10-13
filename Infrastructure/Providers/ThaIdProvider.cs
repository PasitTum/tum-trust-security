using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Netka.Identity.Core.Attributes;
using Netka.Identity.Core.Exceptions;
using Netka.Identity.Core.Interfaces;
using Netka.Identity.Core.Models;
using Netka.Identity.Infrastructure.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Netka.Identity.Infrastructure.Providers;

/// <summary>
/// ThaID (DOPA Digital ID) identity verification provider implementing <see cref="IThaIdProvider"/>.
/// Provides direct methods for every official ThaID OAuth 2.0 / OIDC endpoint and returns the provider response unmodified.
/// </summary>
[AuditLog("IdentityVerification", "ThaID", Description = "Verify citizen identity with DOPA ThaID")]
public class ThaIdProvider : IThaIdProvider
{
    // Official DOPA ThaID default endpoints (can be overridden in appsettings.json or Database)
    private const string DefaultIssuer = "https://imauth.bora.dopa.go.th";
    private const string DefaultDiscoveryUrl = "https://imauth.bora.dopa.go.th/.well-known/openid-configuration";
    private const string DefaultJwksUrl = "https://imauth.bora.dopa.go.th/jwks/";
    private const string DefaultAuthorizationUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/";
    private const string DefaultTokenUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/token/";
    private const string DefaultInspectUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/introspect/";
    private const string DefaultRevokeUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/revoke/";
    private const string DefaultUserInfoUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/userinfo/";

    /// <inheritdoc />
    public string ProviderName => "ThaID";

    private readonly IConfiguration? _configuration;
    private readonly IIdentityOptionsProvider? _optionsProvider;
    private readonly ThaIdOptions? _staticOptions;

    private readonly HttpClient _httpClient;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ThaIdProvider> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ThaIdProvider"/> using <see cref="IConfiguration"/>.
    /// </summary>
    public ThaIdProvider(
        IConfiguration configuration,
        HttpClient httpClient,
        IEncryptionService encryptionService,
        ILogger<ThaIdProvider> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ThaIdProvider"/> using a <see cref="ThaIdOptions"/> object (e.g., loaded from a Database).
    /// </summary>
    public ThaIdProvider(
        ThaIdOptions options,
        HttpClient httpClient,
        IEncryptionService encryptionService,
        ILogger<ThaIdProvider> logger)
    {
        _staticOptions = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ThaIdProvider"/> using a dynamic <see cref="IIdentityOptionsProvider"/> (e.g., Database-backed configuration).
    /// </summary>
    public ThaIdProvider(
        IIdentityOptionsProvider optionsProvider,
        HttpClient httpClient,
        IEncryptionService encryptionService,
        ILogger<ThaIdProvider> logger)
    {
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ThaIdOptions ResolveOptions()
    {
        if (_optionsProvider is not null)
        {
            var root = _optionsProvider.GetOptions();
            if (root is null) return new ThaIdOptions();
            if (root.UseMock)
            {
                root.ThaId.UseMock = true;
                if (!string.IsNullOrWhiteSpace(root.MockScenario) && root.ThaId.MockScenario == "success")
                {
                    root.ThaId.MockScenario = root.MockScenario;
                }
            }
            return root.ThaId;
        }

        if (_configuration is not null)
        {
            return IdentityVerificationOptions.FromConfiguration(_configuration).ThaId;
        }

        return _staticOptions ?? new ThaIdOptions();
    }

    /// <inheritdoc />
    public string GetAuthorizationUrl(string redirectUri, string state)
    {
        return GetAuthorizationUrl(redirectUri, state, codeChallenge: null);
    }

    /// <inheritdoc />
    public string GetAuthorizationUrl(
        string redirectUri,
        string state,
        string? codeChallenge,
        string codeChallengeMethod = "S256",
        string? nonce = null)
    {
        var opts = ResolveOptions();

        var targetRedirectUri = !string.IsNullOrWhiteSpace(redirectUri)
            ? redirectUri
            : opts.RedirectUri;

        if (string.IsNullOrWhiteSpace(targetRedirectUri))
        {
            throw new ArgumentException("Redirect URI cannot be null or whitespace.", nameof(redirectUri));
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("State cannot be null or whitespace.", nameof(state));
        }

        if (opts.UseMock)
        {
            var separator = targetRedirectUri!.Contains('?') ? "&" : "?";
            var scenario = (opts.MockScenario ?? "success").Trim().ToLowerInvariant();
            var loopbackUrl = scenario switch
            {
                "deny" => $"{targetRedirectUri}{separator}error=access_denied&error_description=User+cancelled+login&state={Uri.EscapeDataString(state)}",
                "invalid_state" => $"{targetRedirectUri}{separator}code=mock-auth-code-001&state=tampered-invalid-state",
                "expired_code" => $"{targetRedirectUri}{separator}code=mock-expired-code&state={Uri.EscapeDataString(state)}",
                "timeout" => $"{targetRedirectUri}{separator}code=mock-timeout&state={Uri.EscapeDataString(state)}",
                _ => $"{targetRedirectUri}{separator}code=mock-auth-code-001&state={Uri.EscapeDataString(state)}"
            };

            _logger.LogWarning(
                "[MOCK-MODE] ThaID UseMock=true ({Scenario}): Generated Loopback Callback URL -> {LoopbackUrl}",
                scenario,
                loopbackUrl);
            return loopbackUrl;
        }

        if (string.IsNullOrWhiteSpace(opts.ClientId))
        {
            throw new InvalidOperationException("Missing required configuration: IdentityVerification:ThaId:ClientId");
        }

        var scope = !string.IsNullOrWhiteSpace(opts.Scope)
            ? opts.Scope!
            : "openid pid profile";

        var authorizationUrl = !string.IsNullOrWhiteSpace(opts.AuthorizeUrl)
            ? opts.AuthorizeUrl!
            : DefaultAuthorizationUrl;

        var query = new Dictionary<string, string>
        {
            ["client_id"] = opts.ClientId!,
            ["redirect_uri"] = targetRedirectUri!,
            ["response_type"] = "code",
            ["scope"] = scope,
            ["state"] = state
        };

        if (!string.IsNullOrWhiteSpace(nonce))
        {
            query["nonce"] = nonce!;
        }

        if (!string.IsNullOrWhiteSpace(codeChallenge))
        {
            query["code_challenge"] = codeChallenge!;
            query["code_challenge_method"] = string.IsNullOrWhiteSpace(codeChallengeMethod) ? "S256" : codeChallengeMethod;
        }

        var queryString = string.Join("&", query.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        _logger.LogInformation("Generated ThaID authorization URL for RedirectUri: {RedirectUri}", targetRedirectUri);
        return $"{authorizationUrl}?{queryString}";
    }

    /// <inheritdoc />
    [AuditLog("VerifyAsync", "ThaID", TrackDiff = false)]
    public async Task<VerificationResult> VerifyAsync(
        VerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var expectedState = request.AdditionalParameters.GetStringValue("expected_state");
        if (!string.IsNullOrEmpty(expectedState) &&
            !string.Equals(request.State, expectedState, StringComparison.Ordinal))
        {
            return ProviderHelper.CreateFailureResult("INVALID_STATE", "OAuth state validation failed.");
        }

        var codeVerifier = request.AdditionalParameters.GetStringValue("code_verifier");
        return await ExchangeCodeAsync(
            request.AuthorizationCode ?? string.Empty,
            request.RedirectUri,
            codeVerifier,
            cancellationToken);
    }

    /// <inheritdoc />
    [AuditLog("ExchangeCodeAsync", "ThaID", TrackDiff = false)]
    public async Task<VerificationResult> ExchangeCodeAsync(
        string authorizationCode,
        string? redirectUri = null,
        string? codeVerifier = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode))
        {
            return ProviderHelper.CreateFailureResult("MISSING_AUTH_CODE", "Authorization code is required.");
        }

        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            return ExecuteMockTokenExchange(authorizationCode);
        }

        if (string.IsNullOrWhiteSpace(opts.ClientId) || string.IsNullOrWhiteSpace(opts.ClientSecret))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_CONFIGURATION",
                "Missing required configuration: IdentityVerification:ThaId:ClientId and ClientSecret are required.");
        }

        var targetRedirectUri = !string.IsNullOrWhiteSpace(redirectUri)
            ? redirectUri
            : opts.RedirectUri;

        if (string.IsNullOrWhiteSpace(targetRedirectUri))
        {
            return ProviderHelper.CreateFailureResult("MISSING_REDIRECT_URI", "Redirect URI is required.");
        }

        var formFields = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = authorizationCode,
            ["client_id"] = opts.ClientId!,
            ["client_secret"] = opts.ClientSecret!,
            ["redirect_uri"] = targetRedirectUri!
        };

        if (!string.IsNullOrWhiteSpace(codeVerifier))
        {
            formFields["code_verifier"] = codeVerifier!;
        }

        var tokenUrl = !string.IsNullOrWhiteSpace(opts.TokenUrl) ? opts.TokenUrl! : DefaultTokenUrl;

        return await ExecutePostFormAsync(
            tokenUrl,
            opts,
            formFields,
            "TOKEN_EXCHANGE_FAILED",
            "ThaID token exchange successful",
            cancellationToken);
    }

    /// <inheritdoc />
    [AuditLog("RefreshTokenAsync", "ThaID", TrackDiff = false)]
    public async Task<VerificationResult> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return ProviderHelper.CreateFailureResult("MISSING_REFRESH_TOKEN", "Refresh token is required.");
        }

        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            return ExecuteMockTokenExchange("mock-refresh-code");
        }

        var tokenUrl = !string.IsNullOrWhiteSpace(opts.TokenUrl) ? opts.TokenUrl! : DefaultTokenUrl;

        var formFields = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        };

        return await ExecutePostFormAsync(
            tokenUrl,
            opts,
            formFields,
            "TOKEN_REFRESH_FAILED",
            "ThaID token refresh successful",
            cancellationToken);
    }

    /// <inheritdoc />
    [AuditLog("InspectTokenAsync", "ThaID", TrackDiff = false)]
    public async Task<VerificationResult> InspectTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return ProviderHelper.CreateFailureResult("MISSING_TOKEN", "Access token is required for inspection.");
        }

        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            _logger.LogWarning("[MOCK-MODE] ThaID InspectTokenAsync returning simulated active token response.");
            var isActive = !string.Equals(accessToken, "revoked-token", StringComparison.OrdinalIgnoreCase);
            return new VerificationResult
            {
                IsSuccess = true,
                Message = "ThaID token inspection successful (Mock)",
                Data = new Dictionary<string, object>
                {
                    ["active"] = isActive,
                    ["scope"] = "openid pid title title_en name name_en given_name family_name given_name_en family_name_en gender birthdate address house_address smartcard_code date_of_issuance date_of_expiry ial",
                    ["sub"] = "thaid-mock-sub-001"
                }
            };
        }

        var inspectUrl = !string.IsNullOrWhiteSpace(opts.InspectUrl) ? opts.InspectUrl! : DefaultInspectUrl;

        var formFields = new Dictionary<string, string>
        {
            ["token"] = accessToken
        };

        return await ExecutePostFormAsync(
            inspectUrl,
            opts,
            formFields,
            "TOKEN_INSPECT_FAILED",
            "ThaID token inspection successful",
            cancellationToken);
    }

    /// <inheritdoc />
    [AuditLog("RevokeTokenAsync", "ThaID", TrackDiff = false)]
    public async Task<VerificationResult> RevokeTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ProviderHelper.CreateFailureResult("MISSING_TOKEN", "Token is required for revocation.");
        }

        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            _logger.LogWarning("[MOCK-MODE] ThaID RevokeTokenAsync returning simulated revoked response.");
            return new VerificationResult
            {
                IsSuccess = true,
                Message = "ThaID token revocation successful (Mock)",
                Data = new Dictionary<string, object>
                {
                    ["message"] = "Token has been revoked successfully."
                }
            };
        }

        var revokeUrl = !string.IsNullOrWhiteSpace(opts.RevokeUrl) ? opts.RevokeUrl! : DefaultRevokeUrl;

        var formFields = new Dictionary<string, string>
        {
            ["token"] = token
        };

        return await ExecutePostFormAsync(
            revokeUrl,
            opts,
            formFields,
            "TOKEN_REVOKE_FAILED",
            "ThaID token revocation successful",
            cancellationToken);
    }

    /// <inheritdoc />
    [AuditLog("GetUserInfoAsync", "ThaID", TrackDiff = false)]
    public async Task<VerificationResult> GetUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return ProviderHelper.CreateFailureResult("MISSING_ACCESS_TOKEN", "Access token is required.");
        }

        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            _logger.LogWarning("[MOCK-MODE] ThaID GetUserInfoAsync returning simulated 20-scope citizen profile.");
            var mockProfile = CreateMockThaIdCitizenClaims();
            _logger.LogEncryptedPiiIfPresent(_encryptionService, ProviderName, mockProfile, "pid", "citizen_id");
            return new VerificationResult
            {
                IsSuccess = true,
                Message = "ThaID userinfo retrieval successful (Mock)",
                Data = mockProfile
            };
        }

        var userInfoUrl = !string.IsNullOrWhiteSpace(opts.UserInfoUrl) ? opts.UserInfoUrl! : DefaultUserInfoUrl;

        if (string.IsNullOrWhiteSpace(userInfoUrl))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_USERINFO_URL",
                "IdentityVerification:ThaId:UserInfoUrl is not configured.");
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            ApplyApiKeyHeader(httpRequest, opts);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return ProviderHelper.CreateFailureResult(
                    "USERINFO_FETCH_FAILED",
                    $"ThaID userinfo request failed with HTTP {(int)response.StatusCode}.",
                    (int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseData = ProviderHelper.ParseJsonToDictionary(json, "INVALID_USERINFO_RESPONSE");

            _logger.LogEncryptedPiiIfPresent(_encryptionService, ProviderName, responseData, "pid", "citizen_id");

            return new VerificationResult
            {
                IsSuccess = true,
                Message = "ThaID userinfo retrieval successful",
                Data = responseData
            };
        }
        catch (VerificationException ex)
        {
            _logger.LogError(ex, "ThaID GetUserInfoAsync failed: {ErrorCode}", ex.ErrorCode);
            return ProviderHelper.CreateFailureResult(ex.ErrorCode, ex.Message, ex.HttpStatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling ThaID userinfo endpoint.");
            return ProviderHelper.CreateFailureResult("NETWORK_ERROR", $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in ThaID GetUserInfoAsync.");
            return ProviderHelper.CreateFailureResult("INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    [AuditLog("GetOpenIdConfigurationAsync", "ThaID", TrackDiff = false)]
    public async Task<VerificationResult> GetOpenIdConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            _logger.LogWarning("[MOCK-MODE] ThaID GetOpenIdConfigurationAsync returning simulated OIDC Discovery metadata.");
            var issuer = !string.IsNullOrWhiteSpace(opts.Issuer) ? opts.Issuer! : DefaultIssuer;
            return new VerificationResult
            {
                IsSuccess = true,
                Message = "ThaID OpenID configuration retrieval successful (Mock)",
                Data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["issuer"] = issuer,
                    ["authorization_endpoint"] = !string.IsNullOrWhiteSpace(opts.AuthorizeUrl) ? opts.AuthorizeUrl! : DefaultAuthorizationUrl,
                    ["token_endpoint"] = !string.IsNullOrWhiteSpace(opts.TokenUrl) ? opts.TokenUrl! : DefaultTokenUrl,
                    ["userinfo_endpoint"] = !string.IsNullOrWhiteSpace(opts.UserInfoUrl) ? opts.UserInfoUrl! : DefaultUserInfoUrl,
                    ["revocation_endpoint"] = !string.IsNullOrWhiteSpace(opts.RevokeUrl) ? opts.RevokeUrl! : DefaultRevokeUrl,
                    ["introspection_endpoint"] = !string.IsNullOrWhiteSpace(opts.InspectUrl) ? opts.InspectUrl! : DefaultInspectUrl,
                    ["jwks_uri"] = !string.IsNullOrWhiteSpace(opts.JwksUrl) ? opts.JwksUrl! : DefaultJwksUrl,
                    ["grant_types_supported"] = new List<object?> { "authorization_code", "refresh_token" },
                    ["token_endpoint_auth_methods_supported"] = new List<object?> { "client_secret_basic", "client_secret_post" },
                    ["id_token_signing_alg_values_supported"] = new List<object?> { "ES256" }
                }
            };
        }

        var discoveryUrl = !string.IsNullOrWhiteSpace(opts.DiscoveryUrl) ? opts.DiscoveryUrl! : DefaultDiscoveryUrl;
        return await ExecuteGetJsonAsync(
            discoveryUrl,
            opts,
            "DISCOVERY_FETCH_FAILED",
            "INVALID_DISCOVERY_RESPONSE",
            "ThaID OpenID configuration retrieval successful",
            cancellationToken);
    }

    /// <inheritdoc />
    [AuditLog("GetJwksAsync", "ThaID", TrackDiff = false)]
    public async Task<VerificationResult> GetJwksAsync(
        CancellationToken cancellationToken = default)
    {
        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            _logger.LogWarning("[MOCK-MODE] ThaID GetJwksAsync returning simulated JWKS metadata.");
            return new VerificationResult
            {
                IsSuccess = true,
                Message = "ThaID JWKS retrieval successful (Mock)",
                Data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["keys"] = new List<object?>
                    {
                        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["kty"] = "EC",
                            ["crv"] = "P-256",
                            ["use"] = "sig",
                            ["alg"] = "ES256",
                            ["kid"] = "thaid-mock-key-001"
                        }
                    }
                }
            };
        }

        var jwksUrl = !string.IsNullOrWhiteSpace(opts.JwksUrl) ? opts.JwksUrl! : DefaultJwksUrl;
        return await ExecuteGetJsonAsync(
            jwksUrl,
            opts,
            "JWKS_FETCH_FAILED",
            "INVALID_JWKS_RESPONSE",
            "ThaID JWKS retrieval successful",
            cancellationToken);
    }

    /// <inheritdoc />
    [AuditLog("ValidateIdTokenAsync", "ThaID", TrackDiff = false)]
    public async Task<VerificationResult> ValidateIdTokenAsync(
        string idToken,
        string? expectedNonce = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return ProviderHelper.CreateFailureResult("MISSING_ID_TOKEN", "ID token is required for validation.");
        }

        var opts = ResolveOptions();
        if (opts.UseMock && !idToken.Contains('.'))
        {
            return ProviderHelper.CreateFailureResult("INVALID_ID_TOKEN", "ID token must be a valid JWT format.");
        }

        if (opts.UseMock && idToken.StartsWith("eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.mock-id-token", StringComparison.Ordinal))
        {
            _logger.LogWarning("[MOCK-MODE] ThaID ValidateIdTokenAsync returning simulated decoded ID token claims.");
            var mockClaims = CreateMockThaIdCitizenClaims();
            mockClaims["iss"] = !string.IsNullOrWhiteSpace(opts.Issuer) ? opts.Issuer! : DefaultIssuer;
            mockClaims["aud"] = !string.IsNullOrWhiteSpace(opts.ClientId) ? opts.ClientId! : "thaid-mock-client";
            mockClaims["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
            mockClaims["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (!string.IsNullOrWhiteSpace(expectedNonce))
            {
                mockClaims["nonce"] = expectedNonce!;
            }
            _logger.LogEncryptedPiiIfPresent(_encryptionService, ProviderName, mockClaims, "pid", "citizen_id");
            return new VerificationResult
            {
                IsSuccess = true,
                Message = "ThaID ID token validation successful (Mock)",
                Data = mockClaims
            };
        }

        var parts = idToken.Split('.');
        if (parts.Length != 3)
        {
            return ProviderHelper.CreateFailureResult("INVALID_ID_TOKEN", "ID token must be a 3-part JWT (header.payload.signature).");
        }

        IDictionary<string, object> headerData;
        IDictionary<string, object> payloadData;
        try
        {
            var headerJson = Encoding.UTF8.GetString(DecodeBase64Url(parts[0]));
            var payloadJson = Encoding.UTF8.GetString(DecodeBase64Url(parts[1]));
            headerData = ProviderHelper.ParseJsonToDictionary(headerJson, "INVALID_ID_TOKEN");
            payloadData = ProviderHelper.ParseJsonToDictionary(payloadJson, "INVALID_ID_TOKEN");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decode ThaID ID token JWT payload.");
            return ProviderHelper.CreateFailureResult("INVALID_ID_TOKEN", "Failed to decode JWT header or payload.");
        }

        // 1. Validate Expiration (exp)
        if (payloadData.TryGetValue("exp", out var expObj) && TryConvertToLong(expObj, out var expUnix))
        {
            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (expUnix <= nowUnix - 60)
            {
                return ProviderHelper.CreateFailureResult("ID_TOKEN_EXPIRED", "The ThaID ID token has expired.");
            }
        }

        // 2. Validate Issuer (iss)
        var expectedIssuer = !string.IsNullOrWhiteSpace(opts.Issuer) ? opts.Issuer!.TrimEnd('/') : DefaultIssuer;
        var actualIssuer = payloadData.GetStringValue("iss")?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(actualIssuer) &&
            !string.Equals(actualIssuer, expectedIssuer, StringComparison.OrdinalIgnoreCase))
        {
            return ProviderHelper.CreateFailureResult(
                "INVALID_ISSUER",
                $"ID token issuer '{actualIssuer}' does not match expected issuer '{expectedIssuer}'.");
        }

        // 3. Validate Audience (aud) when ClientId is configured
        if (!string.IsNullOrWhiteSpace(opts.ClientId) && payloadData.TryGetValue("aud", out var audObj) && audObj is not null)
        {
            var audMatches = false;
            if (audObj is string audStr)
            {
                audMatches = string.Equals(audStr, opts.ClientId, StringComparison.Ordinal);
            }
            else if (audObj is IEnumerable<object?> audList)
            {
                audMatches = audList.Any(a => string.Equals(a?.ToString(), opts.ClientId, StringComparison.Ordinal));
            }

            if (!audMatches)
            {
                return ProviderHelper.CreateFailureResult(
                    "INVALID_AUDIENCE",
                    $"ID token audience does not match configured ClientId '{opts.ClientId}'.");
            }
        }

        // 4. Validate Nonce when expectedNonce is specified
        if (!string.IsNullOrWhiteSpace(expectedNonce))
        {
            var actualNonce = payloadData.GetStringValue("nonce");
            if (!string.Equals(actualNonce, expectedNonce, StringComparison.Ordinal))
            {
                return ProviderHelper.CreateFailureResult("INVALID_NONCE", "ID token nonce does not match the expected nonce.");
            }
        }

        // 5. Verify Signature against JWKS when JwksUrl is explicitly configured and not in mock mode
        if (!opts.UseMock && !string.IsNullOrWhiteSpace(opts.JwksUrl))
        {
            var jwksResult = await GetJwksAsync(cancellationToken);
            if (!jwksResult.IsSuccess)
            {
                return ProviderHelper.CreateFailureResult(
                    jwksResult.ErrorCode ?? "JWKS_FETCH_FAILED",
                    jwksResult.Message ?? "Failed to retrieve JWKS for ID token signature validation.",
                    jwksResult.Error?.HttpStatusCode);
            }

            if (!VerifyJwtSignatureWithJwks(parts[0], parts[1], parts[2], headerData, jwksResult.Data))
            {
                return ProviderHelper.CreateFailureResult(
                    "INVALID_ID_TOKEN_SIGNATURE",
                    "ID token cryptographic signature verification against ThaID JWKS failed.");
            }
        }

        _logger.LogEncryptedPiiIfPresent(_encryptionService, ProviderName, payloadData, "pid", "citizen_id");

        return new VerificationResult
        {
            IsSuccess = true,
            Message = "ThaID ID token validation successful",
            Data = payloadData
        };
    }

    private VerificationResult ExecuteMockTokenExchange(string authorizationCode)
    {
        if (string.Equals(authorizationCode, "mock-expired-code", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[MOCK-MODE] ThaID ExchangeCodeAsync simulating UC-THAID-03 (Expired/Invalid Code -> HTTP 400).");
            return ProviderHelper.CreateFailureResult(
                "TOKEN_EXCHANGE_FAILED",
                "ThaID request failed with HTTP 400 (Mock: invalid_grant / expired code).",
                400);
        }

        if (string.Equals(authorizationCode, "mock-timeout", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[MOCK-MODE] ThaID ExchangeCodeAsync simulating UC-THAID-04 (Network Timeout -> NETWORK_ERROR).");
            return ProviderHelper.CreateFailureResult(
                "NETWORK_ERROR",
                "Network error: Simulated ThaID connection timeout (Mock).");
        }

        _logger.LogWarning("[MOCK-MODE] ThaID ExchangeCodeAsync returning simulated 20-scope token response.");
        var responseData = CreateMockThaIdCitizenClaims();
        responseData["access_token"] = "mock-thaid-access-token-2026";
        responseData["token_type"] = "Bearer";
        responseData["expires_in"] = 3600L;
        responseData["scope"] = "openid pid title title_en name name_en given_name middle_name family_name given_name_en middle_name_en family_name_en gender birthdate address house_address smartcard_code date_of_issuance date_of_expiry ial";
        responseData["refresh_token"] = "mock-thaid-refresh-token-2026";
        responseData["id_token"] = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.mock-id-token";

        _logger.LogEncryptedPiiIfPresent(_encryptionService, ProviderName, responseData, "pid", "citizen_id");

        return new VerificationResult
        {
            IsSuccess = true,
            Message = "ThaID token exchange successful (Mock)",
            Data = responseData
        };
    }

    private static Dictionary<string, object> CreateMockThaIdCitizenClaims()
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["sub"] = "thaid-mock-sub-001",
            ["pid"] = "1100501234561",
            ["title"] = "นาย",
            ["title_en"] = "Mr.",
            ["name"] = "นาย สมชาย ใจดี",
            ["name_en"] = "Mr. Somchai Jaidee",
            ["given_name"] = "สมชาย",
            ["middle_name"] = "",
            ["family_name"] = "ใจดี",
            ["given_name_en"] = "Somchai",
            ["middle_name_en"] = "",
            ["family_name_en"] = "Jaidee",
            ["gender"] = "male",
            ["birthdate"] = "1990-05-15",
            ["address"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["formatted"] = "99/9 หมู่ที่ 1 ถนนแจ้งวัฒนะ ตำบลคลองเกลือ อำเภอปากเกร็ด จังหวัดนนทบุรี 11120",
                ["house_number"] = "99/9",
                ["moo"] = "หมู่ที่ 1",
                ["trok"] = "",
                ["soi"] = "",
                ["road"] = "ถนนแจ้งวัฒนะ",
                ["subdistrict"] = "คลองเกลือ",
                ["district"] = "ปากเกร็ด",
                ["province"] = "นนทบุรี",
                ["postal_code"] = "11120"
            },
            ["house_address"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["raw"] = "99/9#1####ถนนแจ้งวัฒนะ#คลองเกลือ#ปากเกร็ด#นนทบุรี"
            },
            ["smartcard_code"] = "1001012345678901",
            ["date_of_issuance"] = "2021-05-15",
            ["date_of_expiry"] = "2030-05-14",
            ["ial"] = 2.3
        };
    }

    private static void ApplyApiKeyHeader(HttpRequestMessage httpRequest, ThaIdOptions opts)
    {
        if (!string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            var headerName = !string.IsNullOrWhiteSpace(opts.ApiKeyHeader) ? opts.ApiKeyHeader! : "x-api-key";
            httpRequest.Headers.TryAddWithoutValidation(headerName, opts.ApiKey);
        }
    }

    private async Task<VerificationResult> ExecuteGetJsonAsync(
        string endpointUrl,
        ThaIdOptions opts,
        string errorCodeOnFailure,
        string invalidJsonErrorCode,
        string successMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpointUrl);
            ApplyApiKeyHeader(httpRequest, opts);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return ProviderHelper.CreateFailureResult(
                    errorCodeOnFailure,
                    $"ThaID GET request failed with HTTP {(int)response.StatusCode}.",
                    (int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseData = ProviderHelper.ParseJsonToDictionary(json, invalidJsonErrorCode);

            return new VerificationResult
            {
                IsSuccess = true,
                Message = successMessage,
                Data = responseData
            };
        }
        catch (VerificationException ex)
        {
            _logger.LogError(ex, "ThaID GET operation failed: {ErrorCode}", ex.ErrorCode);
            return ProviderHelper.CreateFailureResult(ex.ErrorCode, ex.Message, ex.HttpStatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling ThaID endpoint {EndpointUrl}.", endpointUrl);
            return ProviderHelper.CreateFailureResult("NETWORK_ERROR", $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling ThaID endpoint {EndpointUrl}.", endpointUrl);
            return ProviderHelper.CreateFailureResult("INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private async Task<VerificationResult> ExecutePostFormAsync(
        string endpointUrl,
        ThaIdOptions opts,
        IDictionary<string, string> formFields,
        string errorCodeOnFailure,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(opts.ClientId) || string.IsNullOrWhiteSpace(opts.ClientSecret))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_CONFIGURATION",
                "Missing required configuration: IdentityVerification:ThaId:ClientId and ClientSecret are required.");
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpointUrl)
            {
                Content = new FormUrlEncodedContent(
                    formFields.Select(kvp => new KeyValuePair<string?, string?>(kvp.Key, kvp.Value)))
            };

            var basicCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{opts.ClientId}:{opts.ClientSecret}"));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicCredentials);
            ApplyApiKeyHeader(httpRequest, opts);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return ProviderHelper.CreateFailureResult(
                    errorCodeOnFailure,
                    $"ThaID request failed with HTTP {(int)response.StatusCode}.",
                    (int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseData = ProviderHelper.ParseJsonToDictionary(json, "INVALID_THAID_RESPONSE");

            _logger.LogEncryptedPiiIfPresent(_encryptionService, ProviderName, responseData, "pid", "citizen_id");

            return new VerificationResult
            {
                IsSuccess = true,
                Message = successMessage,
                Data = responseData
            };
        }
        catch (VerificationException ex)
        {
            _logger.LogError(ex, "ThaID operation failed: {ErrorCode}", ex.ErrorCode);
            return ProviderHelper.CreateFailureResult(ex.ErrorCode, ex.Message, ex.HttpStatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling ThaID endpoint {EndpointUrl}.", endpointUrl);
            return ProviderHelper.CreateFailureResult("NETWORK_ERROR", $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling ThaID endpoint {EndpointUrl}.", endpointUrl);
            return ProviderHelper.CreateFailureResult("INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static byte[] DecodeBase64Url(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    private static bool TryConvertToLong(object? value, out long result)
    {
        switch (value)
        {
            case long l:
                result = l;
                return true;
            case int i:
                result = i;
                return true;
            case double d:
                result = (long)d;
                return true;
            case string s when long.TryParse(s, out var parsed):
                result = parsed;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private static bool VerifyJwtSignatureWithJwks(
        string headerB64,
        string payloadB64,
        string signatureB64,
        IDictionary<string, object> headerData,
        IDictionary<string, object> jwksData)
    {
        if (!jwksData.TryGetValue("keys", out var keysObj) || keysObj is not IEnumerable<object?> keysList)
        {
            return false;
        }

        var kid = headerData.GetStringValue("kid");
        var alg = headerData.GetStringValue("alg") ?? "ES256";
        var signedBytes = Encoding.ASCII.GetBytes($"{headerB64}.{payloadB64}");
        byte[] signatureBytes;
        try
        {
            signatureBytes = DecodeBase64Url(signatureB64);
        }
        catch
        {
            return false;
        }

        foreach (var keyItem in keysList)
        {
            if (keyItem is not IDictionary<string, object> jwk) continue;

            var jwkKid = jwk.GetStringValue("kid");
            if (!string.IsNullOrWhiteSpace(kid) &&
                !string.IsNullOrWhiteSpace(jwkKid) &&
                !string.Equals(kid, jwkKid, StringComparison.Ordinal))
            {
                continue;
            }

            var kty = jwk.GetStringValue("kty");
            try
            {
                if (string.Equals(kty, "EC", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(alg, "ES256", StringComparison.OrdinalIgnoreCase))
                {
                    var xStr = jwk.GetStringValue("x");
                    var yStr = jwk.GetStringValue("y");
                    if (string.IsNullOrWhiteSpace(xStr) || string.IsNullOrWhiteSpace(yStr)) continue;

                    var ecParams = new ECParameters
                    {
                        Curve = ECCurve.NamedCurves.nistP256,
                        Q = new ECPoint
                        {
                            X = DecodeBase64Url(xStr!),
                            Y = DecodeBase64Url(yStr!)
                        }
                    };
                    using var ecdsa = ECDsa.Create(ecParams);
                    if (ecdsa.VerifyData(signedBytes, signatureBytes, HashAlgorithmName.SHA256))
                    {
                        return true;
                    }
                }
                else if (string.Equals(kty, "RSA", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(alg, "RS256", StringComparison.OrdinalIgnoreCase))
                {
                    var nStr = jwk.GetStringValue("n");
                    var eStr = jwk.GetStringValue("e");
                    if (string.IsNullOrWhiteSpace(nStr) || string.IsNullOrWhiteSpace(eStr)) continue;

                    var rsaParams = new RSAParameters
                    {
                        Modulus = DecodeBase64Url(nStr!),
                        Exponent = DecodeBase64Url(eStr!)
                    };
                    using var rsa = RSA.Create();
                    rsa.ImportParameters(rsaParams);
                    if (rsa.VerifyData(signedBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Continue checking next key in JWKS if one fails
            }
        }

        return false;
    }
}
