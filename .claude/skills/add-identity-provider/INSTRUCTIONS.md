# add-identity-provider Skill — Detailed Instructions

เมื่อผู้ใช้เรียกใช้ `/add-identity-provider` ให้ปฏิบัติตามขั้นตอนต่อไปนี้อย่างเคร่งครัด

---

## ขั้นตอนที่ 1: การรับและค้นหาสเปก API (Flexible Spec Discovery)

### 1.1 ถามผู้ใช้ว่าต้องการระบุสเปก API ในวิธีใด:

```
ระบุสเปก API ของ Provider ใหม่ (เลือกหนึ่ง):
[A] URL API Documentation
[B] ข้อความ / cURL / Postman / JSON
[C] ชื่อ Provider (เช่น Google, LINE, Microsoft)
```

### 1.2 ดำเนินการตามที่ผู้ใช้เลือก:

**เคส A — ผู้ใช้ส่ง URL:**
- ใช้เครื่องมือ `WebFetch` ดึงเอกสาร API จากลิงก์นั้น
- สรุปรายการ Endpoints, HTTP Methods, Headers (`Basic`, `Bearer`, `ApiKey`), Request/Response JSON
- เช่น: `GET /oauth/authorization` → `POST /oauth/token` → `GET /user/profile`

**เคส B — ผู้ใช้ส่งข้อความ/ไฟล์:**
- อ่านข้อมูล Endpoints, Parameters, Response จากข้อความหรือไฟล์โดยตรง
- สรุปโครงสร้าง API ตามที่ระบุ

**เคส C — ผู้ใช้บอกแค่ชื่อ Provider:**
- ใช้ `WebSearch` ค้นหา "Provider Name OAuth 2.0 API Documentation" อย่างเป็นทางการ
- เลือกลิงก์ที่เป็น Developer Console / Docs ของ Provider นั้นเอง
- ใช้ `WebFetch` ดึงเอกสารและสรุป Endpoints

### 1.3 จำแนกประเภท Provider:

หลังอ่านสเปก ให้วิเคราะห์และตัดสิน:

- **Type A (OAuth 2.0 / OpenID Connect):**
  - มี Endpoints เช่น `/authorize`, `/token`, `/userinfo`, `/refresh`, `/revoke`
  - มี Authorization URL ที่ redirect ไปหน้า Login
  - ส่วนใหญ่เป็น Type A

- **Type B (Direct REST / Hardware Verification):**
  - ไม่มี `/authorize` หรือ redirect login
  - ยิง Request โดยตรง Server-to-Server (เช่น Card Reader, Hardware Verification)
  - แน่นอนเพราะ spec ไม่มี authorization flow

---

## ขั้นตอนที่ 2: สร้างไฟล์ Interface + Provider Class

### 2.1 สร้าง Interface (`Core/Interfaces/I<Name>Provider.cs`)

```csharp
namespace Netka.Identity.Core.Interfaces;

/// <summary>
/// <Name> Identity Provider interface for OAuth 2.0 / Direct REST verification.
/// </summary>
public interface I<Name>Provider : IVerificationProvider
{
    /// <summary>
    /// Gets the authorization URL for <Name> login flow (Type A only).
    /// </summary>
    /// <param name="redirectUri">Callback URI after user authentication.</param>
    /// <param name="state">CSRF protection state parameter.</param>
    /// <returns>Authorization URL or exception if not supported (Type B).</returns>
    Task<string> GetAuthorizationUrlAsync(string redirectUri, string state);

    /// <summary>
    /// Exchanges authorization code for access token.
    /// </summary>
    /// <param name="code">Authorization code from callback.</param>
    /// <param name="redirectUri">Same URI used in GetAuthorizationUrlAsync.</param>
    /// <returns>VerificationResult with token data.</returns>
    Task<VerificationResult> ExchangeCodeForTokenAsync(string code, string redirectUri);

    /// <summary>
    /// Retrieves user profile information using access token.
    /// </summary>
    /// <param name="accessToken">Valid access token.</param>
    /// <returns>VerificationResult with user profile data.</returns>
    Task<VerificationResult> GetUserProfileAsync(string accessToken);

    /// <summary>
    /// Refreshes an expired access token using refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token from token response.</param>
    /// <returns>VerificationResult with new token data.</returns>
    Task<VerificationResult> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Inspects/validates an access token.
    /// </summary>
    /// <param name="accessToken">Token to validate.</param>
    /// <returns>VerificationResult indicating token validity.</returns>
    Task<VerificationResult> InspectTokenAsync(string accessToken);

    /// <summary>
    /// Revokes an access token or refresh token.
    /// </summary>
    /// <param name="token">Token to revoke.</param>
    /// <param name="tokenType">Type of token (access, refresh).</param>
    /// <returns>VerificationResult indicating revocation status.</returns>
    Task<VerificationResult> RevokeTokenAsync(string token, string tokenType);
}
```

