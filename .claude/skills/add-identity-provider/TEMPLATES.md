# add-identity-provider Templates

ไฟล์นี้เก็บ code templates สำหรับสร้าง Identity Provider ตัวใหม่อย่างรวดเร็ว

---

## Template 1: Interface (`Core/Interfaces/I<Name>Provider.cs`)

### Type A — OAuth 2.0 / OpenID Connect

```csharp
namespace Netka.Identity.Core.Interfaces;

/// <summary>
/// [PROVIDER_NAME] Identity Provider interface for OAuth 2.0 flow.
/// </summary>
public interface I[PROVIDER_NAME]Provider : IVerificationProvider
{
    /// <summary>
    /// Gets the authorization URL for [PROVIDER_NAME] login flow.
    /// </summary>
    /// <param name="redirectUri">Callback URI after user authentication.</param>
    /// <param name="state">CSRF protection state parameter.</param>
    /// <returns>Authorization URL for user redirect.</returns>
    /// <exception cref="InvalidOperationException">If OAuth flow is not supported.</exception>
    Task<string> GetAuthorizationUrlAsync(string redirectUri, string state);

    /// <summary>
    /// Exchanges authorization code for access token.
    /// </summary>
    /// <param name="code">Authorization code from callback.</param>
    /// <param name="redirectUri">Same URI used in GetAuthorizationUrlAsync.</param>
    /// <returns>VerificationResult with token data in Data dictionary.</returns>
    Task<VerificationResult> ExchangeCodeForTokenAsync(string code, string redirectUri);

    /// <summary>
    /// Retrieves user profile information using access token.
    /// </summary>
    /// <param name="accessToken">Valid access token from token response.</param>
    /// <returns>VerificationResult with user profile fields.</returns>
    Task<VerificationResult> GetUserProfileAsync(string accessToken);

    /// <summary>
    /// Refreshes an expired access token.
    /// </summary>
    /// <param name="refreshToken">Refresh token from previous token response.</param>
    /// <returns>VerificationResult with new token data.</returns>
    Task<VerificationResult> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Validates/inspects an access token.
    /// </summary>
    /// <param name="accessToken">Token to validate.</param>
    /// <returns>VerificationResult indicating token validity.</returns>
    Task<VerificationResult> InspectTokenAsync(string accessToken);

    /// <summary>
    /// Revokes an access or refresh token.
    /// </summary>
    /// <param name="token">Token to revoke.</param>
    /// <param name="tokenType">Type: 'access' or 'refresh'.</param>
    /// <returns>VerificationResult indicating revocation success.</returns>
    Task<VerificationResult> RevokeTokenAsync(string token, string tokenType);
}
```

### Type B — Direct REST / Hardware Verification

```csharp
namespace Netka.Identity.Core.Interfaces;

/// <summary>
/// [PROVIDER_NAME] Identity Provider interface for Server-to-Server verification.
/// </summary>
public interface I[PROVIDER_NAME]Provider : IVerificationProvider
{
    /// <summary>
    /// Verifies [PROVIDER_NAME] data by direct API call.
    /// </summary>
    /// <param name="data">[PROVIDER_NAME]-specific parameters.</param>
    /// <returns>VerificationResult with verification outcome.</returns>
    Task<VerificationResult> Verify[PROVIDER_NAME]DataAsync(Dictionary<string, string> data);

    /// <summary>
    /// [OTHER_METHOD] description.
    /// </summary>
    /// <param name="param">Parameter description.</param>
    /// <returns>VerificationResult with [OUTCOME].</returns>
    Task<VerificationResult> [METHOD_NAME]Async(string param);
}
```

---

## Template 2: Provider Class (`Infrastructure/Providers/<Name>Provider.cs`)

### Type A Example Skeleton

