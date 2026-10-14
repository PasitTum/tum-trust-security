# Netka.Identity.SDK

**Enterprise Identity Verification & Authentication SDK (`.NET 5.0`, `.NET 8.0`, & `.NET 10.0` Multi-Target)**  
ไลบรารีมาตรฐานกลางของทีม Netka สำหรับพิสูจน์และยืนยันตัวตน (Digital Identity & Citizen Card Verification) ในระบบ **Web Application** และ **ตู้ยืนยันตัวตน (Browser-Based Kiosk)** ออกแบบตามหลัก **Clean Architecture**, **Stateless Pass-Through Adapter**, และ **Zero Third-Party Dependency Single-DLL** เพื่อให้สามารถคัดลอกเฉพาะไฟล์ `Netka.Identity.SDK.dll` ไปใช้งานในโปรเจกต์ปลายทางได้ทันทีโดยไม่มีปัญหาเรื่อง Git Submodule หรือ Private Repo

---

## 📚 สารบัญเอกสารโครงการ (Project Documentation Index)

1. 📄 **[เอกสารส่งมอบระบบและคู่มือการติดตั้ง (System Handover & Integration Guide)](docs/HANDOVER_GUIDE.md)**
   - ภาพรวมระบบ, โครงสร้างโปรเจกต์, วิธีนำเฉพาะไฟล์ `.dll` ไปติดตั้งใน `libs/`, และวิธีตั้งค่าทั้งจาก **Database (`DbContext` / `ISettingRepository`)** และ **`appsettings.json`**
2. 🧪 **[รายงานผลการทดสอบ Unit Test (Unit Test Verification Report)](docs/UNIT_TEST_REPORT.md)**
   - รายงานผลการทดสอบ **45 Test Cases x 3 Runtimes (`.NET 5.0`, `.NET 8.0`, `.NET 10.0`) = 135 Executions (Pass 100%)**
3. 📋 **[รายงาน Use Case และข้อกำหนดการจำลอง (Use Case & Scenario Specification)](docs/USE_CASE_SPECIFICATION.md)**
   - รายละเอียด **12 มาตรฐาน Use Cases** (6 ThaID + 6 DOPA), การทำงานของ **OAuth 2.0 Redirect & QR Code**, และตัวอย่าง JSON ครบ **20 Scopes**
4. 🔐 **[สถาปัตยกรรม ความปลอดภัย PDPA และตารางรหัสข้อผิดพลาด (Security, Architecture & Error Catalog)](docs/SECURITY_AND_ARCHITECTURE.md)**
   - มาตรฐานการเข้ารหัส `AES-256-CBC + HMAC-SHA256`, `PKCE (RFC 7636)`, `IdentityResilienceHandler` (Retry & Circuit Breaker), และตารางรวม `ErrorCode` ทั้งหมด
5. 🔌 **คู่มืออ้างอิงรายผู้ให้บริการ (Provider Reference Docs):**
   - [คู่มือผู้ให้บริการ DOPA ThaID (`IThaIdProvider`)](docs/providers/thaid.md)
   - [คู่มือผู้ให้บริการ DOPA Check Card & Face Verification (`IDopaProvider`)](docs/providers/dopa.md)
   - [คู่มือการขยายระบบเพิ่มผู้ให้บริการใหม่](docs/providers/README.md)
6. 🤖 **แนวทางการจัดการแค่คำปลายทางของ Identity Provider (Automated Provider Roadmap):**
   - [บันทึกแนวทางการปรับปรุงสถาปัตยกรรม Secret Management & Provider Discovery](docs/ROADMAP_SECRET_MANAGEMENT.md)
   - ใช้ `/add-identity-provider` Skill เพื่อให้ AI ค้นหา OAuth Endpoints และสร้าง Provider Implementation อัตโนมัติ

---

## 1. จุดเด่นและแนวคิดหลัก (Core Highlights)

* **Multi-Targeting (`.NET 5.0`, `.NET 8.0` & `.NET 10.0`):**
  * ตั้งค่า `<TargetFrameworks>net5.0;net8.0;net10.0</TargetFrameworks>` รองรับตั้งแต่โปรเจกต์รุ่น `.NET 5.0` ไปจนถึง `.NET 8.0 (LTS)` และ `.NET 10.0`
