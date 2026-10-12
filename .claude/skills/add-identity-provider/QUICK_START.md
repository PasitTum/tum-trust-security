# add-identity-provider — Quick Start Examples

ตัวอย่างจริง + checklist ให้เตรียมตัวก่อนเรียก `/add-identity-provider`

---

## Example 1: Google OAuth 2.0 (Type A)

### ข้อมูลพื้นฐาน

```
Provider Name:       Google
Type:                OAuth 2.0 (Type A)
Documentation:       https://developers.google.com/identity/protocols/oauth2
Authorization Flow:  User login → redirect with code → token exchange → user info
Scopes:              openid, profile, email
```

### Spec API ที่ต้องส่ง

**Endpoint 1: Authorization**
```
GET /o/oauth2/v2/auth
Query Parameters:
  - client_id: string
  - redirect_uri: string
  - response_type: "code"
  - scope: "openid profile email"
  - state: string
Response: Redirect to login page
```

**Endpoint 2: Token Exchange**
```
POST /token
Content-Type: application/x-www-form-urlencoded
Body:
  - grant_type: "authorization_code"
  - code: string
  - redirect_uri: string
  - client_id: string
  - client_secret: string
Response (JSON):
{
  "access_token": "...",
  "expires_in": 3600,
  "refresh_token": "...",
  "token_type": "Bearer",
  "id_token": "..." (JWT)
}
```

**Endpoint 3: User Info**
```
GET /userinfo
Headers:
  Authorization: Bearer {access_token}
Response (JSON):
{
  "sub": "user-id",
  "email": "user@example.com",
  "name": "John Doe",
  "picture": "https://..."
}
```

### Pre-Submission Checklist

```
☐ ทำความเข้าใจว่า Google ใช้ OAuth 2.0 (Type A)
☐ ตรวจสอบ Scopes ที่ต้องการ (openid, profile, email, etc.)
☐ ยืนยัน Redirect URI ที่ใช้งาน (เช่น https://myapp.com/callback)
☐ เตรียม Client ID + Client Secret จาก Google Console
☐ ตรวจสอบ Endpoint URLs (ต้องใช้ v2 ไม่ใช่ v1)
```

### Skill Execution

```bash
/add-identity-provider

# When prompted:
# [A] URL API Documentation
# → Paste: https://developers.google.com/identity/protocols/oauth2

# Skill will:
# 1. Fetch documentation
# 2. Create I<GoogleProvider.cs
# 3. Create GoogleProvider.cs
# 4. Create GoogleProviderTests.cs
# 5. Update DependencyInjection.cs
# 6. Verify multi-target build
# 7. Update documentation
```

### Configuration (appsettings.json)

```json
{
  "IdentityVerification": {
    "Google": {
      "ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "YOUR_CLIENT_SECRET",
      "AuthorizationEndpoint": "https://accounts.google.com/o/oauth2/v2/auth",
      "TokenEndpoint": "https://oauth2.googleapis.com/token",
      "UserInfoEndpoint": "https://www.googleapis.com/oauth2/v2/userinfo",
      "RefreshTokenEndpoint": "https://oauth2.googleapis.com/token",
      "InspectTokenEndpoint": "https://www.googleapis.com/tokeninfo",
      "RevokeTokenEndpoint": "https://oauth2.googleapis.com/revoke",
      "Scopes": "openid profile email",
      "AllowUntrustedCertificates": false,
      "TimeoutSeconds": 30
    }
  }
}
```

### Expected Files Created

```
Core/Interfaces/IGoogleProvider.cs
Infrastructure/Providers/GoogleProvider.cs
Tests/GoogleProviderTests.cs
(Modified) Application/DependencyInjection.cs
(Modified) README.md, ARCHITECTURE.md
```

### Verification Steps

```bash
# Build + Test
dotnet build Netka.Identity.SDK.sln -c Release
dotnet test Netka.Identity.SDK.sln

# Expected: ✅ 0 Warnings, 0 Errors, all 3 runtimes passed
```

---

## Example 2: DOPA (ระบบสินชาติอิเล็กทรอนิกส์) (Type B)

### ข้อมูลพื้นฐาน

