using System.Net.Http.Headers;
using System.Text;
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
    // Official DOPA ThaID default endpoints (can be overridden in appsettings.json)
    private const string DefaultAuthorizationUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/";
    private const string DefaultTokenUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/token/";
    private const string DefaultInspectUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/introspect/";
    private const string DefaultRevokeUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/revoke/";
    private const string DefaultUserInfoUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/userinfo/";

    /// <inheritdoc />
    public string ProviderName => "ThaID";

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _authorizationUrl;
    private readonly string _tokenUrl;
    private readonly string _inspectUrl;
    private readonly string _revokeUrl;
    private readonly string _userInfoUrl;
    private readonly string? _defaultRedirectUri;
    private readonly string _scope;

    private readonly HttpClient _httpClient;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ThaIdProvider> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ThaIdProvider"/>.
    /// </summary>
    public ThaIdProvider(
        IConfiguration configuration,
        HttpClient httpClient,
        IEncryptionService encryptionService,
        ILogger<ThaIdProvider> logger)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (httpClient is null) throw new ArgumentNullException(nameof(httpClient));
        if (encryptionService is null) throw new ArgumentNullException(nameof(encryptionService));
        if (logger is null) throw new ArgumentNullException(nameof(logger));

        var section = configuration.GetSection("IdentityVerification:ThaId");

        _clientId = section.GetRequiredValue("ClientId");
        _clientSecret = section.GetRequiredValue("ClientSecret");
        _authorizationUrl = section["AuthorizationUrl"] ?? DefaultAuthorizationUrl;
        _tokenUrl = section["TokenUrl"] ?? DefaultTokenUrl;
        _inspectUrl = section["InspectUrl"] ?? DefaultInspectUrl;
        _revokeUrl = section["RevokeUrl"] ?? DefaultRevokeUrl;
        _userInfoUrl = section["UserInfoUrl"] ?? DefaultUserInfoUrl;
        _defaultRedirectUri = section["RedirectUri"];
        _scope = !string.IsNullOrWhiteSpace(section["Scope"])
            ? section["Scope"]!
            : "openid pid profile";

        _httpClient = httpClient;
        _encryptionService = encryptionService;
        _logger = logger;
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
        var targetRedirectUri = !string.IsNullOrWhiteSpace(redirectUri)
            ? redirectUri
            : _defaultRedirectUri;

        if (string.IsNullOrWhiteSpace(targetRedirectUri))
        {
            throw new ArgumentException("Redirect URI cannot be null or whitespace.", nameof(redirectUri));
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("State cannot be null or whitespace.", nameof(state));
        }

        var query = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["redirect_uri"] = targetRedirectUri,
            ["response_type"] = "code",
            ["scope"] = _scope,
            ["state"] = state
        };

        if (!string.IsNullOrWhiteSpace(nonce))
        {
            query["nonce"] = nonce;
        }

        if (!string.IsNullOrWhiteSpace(codeChallenge))
        {
            query["code_challenge"] = codeChallenge;
            query["code_challenge_method"] = string.IsNullOrWhiteSpace(codeChallengeMethod) ? "S256" : codeChallengeMethod;
        }

        var queryString = string.Join("&", query.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        _logger.LogInformation("Generated ThaID authorization URL for RedirectUri: {RedirectUri}", targetRedirectUri);
        return $"{_authorizationUrl}?{queryString}";
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

        var targetRedirectUri = !string.IsNullOrWhiteSpace(redirectUri)
            ? redirectUri
            : _defaultRedirectUri;

        if (string.IsNullOrWhiteSpace(targetRedirectUri))
        {
            return ProviderHelper.CreateFailureResult("MISSING_REDIRECT_URI", "Redirect URI is required.");
        }

        var formFields = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = authorizationCode,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["redirect_uri"] = targetRedirectUri
        };

        if (!string.IsNullOrWhiteSpace(codeVerifier))
        {
            formFields["code_verifier"] = codeVerifier;
        }

        return await ExecutePostFormAsync(
            _tokenUrl,
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

        var formFields = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        };

        return await ExecutePostFormAsync(
            _tokenUrl,
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

        var formFields = new Dictionary<string, string>
        {
            ["token"] = accessToken
        };

        return await ExecutePostFormAsync(
            _inspectUrl,
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

        var formFields = new Dictionary<string, string>
        {
            ["token"] = token
        };

        return await ExecutePostFormAsync(
            _revokeUrl,
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
        if (string.IsNullOrWhiteSpace(_userInfoUrl))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_USERINFO_URL",
                "IdentityVerification:ThaId:UserInfoUrl is not configured.");
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return ProviderHelper.CreateFailureResult("MISSING_ACCESS_TOKEN", "Access token is required.");
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, _userInfoUrl);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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

    private async Task<VerificationResult> ExecutePostFormAsync(
        string endpointUrl,
        IDictionary<string, string> formFields,
        string errorCodeOnFailure,
        string successMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpointUrl)
            {
                Content = new FormUrlEncodedContent(
                    formFields.Select(kvp => new KeyValuePair<string?, string?>(kvp.Key, kvp.Value)))
            };

            var basicCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicCredentials);

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
}