```csharp
namespace Netka.Identity.Infrastructure.Providers;

/// <summary>
/// [PROVIDER_NAME] Identity Provider implementation for OAuth 2.0 flow.
/// </summary>
[AuditLog]
public class [PROVIDER_NAME]Provider : I[PROVIDER_NAME]Provider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<[PROVIDER_NAME]Provider> _logger;
    private readonly IEncryptionService _encryptionService;

    public string ProviderName => "[PROVIDER_NAME]";

    public [PROVIDER_NAME]Provider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<[PROVIDER_NAME]Provider> logger,
        IEncryptionService encryptionService)
    {
        if (httpClient is null)
            throw new ArgumentNullException(nameof(httpClient));
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));
        if (encryptionService is null)
            throw new ArgumentNullException(nameof(encryptionService));

        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Gets authorization URL for [PROVIDER_NAME] OAuth flow.
    /// </summary>
    public async Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new ArgumentException("Redirect URI is required.", nameof(redirectUri));
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required.", nameof(state));

        var clientId = ProviderHelper.GetRequiredValue(
            _configuration,
            $"IdentityVerification:{ProviderName}:ClientId");

        var authorizationEndpoint = ProviderHelper.GetRequiredValue(
            _configuration,
            $"IdentityVerification:{ProviderName}:AuthorizationEndpoint");

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "client_id", clientId },
            { "redirect_uri", redirectUri },
            { "response_type", "code" },
            { "scope", "[PROVIDER_SCOPES]" },
            { "state", state }
        };

        var query = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        return $"{authorizationEndpoint}?{query}";
    }

    /// <summary>
    /// Exchanges authorization code for access token.
    /// </summary>
    public async Task<VerificationResult> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(code))
            return ProviderHelper.CreateFailureResult(
                "invalid_code",
                "Authorization code is required.",
                400);

        if (string.IsNullOrWhiteSpace(redirectUri))
            return ProviderHelper.CreateFailureResult(
                "invalid_redirect_uri",
                "Redirect URI is required.",
                400);

        try
        {
            var clientId = ProviderHelper.GetRequiredValue(
                _configuration,
                $"IdentityVerification:{ProviderName}:ClientId");

            var clientSecret = ProviderHelper.GetRequiredValue(
                _configuration,
                $"IdentityVerification:{ProviderName}:ClientSecret");

            var tokenEndpoint = ProviderHelper.GetRequiredValue(
                _configuration,
                $"IdentityVerification:{ProviderName}:TokenEndpoint");

            var formFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };

            // Cross-version compatible FormUrlEncodedContent
            var content = new FormUrlEncodedContent(
                formFields.Select(kvp => new KeyValuePair<string?, string?>(kvp.Key, kvp.Value)));

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "[{ProviderName}] Token exchange failed: {StatusCode} {Content}",
                    ProviderName,
                    response.StatusCode,
                    errorContent);

                return ProviderHelper.CreateFailureResult(
                    "token_exchange_failed",
                    $"Failed to exchange code: {errorContent}",
                    (int)response.StatusCode);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var data = ProviderHelper.ParseJsonToDictionary(jsonResponse, "token_exchange_success");

            // Log sensitive data with encryption
            _logger.LogEncryptedPiiIfPresent(
                _encryptionService,
                ProviderName,
                data,
                "access_token", "refresh_token", "id_token");

            return new VerificationResult
            {
                IsSuccess = true,
                ErrorCode = null,
                ErrorMessage = null,
                StatusCode = (int)response.StatusCode,
                Data = data
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "[{ProviderName}] HTTP error during token exchange",
                ProviderName);
            return ProviderHelper.CreateFailureResult(
                "http_error",
                ex.Message,
                500);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{ProviderName}] Unexpected error during token exchange",
                ProviderName);
            return ProviderHelper.CreateFailureResult(
                "token_exchange_error",
                ex.Message,
                500);
        }
    }

    /// <summary>
    /// Retrieves user profile using access token.
    /// </summary>
    public async Task<VerificationResult> GetUserProfileAsync(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return ProviderHelper.CreateFailureResult(
                "invalid_token",
                "Access token is required.",
                400);

        try
        {
            var userInfoEndpoint = ProviderHelper.GetRequiredValue(
                _configuration,
                $"IdentityVerification:{ProviderName}:UserInfoEndpoint");

            var request = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return ProviderHelper.CreateFailureResult(
                    "userinfo_failed",
                    $"Failed to get user profile: {errorContent}",
                    (int)response.StatusCode);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var data = ProviderHelper.ParseJsonToDictionary(jsonResponse, "userinfo_success");

            _logger.LogEncryptedPiiIfPresent(
                _encryptionService,
                ProviderName,
                data,
                "sub", "email", "phone_number", "name");

            return new VerificationResult
            {
                IsSuccess = true,
                ErrorCode = null,
                ErrorMessage = null,
                StatusCode = (int)response.StatusCode,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ProviderName}] Error retrieving user profile", ProviderName);
            return ProviderHelper.CreateFailureResult(
                "userinfo_error",
                ex.Message,
                500);
        }
    }

    /// <summary>
    /// Generic verification method (required by IVerificationProvider).
    /// </summary>
    public async Task<VerificationResult> Verify(Dictionary<string, string> parameters)
    {
        // Implement routing or wrapper logic if needed
        throw new NotImplementedException(
            $"[{ProviderName}] Use specific method instead (ExchangeCodeForTokenAsync, GetUserProfileAsync, etc.)");
    }

    // Implement other methods: RefreshAccessTokenAsync, InspectTokenAsync, RevokeTokenAsync
}
```

### Type B Example Skeleton