* **Single-DLL Distribution (`libs/Netka.Identity.SDK.dll`):**
  * ไม่มี Third-Party NuGet DLL พ่วงท้าย สามารถก๊อปปี้เฉพาะ `Netka.Identity.SDK.dll` (และ `.xml`) ไปวางในโฟลเดอร์ `libs/` ของโปรเจกต์ปลายทางแล้ว Commit ขึ้น Git ของโปรเจกต์นั้นได้เลย ใคร `git clone` ไปก็รันและ Build ผ่าน 100%
* **รองรับทั้ง Database Configuration และ `appsettings.json` (พร้อม Lazy Validation & Runtime Hot-Reload):**
  * ดึงค่า `ClientId`, `ClientSecret`, `RedirectUri`, `LaserUrl`, `ChipUrl`, `FaceUrl`, `TokenUrl`, `ApiKey`, `EncryptionKey` จากตารางในฐานข้อมูล (`DbContext` / `ISettingRepository` / `IIdentityOptionsProvider`) หรือจาก `appsettings.json` ได้โดยตรง และรองรับการแก้ค่าใน Database แบบ Hot-Reload
* **Built-in `UseMock = true` (จำลองครบทุก Endpoint + Loopback Redirect ในตัว `.dll`):**
  * แม้มีแค่ไฟล์ `.dll` ไฟล์เดียว ก็สามารถเปิด `UseMock = true` เพื่อทดสอบทั้ง **ThaID (ครบ 20 Scopes + Loopback Redirect + วาด QR Code + OIDC Discovery/JWKS/ID Token)** และ **DOPA (Laser ID, Smart Card Chip, Biometric Face Verification 1:1 และ OAuth Token)** ได้ทันทีโดยไม่ต้องต่ออินเทอร์เน็ต
* **Enterprise Security & PDPA Compliance:**
  * เข้ารหัสข้อมูลส่วนบุคคล (PII เช่น เลขประจำตัวประชาชน 13 หลัก) ด้วย **AES-256-CBC + HMAC-SHA256 (`AesEncryptionService`)** ก่อนบันทึกลง `[PII-VAULT]` Log เสมอ
  * รองรับ **PKCE (RFC 7636 - S256)**, `PkceHelper.RandomUrlSafe(...)`, `ProviderHelper.MaskPid()`, และตรวจสอบความถูกต้องของเลขบัตรประชาชน 13 หลัก (**Modulo 11 Checksum**) ก่อนยิง API
* **Automated Provider Discovery & Extensibility (AI-Ready):**
  * **OpenID Connect Discovery** — SDK สามารถดึง Endpoint URLs โดยอัตโนมัติจาก `.well-known/openid-configuration` ของ OAuth 2.0 Provider ใดๆ ไม่ต้องกำหนดเอง
  * **Provider Registry Pattern** — Architecture รองรับการค้นหา (Discovery) และการลงทะเบียนผู้ให้บริการใหม่ที่เพิ่มเข้ามาในลักษณะ Plug-and-Play โดยไม่ต้องแก้ไข Core SDK Code
  * **Multi-Provider Factory** — ระบบ `IProviderFactory` เลือก Provider ที่เหมาะสมอัตโนมัติตามการตั้งค่าบน Kiosk (จะขยายรองรับ Azure AD, Google, Apple SignIn, eIDAS ฯลฯ ในอนาคต)
  * **AI-Driven Provider Onboarding** — ทีม Architecture ออกแบบให้ AI (เช่น Claude Code) สามารถช่วย:
    - ✓ ค้นหา OAuth 2.0 endpoints โดยอัตโนมัติจากชื่อ Provider
    - ✓ สร้าง `IVerificationProvider` implementation จากการวิเคราะห์เอกสาร API
    - ✓ แมป scopes/fields จากผู้ให้บริการใหม่ไปยัง Netka Unified Data Model
    - ✓ สร้าง Unit Tests และ Mock Scenarios โดยอัตโนมัติ
    - (ดู [Provider Extensibility Guide](docs/providers/README.md) สำหรับรายละเอียด)

---

## 2. ตารางสรุป API ทั้งหมดที่มีใน SDK (100% Full Provider Endpoint Coverage)

### A. `IThaIdProvider` (DOPA ThaID — OAuth 2.0 + OpenID Connect)