```
Provider Name:       Dopa
Type:                Direct REST (Type B)
Documentation:       Internal/Confidential (Thai Government)
Verification Flow:   Server sends citizen data → DOPA validates → returns result
Auth Type:           API Key / Bearer Token
No User Redirect:    ❌ Not applicable for Type B
```

### Spec API ที่ต้องส่ง

**Endpoint: Verify National ID**
```
POST /api/verify
Headers:
  Authorization: Bearer {api-key}
  Content-Type: application/json
Body:
{
  "pid": "1234567890123",              // 13-digit Thai ID
  "first_name": "สมชาย",
  "last_name": "ใจดี",
  "birth_date": "1990-01-15",
  "address": "123 Moo 4..."
}
Response (JSON):
{
  "status": "success",                 // or "failed"
  "message": "Verified",
  "citizen_id": "1234567890123",
  "name_th": "สมชาย ใจดี",
  "birth_date": "1990-01-15",
  "citizenship": "Thai",
  "card_status": "active",
  "confidence_score": 0.95
}
```

### Pre-Submission Checklist

```
☐ ตรวจสอบว่า Type B (ไม่มี Authorization URL)
☐ เตรียม API Key / Bearer Token
☐ ยืนยัน Request/Response format (JSON)
☐ ตรวจสอบ Error responses (400, 401, 500, etc.)
☐ ยืนยัน Rate limiting / Throttling (if any)
☐ ตรวจสอบ Encryption requirements (HTTPS, mTLS, etc.)
```

### Skill Execution

```bash
/add-identity-provider

# When prompted:
# [B] ข้อความ / JSON / cURL
# → Paste API spec (as shown above)

# Or:
# [C] ชื่อ Provider
# → Type: "DOPA" or "Thai ID Verification"
# → Skill will search for official documentation

# Skill will:
# 1. Analyze spec
# 2. Classify as Type B (no AuthorizationUrl)
# 3. Create IDopaProvider.cs (custom method instead)
# 4. Create DopaProvider.cs (direct verification)
# 5. Create DopaProviderTests.cs
# 6. Update DependencyInjection.cs
# 7. Verify multi-target build
```

### Configuration (appsettings.json)

```json
{
  "IdentityVerification": {
    "Dopa": {
      "ApiKey": "your-api-key-here",
      "VerifyEndpoint": "https://dopa-api.thailand.go.th/api/verify",
      "AllowUntrustedCertificates": false,
      "TimeoutSeconds": 30,
      "RetryCount": 3
    }
  }
}
```

### Interface Difference (Type B)

```csharp
// ❌ Type B does NOT have these:
public interface IDopaProvider : IVerificationProvider
{
    // ❌ Task<string> GetAuthorizationUrlAsync(...);   // Not for Type B
    // ❌ Task<VerificationResult> RefreshAccessTokenAsync(...);
    // ❌ Task<VerificationResult> RevokeTokenAsync(...);

    // ✅ Type B HAS custom verification methods:
    Task<VerificationResult> VerifyNationalIdAsync(
        string pid,
        string firstName,
        string lastName,
        string birthDate,
        string address);

    // And general interface requirement:
    Task<VerificationResult> Verify(Dictionary<string, string> parameters);
}
```

### Expected Files Created

```
Core/Interfaces/IDopaProvider.cs
Infrastructure/Providers/DopaProvider.cs
Tests/DopaProviderTests.cs
(Modified) Application/DependencyInjection.cs
(Modified) README.md, ARCHITECTURE.md
```

### Verification Steps

```bash
# Build + Test
dotnet build Netka.Identity.SDK.sln -c Release
dotnet test Netka.Identity.SDK.sln

# Expected: ✅ 0 Warnings, 0 Errors, all 3 runtimes passed
```

---

## Master Checklist (Before Running Skill)

### Phase 1: Preparation (30 min)

- [ ] **Find Official Documentation**
  - [ ] Type A: Look for OAuth 2.0 / OpenID Connect spec
  - [ ] Type B: Look for REST API spec with examples

- [ ] **Collect Endpoints**
  - [ ] [ ] List all endpoints (authorize, token, userinfo, refresh, etc.)
  - [ ] [ ] Note HTTP methods (GET, POST, etc.)
  - [ ] [ ] Record required headers (Authorization, Content-Type)
  - [ ] [ ] Document request/response JSON structures

