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

    private readonly string _defaultVerificationUrl;
    private readonly string _laserVerificationUrl;
    private readonly string _chipVerificationUrl;
    private readonly string? _apiKey;
    private readonly HttpClient _httpClient;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<DopaProvider> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DopaProvider"/>.
    /// </summary>
    public DopaProvider(
        IConfiguration configuration,
        HttpClient httpClient,
        IEncryptionService encryptionService,
        ILogger<DopaProvider> logger)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (httpClient is null) throw new ArgumentNullException(nameof(httpClient));
        if (encryptionService is null) throw new ArgumentNullException(nameof(encryptionService));
        if (logger is null) throw new ArgumentNullException(nameof(logger));

        var section = configuration.GetSection("IdentityVerification:Dopa");

        _defaultVerificationUrl = section["VerificationUrl"]
            ?? section["LaserUrl"]
            ?? section["LaserVerificationUrl"]
            ?? throw new InvalidOperationException(
                "Missing required configuration: IdentityVerification:Dopa:VerificationUrl (or LaserUrl)");

        _laserVerificationUrl = section["LaserUrl"]
            ?? section["LaserVerificationUrl"]
            ?? _defaultVerificationUrl;

        _chipVerificationUrl = section["ChipUrl"]
            ?? section["ChipVerificationUrl"]
            ?? _defaultVerificationUrl;

        _apiKey = section["ApiKey"];

        _httpClient = httpClient;
        _encryptionService = encryptionService;
        _logger = logger;
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

        var payload = new Dictionary<string, object>
        {
            ["PID"] = pid,
            ["FirstName"] = firstName,
            ["LastName"] = lastName,
            ["BirthDay"] = birthDate,
            ["Laser"] = laserId
        };

        return await SendPostJsonAsync(_laserVerificationUrl, payload, cancellationToken);
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

        var payload = new Dictionary<string, object>
        {
            ["PID"] = pid,
            ["ChipNo"] = chipNo
        };

        if (!string.IsNullOrWhiteSpace(bp1No))
        {
            payload["bp1no"] = bp1No;
        }

        return await SendPostJsonAsync(_chipVerificationUrl, payload, cancellationToken);
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

        return await SendPostJsonAsync(_defaultVerificationUrl, request.AdditionalParameters, cancellationToken);
    }

    private async Task<VerificationResult> SendPostJsonAsync(
        string targetUrl,
        IDictionary<string, object> requestParameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var jsonContent = JsonSerializer.Serialize(requestParameters);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
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