| ลำดับ | เมธอดใน `IThaIdProvider` | เส้น API ของกรมการปกครอง (Default URL) | หน้าที่การทำงาน |
| :---: | :--- | :--- | :--- |
| **1** | `GetAuthorizationUrl(redirectUri, state, codeChallenge, codeChallengeMethod, nonce)` | `GET .../api/v2/oauth2/auth/` | สร้าง URL สำหรับแสดง QR Code บนหน้าจอ Kiosk หรือสั่ง Redirect หน้าจอ (ในโหมด `UseMock = true` จะคืน Loopback Callback URL) |
| **2** | `ExchangeCodeAsync(authorizationCode, redirectUri, codeVerifier, ct)` | `POST .../api/v2/oauth2/token/` | นำ `code` ที่ได้จาก Callback ไปแลก `access_token`, `refresh_token`, `id_token` และข้อมูลประชาชนครบ 20 Scopes |
| **3** | `RefreshTokenAsync(refreshToken, ct)` | `POST .../api/v2/oauth2/token/` | นำ `refresh_token` ไปขอต่ออายุ `access_token` ใหม่ (`grant_type=refresh_token`) |
| **4** | `InspectTokenAsync(accessToken, ct)` | `POST .../api/v2/oauth2/introspect/` | ตรวจสอบสถานะความถูกต้องและวันหมดอายุของ Token (`active: true/false`) |
| **5** | `RevokeTokenAsync(token, ct)` | `POST .../api/v2/oauth2/revoke/` | ยกเลิกการใช้งาน Token เมื่อจบการทำรายการที่ตู้ Kiosk |
| **6** | `GetUserInfoAsync(accessToken, ct)` | `GET .../api/v2/oauth2/userinfo/` | ใช้ `Bearer access_token` ดึงข้อมูลโปรไฟล์ประชาชนตาม `scope` ที่ขออนุญาต |
| **7** | `GetOpenIdConfigurationAsync(ct)` | `GET .../.well-known/openid-configuration` | ดึงข้อมูล OIDC Discovery Metadata ของระบบ ThaID |
| **8** | `GetJwksAsync(ct)` | `GET .../jwks/` | ดึง Public Keys (`EC` P-256 `ES256`) จาก JWKS Endpoint ของระบบ ThaID |
| **9** | `ValidateIdTokenAsync(idToken, expectedNonce, ct)` | ตรวจสอบในหน่วยความจำ + `JwksUrl` | ถอดรหัสและตรวจสอบความถูกต้องของ `id_token` (`exp`, `iss`, `aud`, `nonce`, และลายเซ็นดิจิทัล `ES256`/`RS256`) |
| **10** | `VerifyAsync(request, ct)` | `POST .../api/v2/oauth2/token/` | ตรวจสอบ `expected_state` ป้องกัน CSRF และแลก `AuthorizationCode` ผ่าน `VerificationRequest` |

### B. `IDopaProvider` (DOPA Check Card & Biometric Face Service — Direct API)

| ลำดับ | เมธอดใน `IDopaProvider` | พารามิเตอร์ที่ส่งไปตรวจสอบ | หน้าที่การทำงาน |
| :---: | :--- | :--- | :--- |
| **1** | `VerifyByLaserAsync(pid, firstName, lastName, birthDate, laserId, ct)` | `PID`, `FirstName`, `LastName`, `BirthDay` (พ.ศ. `YYYYMMDD`), `Laser` | ตรวจสอบสถานะบัตรประชาชนด้วยข้อมูลหน้าบัตรและรหัสหลังบัตร 12 หลัก (`CheckCardByLaser`) |
| **2** | `VerifyByChipAsync(pid, chipNo, bp1No, ct)` | `PID`, `ChipNo`, `bp1no` | ตรวจสอบสถานะบัตรประชาชนจากข้อมูลที่อ่านได้ผ่านเครื่องอ่านสมาร์ทการ์ด (`CheckCardByCID`) |
| **3** | `VerifyByFaceAsync(pid, faceImageBase64, threshold, ct)` | `pid`, `image` (ตัด `data:image/...;base64,` อัตโนมัติ) | ตรวจสอบเปรียบเทียบใบหน้า 1:1 กับฐานข้อมูลทะเบียนราษฎร (`similarity`, `confidence`, `match`, `result_code`) |
| **4** | `RequestAccessTokenAsync(ct)` | `grant_type=client_credentials`, `client_id`, `client_secret` | ขอ OAuth 2.0 Bearer Access Token จาก DOPA Gateway (SDK เรียกและ Cache ให้อัตโนมัติเมื่อตั้งค่า `TokenUrl` + `ClientId`) |
| **5** | `VerifyAsync(request, ct)` | `request.AdditionalParameters` | ส่งพารามิเตอร์ตรวจสอบแบบยืดหยุ่นผ่าน Dictionary (รองรับทั้งตรวจบัตรและตรวจใบหน้า) |