- [ ] **Prepare Configuration**
  - [ ] [ ] Obtain Client ID / API Key
  - [ ] [ ] Obtain Client Secret (keep secure)
  - [ ] [ ] Know Redirect URI (for Type A)
  - [ ] [ ] Know Scopes (for Type A)

- [ ] **Classify Provider**
  - [ ] [ ] Decide: Type A (OAuth 2.0) or Type B (Direct REST)?
  - [ ] [ ] If Type A: confirm Authorization URL + redirect flow
  - [ ] [ ] If Type B: confirm direct API endpoints + no redirect

### Phase 2: Run Skill (1-3 hours)

- [ ] **Call Skill**
  - [ ] [ ] `bash /add-identity-provider`
  - [ ] [ ] Provide spec (URL / text / provider name)

- [ ] **Follow Step-by-Step**
  - [ ] [ ] Step 1: Spec Analysis ✓
  - [ ] [ ] Step 2: Create Interface + Provider ✓
  - [ ] [ ] Step 3: Cross-Version Compatibility ✓
  - [ ] [ ] Step 4: DI Configuration ✓
  - [ ] [ ] Step 5: Unit Tests ✓

- [ ] **Code Review Generated Files**
  - [ ] [ ] Check XML Docs completeness
  - [ ] [ ] Verify Cross-Version patterns (no `.NET 6+` APIs)
  - [ ] [ ] Confirm all `ProviderHelper` methods used
  - [ ] [ ] Ensure `[AuditLog]` attribute present

### Phase 3: Build & Test (15 min)

- [ ] **Multi-Target Build**
  ```bash
  dotnet build Netka.Identity.SDK.sln -c Release
  ```
  - [ ] [ ] Build succeeded
  - [ ] [ ] 0 Warnings
  - [ ] [ ] 0 Errors
  - [ ] [ ] net5.0, net8.0, net10.0 all compiled

- [ ] **Unit Tests**
  ```bash
  dotnet test Netka.Identity.SDK.sln
  ```
  - [ ] [ ] All tests passed on net5.0
  - [ ] [ ] All tests passed on net8.0
  - [ ] [ ] All tests passed on net10.0
  - [ ] [ ] 0 Warnings
  - [ ] [ ] 0 Errors

### Phase 4: Documentation (15 min)

- [ ] **Update Project Docs**
  - [ ] [ ] README.md: Add provider to Supported Providers list
  - [ ] [ ] ARCHITECTURE.md: Add flow diagram + config details
  - [ ] [ ] IMPLEMENTATION_NOTES.md: Record any gotchas
  - [ ] [ ] identity_verification_sdk_blueprint.md: Mark as Completed

- [ ] **Git Commit**
  - [ ] [ ] Stage all new files + modifications
  - [ ] [ ] Write meaningful commit message
  - [ ] [ ] Commit with attribution line
  - [ ] [ ] (Optional) Create PR for code review

### Phase 5: Final Verification (5 min)

- [ ] **Test in Isolation**
  ```csharp
  // Can this code be instantiated?
  var services = new ServiceCollection();
  var config = ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
  services.AddIdentityVerification(config);
  
  var provider = services
    .BuildServiceProvider()
    .GetRequiredService<I[PROVIDER_NAME]Provider>();
  
  Assert.NotNull(provider);
  ```

- [ ] **Verify Configuration**
  - [ ] [ ] appsettings.json has all required keys
  - [ ] [ ] No secrets hardcoded in code
  - [ ] [ ] Environment variable overrides work (if needed)

- [ ] **Documentation Review**
  - [ ] [ ] New provider listed in README
  - [ ] [ ] Flow diagram added to ARCHITECTURE.md
  - [ ] [ ] Known issues documented

---

## Common Mistakes to Avoid

### ❌ Do NOT