**สำหรับ Type B (Direct REST):**
- ลบ `GetAuthorizationUrlAsync` / `RefreshAccessTokenAsync` / `RevokeTokenAsync` ออก
- เก็บเฉพาะเมธอดยิง Request โดยตรง

### 2.2 สร้าง Provider Class (`Infrastructure/Providers/<Name>Provider.cs`)

```csharp
namespace Netka.Identity.Infrastructure.Providers;

/// <summary>
/// <Name> Identity Provider implementation.
/// </summary>
[AuditLog]
public class <Name>Provider : I<Name>Provider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<> _logger;
    private readonly IEncryptionService _encryptionService;

    public string ProviderName => "<Name>";

    public <Name>Provider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<<Name>Provider> logger,
        IEncryptionService encryptionService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Gets authorization URL for <Name> OAuth flow.
    /// </summary>
    public async Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        var clientId = ProviderHelper.GetRequiredValue(_configuration, "IdentityVerification:<Name>:ClientId");
        var authorizationEndpoint = ProviderHelper.GetRequiredValue(_configuration, "IdentityVerification:<Name>:AuthorizationEndpoint");

        var parameters = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "redirect_uri", redirectUri },
            { "response_type", "code" },
            { "scope", "openid profile email" },
            { "state", state }
        };

        var query = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        return $"{authorizationEndpoint}?{query}";
    }

    /// <summary>
    /// Exchanges authorization code for access token.
    /// </summary>
    public async Task<VerificationResult> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(code))
            return ProviderHelper.CreateFailureResult("invalid_code", "Authorization code is required.", 400);

        try
        {
            var clientId = ProviderHelper.GetRequiredValue(_configuration, "IdentityVerification:<Name>:ClientId");
            var clientSecret = ProviderHelper.GetRequiredValue(_configuration, "IdentityVerification:<Name>:ClientSecret");
            var tokenEndpoint = ProviderHelper.GetRequiredValue(_configuration, "IdentityVerification:<Name>:TokenEndpoint");

            var formFields = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };

            // Cross-version compatible FormUrlEncodedContent creation
            var content = new FormUrlEncodedContent(
                formFields.Select(kvp => new KeyValuePair<string?, string?>(kvp.Key, kvp.Value)));

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint) { Content = content };
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return ProviderHelper.CreateFailureResult(
                    "token_exchange_failed",
                    $"Failed to exchange code: {errorContent}",
                    (int)response.StatusCode);
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var data = ProviderHelper.ParseJsonToDictionary(jsonResponse, "token_exchange_success");

            // Log encrypted PII if present
            _logger.LogEncryptedPiiIfPresent(_encryptionService, ProviderName, data, "access_token", "refresh_token");

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
            _logger.LogError(ex, "[{ProviderName}] Error exchanging code for token", ProviderName);
            return ProviderHelper.CreateFailureResult("token_exchange_error", ex.Message, 500);
        }
    }

    // ... implement other methods (GetUserProfileAsync, RefreshAccessTokenAsync, etc.)

    public async Task<VerificationResult> Verify(Dictionary<string, string> parameters)
    {
        // Implement wrapper for generic verify interface
        throw new NotImplementedException();
    }
}
```

### 2.3 ปฏิบัติตามกฎ Shared Helpers:
- ✅ ใช้ `ProviderHelper.GetRequiredValue()` สำหรับ config
- ✅ ใช้ `ProviderHelper.ParseJsonToDictionary()` สำหรับ response
- ✅ ใช้ `ProviderHelper.CreateFailureResult()` สำหรับ error
- ✅ ใช้ `_logger.LogEncryptedPiiIfPresent()` สำหรับ audit log
- ✅ ทุก method ต้องมี XML Docs
- ✅ ติด `[AuditLog]` ที่ระดับคลาส

---

## ขั้นตอนที่ 3: Cross-Version Compatibility Check (net5.0, net8.0, net10.0)

### 3.1 ตรวจสอบ Code Pattern:

```csharp
// ❌ ห้าม (.NET 6+ only)
ArgumentNullException.ThrowIfNull(x);
ArgumentException.ThrowIfNullOrWhiteSpace(s);
char.IsAsciiLetterOrDigit(c);

// ✅ ให้ใช้ (.NET 5.0 compatible)
if (x is null) throw new ArgumentNullException(nameof(x));
if (string.IsNullOrWhiteSpace(s)) throw new ArgumentException("...", nameof(s));
if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9') { }
```

### 3.2 FormUrlEncodedContent Builder:

```csharp
// ❌ ห้าม (CS8620 warning ใน .NET 5.0)
new FormUrlEncodedContent(formFields)

// ✅ ให้ใช้
new FormUrlEncodedContent(
    formFields.Select(kvp => new KeyValuePair<string?, string?>(kvp.Key, kvp.Value)))
```

### 3.3 String Comparison:

```csharp
// ❌ ห้าม (ขึ้นอยู่กับ Culture)
if (key == "VALUE") { }

// ✅ ให้ใช้
if (key.Equals("VALUE", StringComparison.OrdinalIgnoreCase)) { }

// Dictionary creation
var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
```

### 3.4 ห้ามแก้ NuGet Versions:
- ✅ คงเวอร์ชัน: `5.0.0` (net5.0), `8.0.0` (net8.0), `10.0.0` (net10.0)
- ❌ ห้ามเปลี่ยนเป็น `5.1.2` หรือ `8.0.5` เป็นต้น

---

## ขั้นตอนที่ 4: ลงทะเบียน Dependency Injection

### 4.1 อัปเดต `Application/DependencyInjection.cs`:

ค้นหาเมธอด `AddIdentityVerification` แล้วเพิ่ม:

```csharp
/// <summary>
/// Configures <Name> Provider in the DI container.
/// </summary>
private static void Configure<Name>Provider(IServiceCollection services, IConfiguration configuration)
{
    ConfigureProviderHttpClient<<Name>Provider>(
        services,
        configuration,
        "IdentityVerification:<Name>:AllowUntrustedCertificates");

    services.AddTransient<I<Name>Provider>(sp => sp.GetRequiredService<<Name>Provider>());
    services.AddTransient<IVerificationProvider>(sp => sp.GetRequiredService<<Name>Provider>());
}

// In AddIdentityVerification method:
public static IServiceCollection AddIdentityVerification(this IServiceCollection services, IConfiguration configuration)
{
    // ... existing providers ...
    Configure<Name>Provider(services, configuration);
    return services;
}
```

### 4.2 เพิ่มคอนฟิกไฟล์:

สร้าง/อัปเดต `appsettings.json` หรือ `appsettings.Development.json`:

```json
{
  "IdentityVerification": {
    "<Name>": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "AuthorizationEndpoint": "https://provider.com/oauth/authorize",
      "TokenEndpoint": "https://provider.com/oauth/token",
      "UserInfoEndpoint": "https://provider.com/oauth/userinfo",
      "AllowUntrustedCertificates": false
    }
  }
}
```

---

## ขั้นตอนที่ 5: สร้าง Unit Tests

### 5.1 สร้าง `Tests/<Name>ProviderTests.cs`:

```csharp
namespace Netka.Identity.SDK.Tests;

public class <Name>ProviderTests
{
    private readonly MockHttpMessageHandler _mockHttpHandler = new();
    private readonly <Name>Provider _provider;
    private readonly IConfiguration _configuration;

    public <Name>ProviderTests()
    {
        // Setup configuration
        var configDict = new Dictionary<string, string>
        {
            { "IdentityVerification:<Name>:ClientId", "test-client-id" },
            { "IdentityVerification:<Name>:ClientSecret", "test-secret" },
            { "IdentityVerification:<Name>:AuthorizationEndpoint", "https://test.com/authorize" },
            { "IdentityVerification:<Name>:TokenEndpoint", "https://test.com/token" },
            { "IdentityVerification:<Name>:UserInfoEndpoint", "https://test.com/userinfo" },
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var httpClient = new HttpClient(_mockHttpHandler) { BaseAddress = new Uri("https://test.com") };
        var logger = new Mock<ILogger<<Name>Provider>>().Object;
        var encryptionService = new Mock<IEncryptionService>().Object;

        _provider = new <Name>Provider(httpClient, _configuration, logger, encryptionService);
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_ShouldReturnValidUrl()
    {
        // Arrange
        var redirectUri = "https://myapp.com/callback";
        var state = "random-state";

        // Act
        var url = await _provider.GetAuthorizationUrlAsync(redirectUri, state);

        // Assert
        Assert.Contains("client_id=test-client-id", url);
        Assert.Contains($"redirect_uri={Uri.EscapeDataString(redirectUri)}", url);
        Assert.Contains($"state={state}", url);
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithValidCode_ShouldReturnToken()
    {
        // Arrange
        var code = "valid-code";
        var redirectUri = "https://myapp.com/callback";
        var tokenResponse = new { access_token = "token123", token_type = "Bearer", expires_in = 3600 };

        _mockHttpHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(tokenResponse));

        // Act
        var result = await _provider.ExchangeCodeForTokenAsync(code, redirectUri);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("token123", result.Data.GetStringValue("access_token"));
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_WithInvalidCode_ShouldReturnFailure()
    {
        // Arrange
        var code = "invalid-code";
        var redirectUri = "https://myapp.com/callback";
        _mockHttpHandler.SetupResponse(HttpStatusCode.BadRequest, "Invalid code");

        // Act
        var result = await _provider.ExchangeCodeForTokenAsync(code, redirectUri);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("token_exchange_failed", result.ErrorCode);
    }

    // ... additional test methods for GetUserProfileAsync, RefreshAccessTokenAsync, etc.
}
```