---

## 2.5 🤖 AI-Powered Provider Discovery & Integration

SDK ออกแบบให้ AI tools (Claude Code / ChatGPT) สามารถช่วยค้นหาและเพิ่มผู้ให้บริการด้านตัวตนใหม่โดยอัตโนมัติ ผ่านกระบวนการต่อไปนี้:

### 🔍 **Phase A: Automated Provider Discovery**

**AI ค้นหา OAuth/OIDC Endpoints เอง:**

```csharp
// ❌ Old Way (Manual Configuration)
options.ThaId.AuthorizeUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/";
options.ThaId.TokenUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/token/";
options.ThaId.UserInfoUrl = "https://imauth.bora.dopa.go.th/api/v2/oauth2/userinfo/";
options.ThaId.JwksUrl = "https://imauth.bora.dopa.go.th/jwks/";

// ✅ New Way (AI Discovered Automatically)
// AI reads .well-known/openid-configuration and populates all endpoints
var discoveredConfig = await discoveryService.FetchOpenIdConfigAsync(
    discoveryEndpoint: "https://imauth.bora.dopa.go.th/.well-known/openid-configuration"
);
// → Endpoints auto-populated: authorize_endpoint, token_endpoint, userinfo_endpoint, jwks_uri
```

### 📋 **Phase B: AI-Generated Provider Implementation Template**

AI สามารถสร้าง `IVerificationProvider` implementation ใหม่จากชื่อ Provider และ API Docs:

**Input ที่ AI ต้องการ:**
```
Provider Name: "Microsoft Entra ID"
Discovery Endpoint: "https://login.microsoftonline.com/common/.well-known/openid-configuration"
Target Scopes: ["openid", "profile", "email", "offline_access"]
Required Claims: ["oid", "sub", "email"]
```

**Output ที่ AI สร้างออกมา:**
```csharp
public class MicrosoftEntraIdProvider : IOAuthProvider
{
    public string ProviderName => "Microsoft Entra ID";

    public async Task<VerificationResult> VerifyAsync(VerificationRequest request, CancellationToken ct)
    {
        var openIdConfig = await _discoveryService.FetchOpenIdConfigAsync(
            "https://login.microsoftonline.com/common/.well-known/openid-configuration", ct
        );
        
        var authUrl = $"{openIdConfig.AuthorizeEndpoint}?" +
            $"client_id={_options.ClientId}&" +
            $"redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}&" +
            $"response_type=code&scope=openid+profile+email";
        
        return new VerificationResult 
        { 
            Success = true, 
            AuthorizationUrl = authUrl,
            ProviderName = ProviderName 
        };
    }
}
```

### 🧪 **Phase C: Automated Test Generation**

AI สร้าง Mock Scenarios + Unit Tests ให้:

```csharp
// AI generates these test cases automatically
public class MicrosoftEntraIdProviderTests
{
    [Fact]
    public async Task VerifyAsync_WithValidCode_ReturnsUserInfo()
    {
        // Arrange
        var provider = new MicrosoftEntraIdProvider(_options, _httpClient);
        var request = new VerificationRequest 
        { 
            AuthorizationCode = "mock-auth-code",
            RedirectUri = "http://localhost:5201/callback"
        };
        
        // Act
        var result = await provider.VerifyAsync(request);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("Microsoft Entra ID", result.ProviderName);
        Assert.NotNull(result.Data);
    }
}
```

### 📦 **Phase D: Provider Registry Auto-Population**

DB migration ที่ AI สร้าง:

```sql
-- AI generates INSERT statements for new provider
INSERT INTO identity_providers (id, name, type, enabled, priority, config)
VALUES (
    'b4c2a8f0-1234-5678-90ab-cdef12345678',
    'Microsoft Entra ID',
    'oauth',
    true,
    10,
    JSON_OBJECT(
        'discovery_endpoint', 'https://login.microsoftonline.com/common/.well-known/openid-configuration',
        'client_id', '<SET_BY_ADMIN>',
        'client_secret', '<ENCRYPTED>',
        'redirect_uri', 'http://kiosk-web:5201/callback',
        'scopes', JSON_ARRAY('openid', 'profile', 'email')
    )
);
```