```csharp
// 1. Use .NET 6+ APIs
ArgumentNullException.ThrowIfNull(x);                  // ❌ net5.0 fails
ArgumentException.ThrowIfNullOrWhiteSpace(s);          // ❌ net5.0 fails
char.IsAsciiLetterOrDigit(c);                          // ❌ net5.0 fails

// 2. Pass bare Dictionary to FormUrlEncodedContent
new FormUrlEncodedContent(formFields)                  // ❌ CS8620 on net5.0

// 3. Culture-dependent string comparison
if (key == "VALUE")                                    // ❌ Not OrdinalIgnoreCase
dict = new Dictionary<string, string>();              // ❌ No StringComparer

// 4. Modify API Response
var response = await _http.GetAsync(...);
var json = await response.Content.ReadAsStringAsync();
var data = JsonSerializer.Deserialize<...>(json);
data.Remove("access_token");                           // ❌ LOSE ORIGINAL DATA
return new VerificationResult { Data = data };

// 5. Log sensitive data plaintext
_logger.LogInformation($"Token: {accessToken}");       // ❌ PII in logs

// 6. Mix multiple classes in one file
public class GoogleProvider { }
public class GoogleProviderOptions { }                 // ❌ 1 file = 1 class only

// 7. Forget XML Docs
public async Task<VerificationResult> GetUserProfileAsync(string token)
{
    // No XML Docs = CS1591 warning
}

// 8. Forget [AuditLog] attribute
public class GoogleProvider { }                        // ❌ Missing [AuditLog]

// 9. Forget DI registration
// No services.AddTransient<IGoogleProvider>(...)      // ❌ Service not found
```

### ✅ DO

```csharp
// 1. Use cross-version compatible patterns
if (x is null) throw new ArgumentNullException(nameof(x));
if (string.IsNullOrWhiteSpace(s)) throw new ArgumentException("...");

// 2. Use compatible FormUrlEncodedContent
new FormUrlEncodedContent(
  dict.Select(kvp => new KeyValuePair<string?, string?>(kvp.Key, kvp.Value)))

// 3. Use OrdinalIgnoreCase everywhere
var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
if (key.Equals("VALUE", StringComparison.OrdinalIgnoreCase)) { }

// 4. Keep API Response intact
var data = ProviderHelper.ParseJsonToDictionary(json, "success");
return new VerificationResult { Data = data };  // 100% unchanged

// 5. Log with encryption
_logger.LogEncryptedPiiIfPresent(
  _encryptionService, ProviderName, data, "access_token", "refresh_token");

// 6. One class per file
public class GoogleProvider { }                 // GoogleProvider.cs

// 7. Always write XML Docs
/// <summary>
/// Gets user profile using access token.
/// </summary>
public async Task<VerificationResult> GetUserProfileAsync(string token)
{
    // Implementation
}

// 8. Add [AuditLog] attribute
[AuditLog]
public class GoogleProvider { }

// 9. Register in DI
services.AddTransient<IGoogleProvider>(...);
services.AddTransient<IVerificationProvider>(...);
```

---

## Quick Test Commands

```bash
# Restore packages
dotnet restore Netka.Identity.SDK.sln

# Clean build
dotnet clean Netka.Identity.SDK.sln
dotnet build Netka.Identity.SDK.sln -c Release

# Run specific test file
dotnet test Netka.Identity.SDK.sln -k GoogleProviderTests

# Run with verbose output
dotnet test Netka.Identity.SDK.sln -v detailed

# Test specific framework
dotnet test Netka.Identity.SDK.sln -f net5.0
dotnet test Netka.Identity.SDK.sln -f net8.0
dotnet test Netka.Identity.SDK.sln -f net10.0
```

---

## Next Steps After Skill Completes

1. **Review Generated Code**
   - Open `Core/Interfaces/I[Name]Provider.cs`
   - Open `Infrastructure/Providers/[Name]Provider.cs`
   - Spot-check for issues

2. **Run Full Test Suite**
   ```bash
   dotnet test Netka.Identity.SDK.sln
   ```

3. **Update Configuration**
   - Add real API credentials to `appsettings.Development.json`
   - Keep `appsettings.json` with placeholder values

4. **Create Git Commit**
   ```bash
   git add Core/Interfaces/I[Name]Provider.cs \
           Infrastructure/Providers/[Name]Provider.cs \
           Tests/[Name]ProviderTests.cs \
           Application/DependencyInjection.cs \
           README.md ARCHITECTURE.md
   
   git commit -m "feat: add [Name] identity provider support"
   ```

5. **(Optional) Submit PR**
   - Create PR for code review
   - Attach test results
   - Request approval

---

**Quick Start Version:** 1.0.0  
**Last Updated:** 2026-10-12