```csharp
namespace Netka.Identity.Infrastructure.Providers;

/// <summary>
/// [PROVIDER_NAME] Identity Provider implementation for direct verification.
/// </summary>
[AuditLog]
public class [PROVIDER_NAME]Provider : I[PROVIDER_NAME]Provider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<[PROVIDER_NAME]Provider> _logger;
    private readonly IEncryptionService _encryptionService;

    public string ProviderName => "[PROVIDER_NAME]";

    public [PROVIDER_NAME]Provider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<[PROVIDER_NAME]Provider> logger,
        IEncryptionService encryptionService)
    {
        if (httpClient is null)
            throw new ArgumentNullException(nameof(httpClient));
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));
        if (encryptionService is null)
            throw new ArgumentNullException(nameof(encryptionService));

        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Verifies [PROVIDER_NAME] data by direct server-to-server API call.
    /// </summary>
    public async Task<VerificationResult> Verify[PROVIDER_NAME]DataAsync(Dictionary<string, string> data)
    {
        if (data is null || data.Count == 0)
            return ProviderHelper.CreateFailureResult(
                "invalid_data",
                "Verification data is required.",
                400);

        try
        {
            var verifyEndpoint = ProviderHelper.GetRequiredValue(
                _configuration,
                $"IdentityVerification:{ProviderName}:VerifyEndpoint");

            // Build request payload
            var payload = new { /* map from data */ };
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, verifyEndpoint)
            {
                Content = jsonContent
            };

            // Add authentication headers if needed
            var apiKey = ProviderHelper.GetRequiredValue(
                _configuration,
                $"IdentityVerification:{ProviderName}:ApiKey");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return ProviderHelper.CreateFailureResult(
                    "verification_failed",
                    $"[{ProviderName}] Verification failed: {errorContent}",
                    (int)response.StatusCode);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = ProviderHelper.ParseJsonToDictionary(jsonResponse, "verification_success");

            _logger.LogEncryptedPiiIfPresent(_encryptionService, ProviderName, result);

            return new VerificationResult
            {
                IsSuccess = true,
                ErrorCode = null,
                ErrorMessage = null,
                StatusCode = (int)response.StatusCode,
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ProviderName}] Error during verification", ProviderName);
            return ProviderHelper.CreateFailureResult("verification_error", ex.Message, 500);
        }
    }

    /// <summary>
    /// Generic verification method (required by IVerificationProvider).
    /// </summary>
    public async Task<VerificationResult> Verify(Dictionary<string, string> parameters)
    {
        return await Verify[PROVIDER_NAME]DataAsync(parameters);
    }
}
```

---

## Template 3: Unit Tests (`Tests/<Name>ProviderTests.cs`)

```csharp
namespace Netka.Identity.SDK.Tests;

/// <summary>
/// Unit tests for [PROVIDER_NAME]Provider.
/// </summary>
public class [PROVIDER_NAME]ProviderTests
{
    private readonly MockHttpMessageHandler _mockHttpHandler = new();
    private readonly [PROVIDER_NAME]Provider _provider;
    private readonly IConfiguration _configuration;

    public [PROVIDER_NAME]ProviderTests()
    {
        // Setup mock configuration
        var configDict = new Dictionary<string, string>
        {
            { "IdentityVerification:[PROVIDER_NAME]:ClientId", "test-client-id" },
            { "IdentityVerification:[PROVIDER_NAME]:ClientSecret", "test-secret" },
            { "IdentityVerification:[PROVIDER_NAME]:AuthorizationEndpoint", "https://test.provider.com/authorize" },
            { "IdentityVerification:[PROVIDER_NAME]:TokenEndpoint", "https://test.provider.com/token" },
            { "IdentityVerification:[PROVIDER_NAME]:UserInfoEndpoint", "https://test.provider.com/userinfo" },
            { "IdentityVerification:[PROVIDER_NAME]:AllowUntrustedCertificates", "false" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var httpClient = new HttpClient(_mockHttpHandler)
        {
            BaseAddress = new Uri("https://test.provider.com")
        };

        var loggerMock = new Mock<ILogger<[PROVIDER_NAME]Provider>>();
        var encryptionMock = new Mock<IEncryptionService>();

        _provider = new [PROVIDER_NAME]Provider(
            httpClient,
            _configuration,
            loggerMock.Object,
            encryptionMock.Object);
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithValidParams_ShouldReturnValidUrl()
    {
        // Arrange
        var redirectUri = "https://myapp.com/callback";
        var state = "random-state-123";

        // Act
        var url = await _provider.GetAuthorizationUrlAsync(redirectUri, state);

        // Assert
        Assert.Contains("client_id=test-client-id", url);
        Assert.Contains($"redirect_uri={Uri.EscapeDataString(redirectUri)}", url);
        Assert.Contains($"state={state}", url);
        Assert.StartsWith("https://test.provider.com/authorize", url);
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithNullRedirectUri_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _provider.GetAuthorizationUrlAsync(null, "state"));
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithValidCode_ShouldReturnToken()
    {
        // Arrange
        var code = "valid-code-123";
        var redirectUri = "https://myapp.com/callback";
        var tokenResponse = new
        {
            access_token = "token123",
            token_type = "Bearer",
            expires_in = 3600,
            refresh_token = "refresh123"
        };

        _mockHttpHandler.SetupResponse(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        // Act
        var result = await _provider.ExchangeCodeForTokenAsync(code, redirectUri);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorCode);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.TryGetValue("access_token", out var token));
        Assert.Equal("token123", token);
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithInvalidCode_ShouldReturnFailure()
    {
        // Arrange
        var code = "invalid-code";
        var redirectUri = "https://myapp.com/callback";
        _mockHttpHandler.SetupResponse(
            HttpStatusCode.BadRequest,
            "{ \"error\": \"invalid_grant\" }");

        // Act
        var result = await _provider.ExchangeCodeForTokenAsync(code, redirectUri);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("token_exchange_failed", result.ErrorCode);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithValidToken_ShouldReturnProfile()
    {
        // Arrange
        var accessToken = "valid-token";
        var profileResponse = new
        {
            sub = "user123",
            email = "user@example.com",
            name = "Test User"
        };

        _mockHttpHandler.SetupResponse(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(profileResponse));

        // Act
        var result = await _provider.GetUserProfileAsync(accessToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.TryGetValue("sub", out var sub));
        Assert.Equal("user123", sub);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var accessToken = "invalid-token";
        _mockHttpHandler.SetupResponse(HttpStatusCode.Unauthorized, "{ \"error\": \"invalid_token\" }");

        // Act
        var result = await _provider.GetUserProfileAsync(accessToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("userinfo_failed", result.ErrorCode);
        Assert.Equal(401, result.StatusCode);
    }
}
```