---

### 🚀 **How to Use AI for Provider Discovery**

**ทีมสามารถใช้ Claude Code Skill: `/add-identity-provider`**

```bash
# Example: Let AI discover and add Google One Tap
/add-identity-provider "Google One Tap"

# AI จะทำให้อัตโนมัติ:
# 1. ✓ ค้นหา https://accounts.google.com/.well-known/openid-configuration
# 2. ✓ สร้าง GoogleOneProviderTapProvider class
# 3. ✓ สร้าง Unit Tests (ทั้งสำเร็จและ error scenarios)
# 4. ✓ สร้าง Mock Scenarios สำหรับ Sandbox Testing
# 5. ✓ อัปเดต Provider Registry ในฐานข้อมูล
# 6. ✓ Commit code พร้อม commit message
```

---

### 🔐 **Security Guarantees**

- ✓ **Secret Isolation:** AI generates placeholders (`<SET_BY_ADMIN>`, `<ENCRYPTED>`), ไม่ embed secrets
- ✓ **PKCE Always:** AI generates code ที่บังคับใช้ PKCE (RFC 7636) โดยค่าเริ่มต้น
- ✓ **Scope Validation:** AI ตรวจสอบ Scopes ก่อนสร้าง Implementation
- ✓ **Mock Server:** AI สร้าง Mock endpoint สำหรับทดสอบแม้ไม่มีการเข้าถึง Provider จริง

---

## 3. วิธีติดตั้งและใช้งานอย่างย่อ (Quick Start)

### 3.1 อ้างอิง `libs/Netka.Identity.SDK.dll` ใน `.csproj` ของโปรเจกต์ปลายทาง
```xml
<ItemGroup>
  <Reference Include="Netka.Identity.SDK">
    <HintPath>libs\Netka.Identity.SDK.dll</HintPath>
  </Reference>
</ItemGroup>
```

### 3.2 ลงทะเบียนใน `Program.cs` (เลือกได้ทั้งจาก Database หรือ `appsettings.json`)

**แบบดึงจาก Database (หรือเปิดโหมด `UseMock = true`):**
```csharp
using Netka.Identity.Application;

builder.Services.AddIdentityVerification((sp, options) =>
{
    var settingRepo = sp.GetRequiredService<ISystemSettingRepository>();

    options.UseMock = settingRepo.GetBool("Identity:UseMock"); // ถ้าเป็น true จะจำลองใน .dll ทันทีไม่ต้องออกเน็ต
    options.MockScenario = "success"; // "success" | "deny" | "invalid_state" | "expired_code" | "timeout"

    options.ThaId.ClientId = settingRepo.GetValue("ThaID:ClientId");
    options.ThaId.ClientSecret = settingRepo.GetValue("ThaID:ClientSecret");
    options.ThaId.RedirectUri = settingRepo.GetValue("ThaID:RedirectUri");

    options.Dopa.LaserUrl = settingRepo.GetValue("Dopa:LaserUrl");
    options.Dopa.ChipUrl = settingRepo.GetValue("Dopa:ChipUrl");
    options.Dopa.ApiKey = settingRepo.GetValue("Dopa:ApiKey");
});
```

**แบบดึงจาก `appsettings.json`:**
```csharp
builder.Services.AddIdentityVerification(builder.Configuration);
```

---

## 4. คำสั่ง Build และรันชุดทดสอบ (Build & Test Commands)

```powershell
# 1. รัน Unit Tests ทั้งหมด (43 Tests x 3 Target Frameworks = 129 Executions)
dotnet test "Netka.Identity.SDK.sln" -c Release

# 2. Build ไฟล์ Netka.Identity.SDK.dll (Release) สำหรับนำไปแจกจ่ายใส่โฟลเดอร์ libs/
dotnet build "Netka.Identity.SDK.csproj" -c Release

# 3. (ทางเลือกเสริม) รัน Standalone Sandbox Mock Server บน http://localhost:5199
dotnet run --project "MockServer/Netka.Identity.MockServer.csproj"

# 4. ซิงค์ AI Skills (/integrate-netka-identity และ /add-identity-provider) ไปยังระดับ Global
.\install-global-skills.ps1
```