### 5.2 อัปเดต `Tests/DependencyInjectionTests.cs`:

```csharp
[Fact]
public void AddIdentityVerification_Should_RegisterAllProviders()
{
    var services = new ServiceCollection();
    var config = new ConfigurationBuilder().AddInMemoryCollection(GetValidConfiguration()).Build();

    services.AddIdentityVerification(config);

    var provider = services.BuildServiceProvider();
    
    // Verify <Name>Provider is registered
    var <name>Provider = provider.GetService<I<Name>Provider>();
    Assert.NotNull(<name>Provider);
    
    // Verify also registered as IVerificationProvider
    var genericProviders = provider.GetServices<IVerificationProvider>();
    Assert.Contains(genericProviders, p => p is <Name>Provider);
}
```

### 5.3 รันทดสอบบน 3 Runtimes:

```bash
# Test on all target frameworks
dotnet test Netka.Identity.SDK.sln

# Output should show:
# Running on: net5.0
# Running on: net8.0
# Running on: net10.0
# 0 Warnings, 0 Errors
```

---

## ขั้นตอนที่ 6: Build Multi-Target Verification

### 6.1 รันคำสั่ง build:

```bash
cd NKS-Kiosk-Identity-SDK
dotnet build Netka.Identity.SDK.sln -c Release
```

### 6.2 ตรวจสอบผล:

- ✅ Build succeeded (0 Warnings, 0 Errors)
- ✅ คอมไพล์ผ่านบน net5.0, net8.0, net10.0
- ✅ ไม่มี CS1591 warnings (ขาด XML Docs)

---

## ขั้นตอนที่ 7: อัปเดตเอกสาร

### 7.1 อัปเดต `README.md`:
- เพิ่ม <Name> Provider ลงในรายการ Supported Providers

### 7.2 อัปเดต `ARCHITECTURE.md`:
- เพิ่มรายละเอียด <Name> Provider Architecture (Type A/B)
- แสดง flow diagram

### 7.3 อัปเดต `IMPLEMENTATION_NOTES.md`:
- บันทึกข้อสังเกต / Configuration points / Known issues

### 7.4 อัปเดต `identity_verification_sdk_blueprint.md`:
- เพิ่ม <Name> Provider ลงในอัปเดต checklist

---

## Final Checklist

- [ ] Interface (`Core/Interfaces/I<Name>Provider.cs`) ✅
- [ ] Provider Class (`Infrastructure/Providers/<Name>Provider.cs`) ✅
- [ ] Cross-Version Compatibility Rules ตรวจสอบแล้ว ✅
- [ ] XML Docs ครบทุก Public members ✅
- [ ] `[AuditLog]` Attribute ติดแล้ว ✅
- [ ] DI Configuration (`Application/DependencyInjection.cs`) ✅
- [ ] appsettings.json Config ✅
- [ ] Unit Tests (`Tests/<Name>ProviderTests.cs`) ✅
- [ ] DependencyInjectionTests.cs ✅
- [ ] Build & Test: `dotnet test` (0 Warnings, 0 Errors) ✅
- [ ] README.md, ARCHITECTURE.md, IMPLEMENTATION_NOTES.md อัปเดต ✅
- [ ] Git commit สร้างแล้ว ✅

---

**Skill Version:** 1.0.0  
**Last Updated:** 2026-10-12