---

## Template 4: Configuration (`appsettings.json` / `appsettings.Development.json`)

```json
{
  "IdentityVerification": {
    "[PROVIDER_NAME]": {
      "ClientId": "your-client-id-here",
      "ClientSecret": "your-client-secret-here",
      "AuthorizationEndpoint": "https://provider.com/oauth/authorize",
      "TokenEndpoint": "https://provider.com/oauth/token",
      "UserInfoEndpoint": "https://provider.com/oauth/userinfo",
      "RefreshTokenEndpoint": "https://provider.com/oauth/refresh",
      "InspectTokenEndpoint": "https://provider.com/oauth/inspect",
      "RevokeTokenEndpoint": "https://provider.com/oauth/revoke",
      "Scopes": "openid profile email",
      "AllowUntrustedCertificates": false,
      "TimeoutSeconds": 30
    }
  }
}
```

---

## Template 5: Dependency Injection Configuration

```csharp
// Add to Application/DependencyInjection.cs

private static void Configure[PROVIDER_NAME]Provider(
    IServiceCollection services,
    IConfiguration configuration)
{
    ConfigureProviderHttpClient<[PROVIDER_NAME]Provider>(
        services,
        configuration,
        $"IdentityVerification:[PROVIDER_NAME]:AllowUntrustedCertificates");

    services.AddTransient<I[PROVIDER_NAME]Provider>(
        sp => sp.GetRequiredService<[PROVIDER_NAME]Provider>());

    services.AddTransient<IVerificationProvider>(
        sp => sp.GetRequiredService<[PROVIDER_NAME]Provider>());
}

// In AddIdentityVerification method:
public static IServiceCollection AddIdentityVerification(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing provider configurations ...
    
    Configure[PROVIDER_NAME]Provider(services, configuration);
    
    return services;
}
```

---

## Placeholders to Replace

| Placeholder | Example | Description |
|-------------|---------|-------------|
| `[PROVIDER_NAME]` | `Google`, `LineLogin`, `MicrosoftEntra` | Provider class name (PascalCase) |
| `[provider_name]` | `google`, `line_login`, `microsoft_entra` | Config section name (snake_case) |
| `[PROVIDER_SCOPES]` | `openid profile email` | OAuth scopes required by provider |
| `[PROVIDER_SPECIFIC_PARAMS]` | `nonce`, `prompt`, `access_type` | Extra params specific to provider |
| `[METHOD_NAME]` | `VerifyNationalId`, `ValidateDevice` | Custom method for Type B providers |
| `[OUTCOME]` | `verification outcome`, `device status` | Expected result description |

---

**Template Version:** 1.0.0  
**Last Updated:** 2026-10-12
