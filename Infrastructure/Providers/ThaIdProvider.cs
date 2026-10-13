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
            return _optionsProvider.GetOptions()?.ThaId ?? new ThaIdOptions();
        }

        if (_configuration is not null)
        {
            return ThaIdOptions.FromSection(_configuration.GetSection("IdentityVerification:ThaId"));
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
        if (string.IsNullOrWhiteSpace(opts.ClientId))
        {
            throw new InvalidOperationException("Missing required configuration: IdentityVerification:ThaId:ClientId");
        }

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
        var opts = ResolveOptions();
        var userInfoUrl = !string.IsNullOrWhiteSpace(opts.UserInfoUrl) ? opts.UserInfoUrl! : DefaultUserInfoUrl;

        if (string.IsNullOrWhiteSpace(userInfoUrl))
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
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
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
