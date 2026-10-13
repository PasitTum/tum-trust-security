using System.Net.Http.Headers;
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
/// DOPA citizen card verification provider implementing <see cref="IDopaProvider"/>.
/// Provides direct methods for Laser ID verification and Smart Card Chip verification,
/// returning the exact provider response unmodified.
/// </summary>
[AuditLog("IdentityVerification", "DOPA", Description = "Verify citizen card data with DOPA")]
public class DopaProvider : IDopaProvider
{
    /// <inheritdoc />
    public string ProviderName => "DOPA";

    private readonly IConfiguration? _configuration;
    private readonly IIdentityOptionsProvider? _optionsProvider;
    private readonly DopaOptions? _staticOptions;
    private readonly HttpClient _httpClient;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<DopaProvider> _logger;

    private string? _cachedAccessToken;
    private DateTimeOffset _tokenExpiresAtUtc = DateTimeOffset.MinValue;

    private string? _cachedDgaToken;
    private DateTimeOffset _dgaTokenExpiresAtUtc = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of <see cref="DopaProvider"/> using <see cref="IConfiguration"/>.
    /// </summary>
    public DopaProvider(
        IConfiguration configuration,
        HttpClient httpClient,
        IEncryptionService encryptionService,
        ILogger<DopaProvider> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DopaProvider"/> using a <see cref="DopaOptions"/> object (e.g., loaded from a Database).
    /// </summary>
    public DopaProvider(
        DopaOptions options,
        HttpClient httpClient,
        IEncryptionService encryptionService,
        ILogger<DopaProvider> logger)
    {
        _staticOptions = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DopaProvider"/> using a dynamic <see cref="IIdentityOptionsProvider"/> (e.g., Database-backed configuration).
    /// </summary>
    public DopaProvider(
        IIdentityOptionsProvider optionsProvider,
        HttpClient httpClient,
        IEncryptionService encryptionService,
        ILogger<DopaProvider> logger)
    {
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private DopaOptions ResolveOptions()
    {
        if (_optionsProvider is not null)
        {
            var root = _optionsProvider.GetOptions();
            if (root is null) return new DopaOptions();
            if (root.UseMock)
            {
                root.Dopa.UseMock = true;
            }
            return root.Dopa;
        }

        if (_configuration is not null)
        {
            return IdentityVerificationOptions.FromConfiguration(_configuration).Dopa;
        }

        return _staticOptions ?? new DopaOptions();
    }

    /// <inheritdoc />
    [AuditLog("VerifyByLaserAsync", "DOPA", TrackDiff = false)]
    public async Task<VerificationResult> VerifyByLaserAsync(
        string pid,
        string firstName,
        string lastName,
        string birthDate,
        string laserId,
        CancellationToken cancellationToken = default)
    {
        if (!pid.IsValidThaiNationalId())
        {
            return ProviderHelper.CreateFailureResult(
                "INVALID_PID_FORMAT",
                "Citizen ID (pid) must be a valid 13-digit Thai National ID.");
        }

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(birthDate) ||
            string.IsNullOrWhiteSpace(laserId))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_PARAMETERS",
                "firstName, lastName, birthDate, and laserId are required for DOPA Laser verification.");
        }

        var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["PID"] = pid,
            ["FirstName"] = firstName,
            ["LastName"] = lastName,
            ["BirthDay"] = birthDate,
            ["Laser"] = laserId
        };

        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            return ExecuteMockDopaVerification(payload);
        }

        if (opts.Mode == DopaGatewayMode.DgaGdx)
        {
            return await ExecuteDgaGdxVerificationAsync(
                pid,
                firstName,
                lastName,
                birthDate,
                laserId,
                opts,
                cancellationToken);
        }

        var laserUrl = !string.IsNullOrWhiteSpace(opts.LaserUrl)
            ? opts.LaserUrl
            : opts.VerificationUrl;

        return await SendPostJsonAsync(laserUrl, opts, payload, cancellationToken);
    }

    /// <inheritdoc />
    [AuditLog("VerifyByChipAsync", "DOPA", TrackDiff = false)]
    public async Task<VerificationResult> VerifyByChipAsync(
        string pid,
        string chipNo,
        string? bp1No = null,
        CancellationToken cancellationToken = default)
    {
        if (!pid.IsValidThaiNationalId())
        {
            return ProviderHelper.CreateFailureResult(
                "INVALID_PID_FORMAT",
                "Citizen ID (pid) must be a valid 13-digit Thai National ID.");
        }

        if (string.IsNullOrWhiteSpace(chipNo))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_PARAMETERS",
                "Smart Card chipNo is required for DOPA Chip verification.");
        }

        var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["PID"] = pid,
            ["ChipNo"] = chipNo
        };

        if (!string.IsNullOrWhiteSpace(bp1No))
        {
            payload["bp1no"] = bp1No!;
        }

        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            return ExecuteMockDopaVerification(payload);
        }

        var chipUrl = !string.IsNullOrWhiteSpace(opts.ChipUrl)
            ? opts.ChipUrl
            : !string.IsNullOrWhiteSpace(opts.VerificationUrl)
                ? opts.VerificationUrl
                : opts.LaserUrl;

        return await SendPostJsonAsync(chipUrl, opts, payload, cancellationToken);
    }

    /// <inheritdoc />
    [AuditLog("VerifyByFaceAsync", "DOPA", TrackDiff = false)]
    public async Task<VerificationResult> VerifyByFaceAsync(
        string pid,
        string faceImageBase64,
        float? threshold = null,
        CancellationToken cancellationToken = default)
    {
        if (!pid.IsValidThaiNationalId())
        {
            return ProviderHelper.CreateFailureResult(
                "INVALID_PID_FORMAT",
                "Citizen ID (pid) must be a valid 13-digit Thai National ID.");
        }

        if (string.IsNullOrWhiteSpace(faceImageBase64))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_PARAMETERS",
                "faceImageBase64 is required for DOPA Face verification.");
        }

        var cleanBase64 = StripDataUriPrefix(faceImageBase64);
        var opts = ResolveOptions();
        var effectiveThreshold = threshold.HasValue && threshold.Value > 0f && threshold.Value <= 1.0f
            ? threshold.Value
            : opts.FaceThreshold > 0f && opts.FaceThreshold <= 1.0f
                ? opts.FaceThreshold
                : 0.80f;

        if (opts.UseMock)
        {
            return ExecuteMockFaceVerification(pid, cleanBase64, effectiveThreshold);
        }

        var faceUrl = !string.IsNullOrWhiteSpace(opts.FaceUrl)
            ? opts.FaceUrl
            : opts.VerificationUrl;

        if (string.IsNullOrWhiteSpace(faceUrl))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_CONFIGURATION",
                "Missing required configuration: IdentityVerification:Dopa:FaceUrl (or VerificationUrl).");
        }

        var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["pid"] = pid,
            ["image"] = cleanBase64
        };

        var result = await SendPostJsonAsync(faceUrl, opts, payload, cancellationToken);
        if (!result.IsSuccess)
        {
            return result;
        }

        return EvaluateFaceResponse(result.Data, effectiveThreshold);
    }

    /// <inheritdoc />
    [AuditLog("RequestAccessTokenAsync", "DOPA", TrackDiff = false)]
    public async Task<VerificationResult> RequestAccessTokenAsync(
        CancellationToken cancellationToken = default)
    {
        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            _logger.LogWarning("[MOCK-MODE] DOPA RequestAccessTokenAsync returning simulated OAuth client_credentials token.");
            _cachedAccessToken = "mock-dopa-access-token-2026";
            _tokenExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(3480);
            return new VerificationResult
            {
                IsSuccess = true,
                Message = "DOPA token acquisition successful (Mock)",
                Data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["access_token"] = _cachedAccessToken,
                    ["token_type"] = "Bearer",
                    ["expires_in"] = 3600L
                }
            };
        }

        if (string.IsNullOrWhiteSpace(opts.TokenUrl) ||
            string.IsNullOrWhiteSpace(opts.ClientId) ||
            string.IsNullOrWhiteSpace(opts.ClientSecret))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_CONFIGURATION",
                "Missing required configuration: IdentityVerification:Dopa:TokenUrl, ClientId, and ClientSecret are required for OAuth token acquisition.");
        }

        try
        {
            var formFields = new Dictionary<string, string?>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = opts.ClientId,
                ["client_secret"] = opts.ClientSecret
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, opts.TokenUrl)
            {
                Content = new FormUrlEncodedContent(
                    formFields.Select(kvp => new KeyValuePair<string?, string?>(kvp.Key, kvp.Value)))
            };

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return ProviderHelper.CreateFailureResult(
                    "DOPA_TOKEN_FAILED",
                    $"DOPA token endpoint failed with HTTP {(int)response.StatusCode}.",
                    (int)response.StatusCode);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseData = ProviderHelper.ParseJsonToDictionary(responseJson, "INVALID_DOPA_RESPONSE");

            var accessToken = responseData.GetStringValue("access_token");
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return ProviderHelper.CreateFailureResult(
                    "DOPA_TOKEN_FAILED",
                    "DOPA token endpoint returned an empty access_token.");
            }

            var expiresIn = 3600L;
            if (responseData.TryGetValue("expires_in", out var expObj) && expObj is not null)
            {
                if (expObj is long l) expiresIn = l;
                else if (expObj is int i) expiresIn = i;
                else if (long.TryParse(expObj.ToString(), out var parsed)) expiresIn = parsed;
            }

            _cachedAccessToken = accessToken;
            _tokenExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn - 120));

            return new VerificationResult
            {
                IsSuccess = true,
                Message = "DOPA token acquisition successful",
                Data = responseData
            };
        }
        catch (VerificationException ex)
        {
            _logger.LogError(ex, "DOPA token request failed: {ErrorCode}", ex.ErrorCode);
            return ProviderHelper.CreateFailureResult(ex.ErrorCode, ex.Message, ex.HttpStatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling DOPA token endpoint {TokenUrl}.", opts.TokenUrl);
            return ProviderHelper.CreateFailureResult("NETWORK_ERROR", $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling DOPA token endpoint.");
            return ProviderHelper.CreateFailureResult("INTERNAL_ERROR", "An unexpected error occurred during DOPA token acquisition.");
        }
    }

    /// <inheritdoc />
    [AuditLog("VerifyAsync", "DOPA", TrackDiff = false)]
    public async Task<VerificationResult> VerifyAsync(
        VerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        if (request.AdditionalParameters is null || request.AdditionalParameters.Count == 0)
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_PARAMETERS",
                "Request parameters in AdditionalParameters are required for DOPA verification.");
        }

        var pid = request.AdditionalParameters.GetStringValue("PID", "pid", "citizen_id");
        if (!string.IsNullOrWhiteSpace(pid) && !pid.IsValidThaiNationalId())
        {
            return ProviderHelper.CreateFailureResult(
                "INVALID_PID_FORMAT",
                "Citizen ID (pid) must be a valid 13-digit Thai National ID.");
        }

        var faceImage = request.AdditionalParameters.GetStringValue("image", "faceImageBase64", "face_image");
        if (!string.IsNullOrWhiteSpace(faceImage) && !string.IsNullOrWhiteSpace(pid))
        {
            return await VerifyByFaceAsync(pid!, faceImage!, threshold: null, cancellationToken);
        }

        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            return ExecuteMockDopaVerification(request.AdditionalParameters);
        }

        var defaultUrl = !string.IsNullOrWhiteSpace(opts.VerificationUrl)
            ? opts.VerificationUrl
            : !string.IsNullOrWhiteSpace(opts.LaserUrl)
                ? opts.LaserUrl
                : opts.ChipUrl;

        return await SendPostJsonAsync(defaultUrl, opts, request.AdditionalParameters, cancellationToken);
    }

    private VerificationResult ExecuteMockFaceVerification(string pid, string cleanBase64, float effectiveThreshold)
    {
        _logger.LogEncryptedPiiIfPresent(
            _encryptionService,
            ProviderName,
            new Dictionary<string, object> { ["pid"] = pid },
            "pid");

        if (cleanBase64.IndexOf("TIMEOUT", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _logger.LogWarning("[MOCK-MODE] DOPA VerifyByFaceAsync simulating Network Timeout -> NETWORK_ERROR.");
            return ProviderHelper.CreateFailureResult(
                "NETWORK_ERROR",
                "Network error: Simulated DOPA Face API connection timeout (Mock).");
        }

        if (cleanBase64.IndexOf("ERROR503", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _logger.LogWarning("[MOCK-MODE] DOPA VerifyByFaceAsync simulating HTTP 503 -> DOPA_API_FAILED.");
            return ProviderHelper.CreateFailureResult(
                "DOPA_API_FAILED",
                "DOPA verification endpoint failed with HTTP 503.",
                503);
        }

        if (cleanBase64.IndexOf("MISMATCH", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _logger.LogWarning("[MOCK-MODE] DOPA VerifyByFaceAsync simulating Face Mismatch -> FACE_VERIFICATION_REJECTED.");
            var mismatchData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["result_code"] = "1",
                ["result_text"] = "ใบหน้าไม่ตรงกับฐานข้อมูลทะเบียนราษฎร",
                ["similarity"] = 0.42,
                ["confidence"] = 42.0,
                ["match"] = false,
                ["threshold"] = (double)effectiveThreshold,
                ["IsError"] = true
            };
            var failure = ProviderHelper.CreateFailureResult(
                "FACE_VERIFICATION_REJECTED",
                "ใบหน้าไม่ตรงกับฐานข้อมูลทะเบียนราษฎร");
            failure.Data = mismatchData;
            return failure;
        }

        _logger.LogWarning("[MOCK-MODE] DOPA VerifyByFaceAsync simulating Face Match Success.");
        return new VerificationResult
        {
            IsSuccess = true,
            Message = "DOPA face verification completed (Mock)",
            Data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["result_code"] = "0",
                ["result_text"] = "ใบหน้าตรงกับฐานข้อมูลทะเบียนราษฎร",
                ["similarity"] = 0.965,
                ["confidence"] = 96.5,
                ["match"] = true,
                ["threshold"] = (double)effectiveThreshold,
                ["IsError"] = false
            }
        };
    }

    private static VerificationResult EvaluateFaceResponse(IDictionary<string, object> responseData, float effectiveThreshold)
    {
        float? similarity = null;
        if (TryGetFloat(responseData, "similarity", out var simVal))
        {
            similarity = simVal > 1.0f ? simVal / 100.0f : simVal;
        }

        float? confidence = null;
        if (TryGetFloat(responseData, "confidence", out var confVal))
        {
            confidence = confVal;
            if (!similarity.HasValue)
            {
                similarity = confVal > 1.0f ? confVal / 100.0f : confVal;
            }
        }

        bool? explicitMatch = null;
        if (responseData.TryGetValue("match", out var matchObj) && matchObj is bool b)
        {
            explicitMatch = b;
        }

        var isMatch = explicitMatch ?? (!similarity.HasValue || similarity.Value >= effectiveThreshold);
        responseData["threshold"] = (double)effectiveThreshold;
        if (similarity.HasValue && !responseData.ContainsKey("similarity"))
        {
            responseData["similarity"] = (double)similarity.Value;
        }
        if (confidence.HasValue && !responseData.ContainsKey("confidence"))
        {
            responseData["confidence"] = (double)confidence.Value;
        }
        if (!responseData.ContainsKey("match"))
        {
            responseData["match"] = isMatch;
        }

        if (!isMatch)
        {
            var rejectMsg = responseData.GetStringValue("result_text", "ErrorMessage", "Desc", "message")
                ?? "DOPA face verification rejected (similarity below threshold).";
            var failure = ProviderHelper.CreateFailureResult("FACE_VERIFICATION_REJECTED", rejectMsg);
            failure.Data = new Dictionary<string, object>(responseData, StringComparer.OrdinalIgnoreCase);
            return failure;
        }

        return new VerificationResult
        {
            IsSuccess = true,
            Message = responseData.GetStringValue("result_text", "Desc", "message") ?? "DOPA face verification completed",
            Data = new Dictionary<string, object>(responseData, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static bool TryGetFloat(IDictionary<string, object> data, string key, out float value)
    {
        if (data.TryGetValue(key, out var raw) && raw is not null)
        {
            switch (raw)
            {
                case float f:
                    value = f;
                    return true;
                case double d:
                    value = (float)d;
                    return true;
                case int i:
                    value = i;
                    return true;
                case long l:
                    value = l;
                    return true;
                case string s when float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                    value = parsed;
                    return true;
            }
        }

        value = 0f;
        return false;
    }

    private static string StripDataUriPrefix(string base64Input)
    {
        var trimmed = base64Input.Trim();
        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = trimmed.IndexOf(',');
            if (commaIndex >= 0 && commaIndex + 1 < trimmed.Length)
            {
                return trimmed.Substring(commaIndex + 1).Trim();
            }
        }
        return trimmed;
    }

    private VerificationResult ExecuteMockDopaVerification(IDictionary<string, object> requestParameters)
    {
        _logger.LogEncryptedPiiIfPresent(_encryptionService, ProviderName, requestParameters, "PID", "pid", "citizen_id");

        var laser = requestParameters.GetStringValue("Laser", "laser", "laser_id") ?? string.Empty;
        var chipNo = requestParameters.GetStringValue("ChipNo", "chipNo", "chip_no") ?? string.Empty;

        // UC-DOPA-06: Timeout / Network Error simulation
        if (string.Equals(laser, "METIMEOUT000", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(chipNo, "TIMEOUT000000000", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[MOCK-MODE] DOPA simulating UC-DOPA-06 (Network Timeout -> NETWORK_ERROR).");
            return ProviderHelper.CreateFailureResult(
                "NETWORK_ERROR",
                "Network error: Simulated DOPA connection timeout (Mock).");
        }

        // UC-DOPA-05: DOPA API HTTP 503 Unavailable simulation
        if (string.Equals(laser, "ME5000000000", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(chipNo, "5000000000000000", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[MOCK-MODE] DOPA simulating UC-DOPA-05 (HTTP 503 Service Unavailable -> DOPA_API_FAILED).");
            return ProviderHelper.CreateFailureResult(
                "DOPA_API_FAILED",
                "DOPA verification endpoint failed with HTTP 503.",
                503);
        }

        // UC-DOPA-04: Card Verification Rejected (IsError: true)
        if (string.Equals(laser, "ME9999999999", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(chipNo, "0000000000000000", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[MOCK-MODE] DOPA simulating UC-DOPA-04 (Card Rejected -> CARD_VERIFICATION_REJECTED).");
            var rejectedData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Code"] = 1,
                ["Desc"] = "สถานะบัตรไม่ปกติ หรือข้อมูลไม่ตรงกับฐานข้อมูลทะเบียนราษฎร",
                ["IsError"] = true,
                ["ErrorMessage"] = "ข้อมูลเลขหลังบัตร (Laser ID) หรือหมายเลขชิปไม่ถูกต้อง"
            };
            var failure = ProviderHelper.CreateFailureResult(
                "CARD_VERIFICATION_REJECTED",
                "ข้อมูลเลขหลังบัตร (Laser ID) หรือหมายเลขชิปไม่ถูกต้อง");
            failure.Data = rejectedData;
            return failure;
        }

        // UC-DOPA-01: Normal Card Verification Success
        _logger.LogWarning("[MOCK-MODE] DOPA simulating UC-DOPA-01 (Normal Card Status -> Code: 0).");
        return new VerificationResult
        {
            IsSuccess = true,
            Message = "DOPA verification completed (Mock)",
            Data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Code"] = 0,
                ["Desc"] = "สถานะปกติ",
                ["IsError"] = false,
                ["ErrorMessage"] = ""
            }
        };
    }

    private async Task<string?> ResolveBearerTokenAsync(DopaOptions opts, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            return opts.ApiKey;
        }

        if (!string.IsNullOrWhiteSpace(opts.TokenUrl) &&
            !string.IsNullOrWhiteSpace(opts.ClientId) &&
            !string.IsNullOrWhiteSpace(opts.ClientSecret))
        {
            if (!string.IsNullOrWhiteSpace(_cachedAccessToken) && DateTimeOffset.UtcNow < _tokenExpiresAtUtc)
            {
                return _cachedAccessToken;
            }

            var tokenResult = await RequestAccessTokenAsync(cancellationToken);
            if (tokenResult.IsSuccess)
            {
                return tokenResult.Data.GetStringValue("access_token");
            }
        }

        return null;
    }

    private async Task<VerificationResult> SendPostJsonAsync(
        string? targetUrl,
        DopaOptions opts,
        IDictionary<string, object> requestParameters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_CONFIGURATION",
                "Missing required configuration: IdentityVerification:Dopa:VerificationUrl (or LaserUrl / ChipUrl / FaceUrl).");
        }

        try
        {
            var bearerToken = await ResolveBearerTokenAsync(opts, cancellationToken);
            if (string.IsNullOrWhiteSpace(bearerToken) &&
                !string.IsNullOrWhiteSpace(opts.TokenUrl) &&
                !string.IsNullOrWhiteSpace(opts.ClientId))
            {
                return ProviderHelper.CreateFailureResult(
                    "DOPA_TOKEN_FAILED",
                    "Failed to acquire OAuth 2.0 Bearer token from DOPA TokenUrl.");
            }

            var jsonContent = JsonSerializer.Serialize(requestParameters);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return ProviderHelper.CreateFailureResult(
                    "DOPA_API_FAILED",
                    $"DOPA verification endpoint failed with HTTP {(int)response.StatusCode}.",
                    (int)response.StatusCode);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseData = ProviderHelper.ParseJsonToDictionary(responseJson, "INVALID_DOPA_RESPONSE");

            _logger.LogEncryptedPiiIfPresent(_encryptionService, ProviderName, requestParameters, "PID", "pid", "citizen_id");

            if (responseData.TryGetValue("IsError", out var isErrorObj) && isErrorObj is true)
            {
                var errorMsg = responseData.GetStringValue("ErrorMessage", "Desc", "message")
                    ?? "DOPA card verification rejected.";
                var failure = ProviderHelper.CreateFailureResult("CARD_VERIFICATION_REJECTED", errorMsg);
                failure.Data = responseData;
                return failure;
            }

            return new VerificationResult
            {
                IsSuccess = true,
                Message = "DOPA verification completed",
                Data = responseData
            };
        }
        catch (VerificationException ex)
        {
            _logger.LogError(ex, "DOPA verification failed: {ErrorCode}", ex.ErrorCode);
            return ProviderHelper.CreateFailureResult(ex.ErrorCode, ex.Message, ex.HttpStatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling DOPA endpoint {TargetUrl}.", targetUrl);
            return ProviderHelper.CreateFailureResult("NETWORK_ERROR", $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected internal error during DOPA verification.");
            return ProviderHelper.CreateFailureResult("INTERNAL_ERROR", "An unexpected error occurred during DOPA verification.");
        }
    }

    private async Task<VerificationResult> ExecuteDgaGdxVerificationAsync(
        string pid,
        string firstName,
        string lastName,
        string birthDate,
        string laserId,
        DopaOptions opts,
        CancellationToken cancellationToken)
    {
        var consumerKey = opts.ConsumerKey;
        if (string.IsNullOrWhiteSpace(consumerKey))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_CONFIGURATION",
                "Missing required DGA GDX configuration: IdentityVerification:Dopa:ConsumerKey.");
        }

        var dgaToken = await ResolveDgaGdxTokenAsync(opts, cancellationToken);
        if (string.IsNullOrWhiteSpace(dgaToken))
        {
            return ProviderHelper.CreateFailureResult(
                "DGA_TOKEN_FAILED",
                "Failed to acquire DGA Government API validation token from /ws/auth/validate.");
        }

        var baseUrl = !string.IsNullOrWhiteSpace(opts.DgaGdxBaseUrl)
            ? opts.DgaGdxBaseUrl.TrimEnd('/')
            : "https://api.egov.go.th";

        var verifyEndpoint = !string.IsNullOrWhiteSpace(opts.LaserUrl)
            ? opts.LaserUrl
            : !string.IsNullOrWhiteSpace(opts.VerificationUrl)
                ? opts.VerificationUrl
                : $"{baseUrl}/ws/dopa/verification/personal";

        var uriBuilder = new UriBuilder(verifyEndpoint);
        var query = new StringBuilder();
        query.Append("CitizenID=").Append(Uri.EscapeDataString(pid));
        query.Append("&FirstName=").Append(Uri.EscapeDataString(firstName));
        query.Append("&LastName=").Append(Uri.EscapeDataString(lastName));
        query.Append("&BEBirthDate=").Append(Uri.EscapeDataString(birthDate));
        query.Append("&LaserCode=").Append(Uri.EscapeDataString(laserId));

        if (!string.IsNullOrWhiteSpace(uriBuilder.Query))
        {
            var existing = uriBuilder.Query.TrimStart('?');
            uriBuilder.Query = string.IsNullOrWhiteSpace(existing) ? query.ToString() : $"{existing}&{query}";
        }
        else
        {
            uriBuilder.Query = query.ToString();
        }

        var targetUrl = uriBuilder.Uri.ToString();

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, targetUrl);
            httpRequest.Headers.TryAddWithoutValidation("Consumer-Key", consumerKey);
            httpRequest.Headers.TryAddWithoutValidation("Token", dgaToken);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogEncryptedPiiIfPresent(
                _encryptionService,
                ProviderName,
                new Dictionary<string, object> { ["PID"] = pid },
                "PID");

            if (!response.IsSuccessStatusCode)
            {
                var parsedErr = TryParseJsonDictionary(responseContent);
                var rawErrMessage = parsedErr?.GetStringValue("message", "Message", "Desc", "ErrorMessage")
                    ?? responseContent;

                var mappedThaiMessage = MapDgaErrorMessage(rawErrMessage, (int)response.StatusCode);

                var fail = ProviderHelper.CreateFailureResult("DGA_GDX_API_FAILED", mappedThaiMessage, (int)response.StatusCode);
                if (parsedErr is not null)
                {
                    fail.Data = parsedErr;
                }
                return fail;
            }

            var responseData = ProviderHelper.ParseJsonToDictionary(responseContent, "INVALID_DGA_RESPONSE");

            // DGA GDX Response: {"Result": true} or {"Result": false}
            var isPass = false;
            if (responseData.TryGetValue("Result", out var resultObj) && resultObj is not null)
            {
                if (resultObj is bool b)
                {
                    isPass = b;
                }
                else if (resultObj is JsonElement je && (je.ValueKind == JsonValueKind.True || je.ValueKind == JsonValueKind.False))
                {
                    isPass = je.GetBoolean();
                }
                else if (bool.TryParse(resultObj.ToString(), out var parsedBool))
                {
                    isPass = parsedBool;
                }
            }

            if (!isPass)
            {
                var fail = ProviderHelper.CreateFailureResult(
                    "CARD_VERIFICATION_REJECTED",
                    "ข้อมูลไม่ตรงกับฐานข้อมูลทะเบียนราษฎร หรือบัตรสิ้นสภาพการใช้งาน");
                fail.Data = responseData;
                return fail;
            }

            return new VerificationResult
            {
                IsSuccess = true,
                Message = "DGA GDX Laser verification passed",
                Data = responseData
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling DGA GDX endpoint {TargetUrl}.", targetUrl);
            return ProviderHelper.CreateFailureResult("NETWORK_ERROR", $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling DGA GDX endpoint.");
            return ProviderHelper.CreateFailureResult("INTERNAL_ERROR", "An unexpected error occurred during DGA GDX verification.");
        }
    }

    private async Task<string?> ResolveDgaGdxTokenAsync(DopaOptions opts, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_cachedDgaToken) && DateTimeOffset.UtcNow < _dgaTokenExpiresAtUtc)
        {
            return _cachedDgaToken;
        }

        var consumerKey = opts.ConsumerKey;
        var consumerSecret = opts.ConsumerSecret;
        if (string.IsNullOrWhiteSpace(consumerKey) || string.IsNullOrWhiteSpace(consumerSecret))
        {
            _logger.LogWarning("DGA GDX ConsumerKey and ConsumerSecret must be configured to request token.");
            return null;
        }

        var baseUrl = !string.IsNullOrWhiteSpace(opts.DgaGdxBaseUrl)
            ? opts.DgaGdxBaseUrl.TrimEnd('/')
            : "https://api.egov.go.th";

        var agentId = !string.IsNullOrWhiteSpace(opts.AgentId) ? opts.AgentId : "NetkaIdentitySDK";
        var validateUrl = $"{baseUrl}/ws/auth/validate?ConsumerSecret={Uri.EscapeDataString(consumerSecret)}&AgentID={Uri.EscapeDataString(agentId)}";

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, validateUrl);
            req.Headers.TryAddWithoutValidation("Consumer-Key", consumerKey);

            using var resp = await _httpClient.SendAsync(req, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("DGA auth validation failed with HTTP {StatusCode}", (int)resp.StatusCode);
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            var dict = ProviderHelper.ParseJsonToDictionary(json, "INVALID_DGA_TOKEN_RESPONSE");
            var token = dict.GetStringValue("Result", "Token", "token", "access_token");

            if (!string.IsNullOrWhiteSpace(token))
            {
                _cachedDgaToken = token;
                // DGA tokens expire in 60 minutes. Cache for 55 minutes for safety margin.
                _dgaTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(55);
                return token;
            }

            _logger.LogError("DGA auth response did not contain Result token.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception requesting DGA auth validation token.");
            return null;
        }
    }

    /// <inheritdoc />
    [AuditLog("HealthCheckAsync", "DOPA", TrackDiff = false)]
    public async Task<VerificationResult> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var opts = ResolveOptions();
        if (opts.UseMock)
        {
            return new VerificationResult
            {
                IsSuccess = true,
                Message = "DOPA Health check passed (Mock)",
                Data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["status"] = "UP",
                    ["mode"] = opts.Mode.ToString(),
                    ["mock"] = true
                }
            };
        }

        if (opts.Mode == DopaGatewayMode.DgaGdx)
        {
            if (string.IsNullOrWhiteSpace(opts.ConsumerKey) || string.IsNullOrWhiteSpace(opts.ConsumerSecret))
            {
                return ProviderHelper.CreateFailureResult(
                    "MISSING_CONFIGURATION",
                    "DGA ConsumerKey and ConsumerSecret must be configured for DOPA health check.");
            }

            var token = await ResolveDgaGdxTokenAsync(opts, cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return new VerificationResult
                {
                    IsSuccess = true,
                    Message = "DGA GDX validation endpoint is healthy and reachable",
                    Data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["status"] = "UP",
                        ["mode"] = "DgaGdx",
                        ["endpoint"] = opts.DgaGdxBaseUrl ?? "https://api.egov.go.th"
                    }
                };
            }

            return ProviderHelper.CreateFailureResult(
                "DGA_HEALTH_CHECK_FAILED",
                "Unable to obtain validation token from DGA GDX service.");
        }

        // Enterprise Gateway mode
        var checkUrl = !string.IsNullOrWhiteSpace(opts.VerificationUrl)
            ? opts.VerificationUrl
            : !string.IsNullOrWhiteSpace(opts.LaserUrl)
                ? opts.LaserUrl
                : opts.TokenUrl;

        if (string.IsNullOrWhiteSpace(checkUrl))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_CONFIGURATION",
                "Missing DOPA verification URL for health check.");
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, checkUrl);
            var bearerToken = await ResolveBearerTokenAsync(opts, cancellationToken);
            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            using var resp = await _httpClient.SendAsync(req, cancellationToken);
            var isUp = (int)resp.StatusCode < 500;
            return new VerificationResult
            {
                IsSuccess = isUp,
                Message = isUp ? "DOPA Gateway is reachable" : $"DOPA Gateway returned HTTP {(int)resp.StatusCode}",
                Data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["status"] = isUp ? "UP" : "DOWN",
                    ["statusCode"] = (int)resp.StatusCode,
                    ["endpoint"] = checkUrl
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed against DOPA endpoint {CheckUrl}.", checkUrl);
            return ProviderHelper.CreateFailureResult("DOPA_HEALTH_CHECK_FAILED", $"DOPA health check failed: {ex.Message}");
        }
    }

    private static string MapDgaErrorMessage(string? rawMessage, int httpStatusCode)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return $"DGA GDX verification failed with HTTP {httpStatusCode}.";
        }

        // Spec codes from kb.dga.or.th/gdx/5api-lasercode:
        // [2] บัตรสิ้นสภาพการใช้งาน เนื่องจากกรณี การเปลี่ยนที่อยู่
        // [4] สถานะไม่ปกติ => ไม่พบเลข Laser จาก PID นี้ / ข้อมูลไม่ตรง
        if (rawMessage.Contains("[2]"))
        {
            return "บัตรสิ้นสภาพการใช้งาน เนื่องจากมีการเปลี่ยนที่อยู่ หรือทำบัตรใหม่ (Code [2])";
        }

        if (rawMessage.Contains("[4]"))
        {
            if (rawMessage.Contains("Laser") || rawMessage.Contains("laser"))
            {
                return "สถานะไม่ปกติ: ไม่พบเลข Laser จากเลขประจำตัวประชาชนนี้ (Code [4])";
            }
            return "สถานะไม่ปกติ: ข้อมูลหน้าบัตร (ชื่อ นามสกุล หรือวันเกิด) ไม่ตรงกับฐานข้อมูลทะเบียนราษฎร (Code [4])";
        }

        return rawMessage;
    }

    private static IDictionary<string, object>? TryParseJsonDictionary(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            return ProviderHelper.ParseJsonToDictionary(content, "ERROR");
        }
        catch
        {
            return null;
        }
    }
}
