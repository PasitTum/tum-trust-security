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
            return _optionsProvider.GetOptions()?.Dopa ?? new DopaOptions();
        }

        if (_configuration is not null)
        {
            return DopaOptions.FromSection(_configuration.GetSection("IdentityVerification:Dopa"));
        }

        return _staticOptions ?? new DopaOptions();
    }

    /// <inheritdoc />
    public string GetAuthorizationUrl(string redirectUri, string state)
    {
        throw new NotSupportedException(
            "DOPA verification is a direct API flow and does not use an OAuth authorization URL.");
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

        var opts = ResolveOptions();
        var laserUrl = !string.IsNullOrWhiteSpace(opts.LaserUrl)
            ? opts.LaserUrl
            : opts.VerificationUrl;

        var payload = new Dictionary<string, object>
        {
            ["PID"] = pid,
            ["FirstName"] = firstName,
            ["LastName"] = lastName,
            ["BirthDay"] = birthDate,
            ["Laser"] = laserId
        };

        return await SendPostJsonAsync(laserUrl, opts.ApiKey, payload, cancellationToken);
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

        var opts = ResolveOptions();
        var chipUrl = !string.IsNullOrWhiteSpace(opts.ChipUrl)
            ? opts.ChipUrl
            : !string.IsNullOrWhiteSpace(opts.VerificationUrl)
                ? opts.VerificationUrl
                : opts.LaserUrl;

        var payload = new Dictionary<string, object>
        {
            ["PID"] = pid,
            ["ChipNo"] = chipNo
        };

        if (!string.IsNullOrWhiteSpace(bp1No))
        {
            payload["bp1no"] = bp1No!;
        }

        return await SendPostJsonAsync(chipUrl, opts.ApiKey, payload, cancellationToken);
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

        var opts = ResolveOptions();
        var defaultUrl = !string.IsNullOrWhiteSpace(opts.VerificationUrl)
            ? opts.VerificationUrl
            : !string.IsNullOrWhiteSpace(opts.LaserUrl)
                ? opts.LaserUrl
                : opts.ChipUrl;

        return await SendPostJsonAsync(defaultUrl, opts.ApiKey, request.AdditionalParameters, cancellationToken);
    }

    private async Task<VerificationResult> SendPostJsonAsync(
        string? targetUrl,
        string? apiKey,
        IDictionary<string, object> requestParameters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return ProviderHelper.CreateFailureResult(
                "MISSING_CONFIGURATION",
                "Missing required configuration: IdentityVerification:Dopa:VerificationUrl (or LaserUrl / ChipUrl).");
        }

        try
        {
            var jsonContent = JsonSerializer.Serialize(requestParameters);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
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
}
