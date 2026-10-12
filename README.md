# Netka.Identity.SDK

**Enterprise Identity Verification & Authentication SDK (.NET 8.0 & .NET 10.0 Multi-Target)**  
ไลบรารีมาตรฐานกลางของทีม Netka สำหรับพิสูจน์และยืนยันตัวตน (Digital Identity & Citizen Card Verification) ในระบบ **Web Application** และ **ตู้ยืนยันตัวตน (Browser-Based Kiosk)** โดยรองรับทั้งโปรเจกต์ **`.NET 8.0` (LTS)** และ **`.NET 10.0`** ออกแบบตามหลัก **Clean Architecture** และ **Stateless Pass-Through Adapter** เพื่อให้นำข้อมูลที่ยืนยันตัวตนสำเร็จไปบันทึกลงฐานข้อมูลหรือส่งต่อไปยังระบบปลายทางอื่นๆ ได้อย่างอิสระ

---

## 1. จุดเด่นและแนวคิดหลัก (Core Highlights)

* **Multi-Targeting (`.NET 8.0` & `.NET 10.0`):**
  * ตั้งค่า `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>` ทำให้สามารถนำไป Reference ใช้งานได้ทันทีทั้งในโปรเจกต์ที่เป็น **.NET 8** และ **.NET 10**
* **Stateless & Cookie-Free (ไม่ผูกมัดกับ Session ของเว็บ):**
  * ไม่ฝัง Cookie และไม่ผูกติดกับ `HttpContext` ของ ASP.NET Core ทำให้ฝั่ง Web Backend / Kiosk API สามารถนำข้อมูลผลลัพธ์ (`VerificationResult.Data`) ไปบันทึกลง Database, ออก Token ของตัวเอง, หรือส่งต่อไปยังระบบอื่นได้ 100%
* **แยก Interface ชัดเจนตามผู้ให้บริการ + คืนค่าแพทเทิร์นเดียวกัน (`VerificationResult`):**
  * **`IThaIdProvider`** — รองรับ **DOPA ThaID (OAuth 2.0 + OpenID Connect)** ครบทั้ง 6 เส้นมาตรฐานของกรมการปกครอง
  * **`IDopaProvider`** — รองรับ **DOPA Check Card Service (Direct API)** ทั้งการตรวจสอบด้วยเลขหลังบัตร (`CheckCardByLaser`) และชิปสมาร์ทการ์ด (`CheckCardByCID`)
  * **`IVerificationProvider`** — Base Interface กลางสำหรับการเรียกใช้งานแบบ Polymorphism และรองรับการขยายผลสู่ Provider อื่นในอนาคต (เช่น Google, Microsoft Entra ID, Internal Company SSO)
* **Pass-Through Response Integrity (ไม่ดัดแปลงข้อมูลต้นทาง):**
  * ทำหน้าที่เป็นตัวกลางจัดการเฉพาะระดับ Network, Polly Retry, Circuit Breaker, การเข้ารหัส Header/Request และส่งคืน JSON Response จากผู้ให้บริการต้นทางกลับมาใน `VerificationResult.Data` **ตามจริง 100% โดยไม่ตัดทอนฟิลด์หรือยัดไส้ข้อมูลเพิ่ม**
* **Enterprise Security & PDPA Compliance:**
  * เข้ารหัสข้อมูลส่วนบุคคล (PII เช่น เลขประจำตัวประชาชน 13 หลัก) ด้วย **AES-256-CBC + HMAC-SHA256 (`AesEncryptionService`)** ก่อนบันทึกลง `[PII-VAULT]` Log เสมอ
  * รองรับ **PKCE (RFC 7636 - S256)** และตรวจสอบความถูกต้องของเลขบัตรประชาชน 13 หลัก (**Modulo 11 Checksum**) ก่อนยิง API

---

## 2. โครงสร้างโปรเจกต์ (Solution & Project Structure)

```text
NKS-Kiosk-Identity-SDK/
├── Core/                                      # Domain Layer (ไร้ Dependency ภายนอก)
│   ├── Attributes/
│   │   └── AuditLogAttribute.cs               # [AuditLog] สำหรับ Tri-Log Strategy
│   ├── Interfaces/
│   │   ├── IVerificationProvider.cs           # Base Contract สำหรับทุก Identity Provider
│   │   ├── IThaIdProvider.cs                  # Explicit Contract สำหรับ DOPA ThaID (ครบ 6 เส้น)
│   │   ├── IDopaProvider.cs                   # Explicit Contract สำหรับ DOPA Check Card (Laser & Chip)
│   │   └── IEncryptionService.cs              # Contract สำหรับเข้ารหัส/ถอดรหัส PII (AES-256)
│   ├── Models/
│   │   ├── VerificationRequest.cs             # Pure DTO สำหรับส่งคำขอยืนยันตัวตนแบบรวม
│   │   ├── VerificationResult.cs              # Pure DTO มาตรฐานเดียวสำหรับผลลัพธ์จากทุกเมธอด
│   │   └── VerificationError.cs               # Pure DTO สำหรับรายละเอียดข้อผิดพลาด
│   └── Exceptions/
│       └── VerificationException.cs           # Structured Exception พร้อม ErrorCode & HttpStatusCode
│
├── Application/                               # Registration & Resilience Pipeline Layer
│   └── DependencyInjection.cs                 # AddIdentityVerification() + Polly Retry & Circuit Breaker
│
├── Infrastructure/                            # External API Adapters & Security Implementation
│   ├── Helpers/
│   │   └── ProviderHelper.cs                  # Shared Extensions (IsValidThaiNationalId, JSON Parser, PII Logger)
│   ├── Providers/
│   │   ├── ThaIdProvider.cs                   # DOPA ThaID OAuth 2.0 + OIDC Adapter
│   │   └── DopaProvider.cs                    # DOPA Smart Card / Laser ID Direct API Adapter
│   └── Security/
│       ├── AesEncryptionService.cs            # AES-256-CBC + HMAC-SHA256 (Native Crypto APIs)
│       └── PkceHelper.cs                      # PKCE (RFC 7636) Code Verifier & S256 Challenge
│
├── Tests/                                     # Unit Tests Project (xUnit - 25 Tests x 2 Runtimes = 50 Runs)
│   ├── Netka.Identity.SDK.Tests.csproj        # Multi-Target: net8.0;net10.0
│   ├── MockHttpMessageHandler.cs
│   ├── RuntimeCompatibilityTests.cs           # ตรวจเช็ค TargetFrameworkAttribute & Runtime Version (.NET 8 & .NET 10)
│   ├── SecurityTests.cs
│   ├── ProviderHelperTests.cs
│   ├── ThaIdProviderTests.cs
│   ├── DopaProviderTests.cs
│   └── DependencyInjectionTests.cs
│
├── Netka.Identity.SDK.csproj                  # Main SDK Project (TargetFrameworks: net8.0;net10.0)
└── Netka.Identity.SDK.sln                     # Solution File (เปิดคู่ทั้ง SDK และ Tests ได้ทันที)
```

---

## 3. ตารางสรุป API ทั้งหมดที่มีใน SDK

### A. `IThaIdProvider` (DOPA ThaID — OAuth 2.0 + OpenID Connect)

| ลำดับ | เมธอดใน `IThaIdProvider` | เส้น API ของกรมการปกครอง (Default URL) | หน้าที่การทำงาน |
| :---: | :--- | :--- | :--- |
| **1** | `GetAuthorizationUrl(redirectUri, state, codeChallenge, codeChallengeMethod, nonce)` | `GET .../api/v2/oauth2/auth/` | สร้าง URL สำหรับ Redirect หน้าจอ หรือสร้าง QR Code ให้ประชาชนสแกนด้วยแอป ThaID |
| **2** | `ExchangeCodeAsync(authorizationCode, redirectUri, codeVerifier, ct)` | `POST .../api/v2/oauth2/token/` | นำ `code` ที่ได้จาก Callback ไปแลก `access_token`, `refresh_token`, `id_token`, `pid` |
| **3** | `RefreshTokenAsync(refreshToken, ct)` | `POST .../api/v2/oauth2/token/` | นำ `refresh_token` ไปขอต่ออายุ `access_token` ใหม่ (`grant_type=refresh_token`) |
| **4** | `InspectTokenAsync(accessToken, ct)` | `POST .../api/v2/oauth2/token/inspect/` | ตรวจสอบสถานะความถูกต้องและวันหมดอายุของ Token (`active: true/false`) |
| **5** | `RevokeTokenAsync(token, ct)` | `POST .../api/v2/oauth2/token/revoke/` | ยกเลิกการใช้งาน Token เมื่อจบการทำรายการที่ตู้ Kiosk |
| **6** | `GetUserInfoAsync(accessToken, ct)` | `GET .../api/v2/oauth2/userinfo/` | ใช้ `Bearer access_token` ดึงข้อมูลโปรไฟล์ประชาชนตาม `scope` ที่ขออนุญาต |

---

### B. `IDopaProvider` (DOPA Check Card Service — Direct API)

| ลำดับ | เมธอดใน `IDopaProvider` | พารามิเตอร์ที่ส่งไปตรวจสอบ | หน้าที่การทำงาน |
| :---: | :--- | :--- | :--- |
| **1** | `VerifyByLaserAsync(pid, firstName, lastName, birthDate, laserId, ct)` | `PID`, `FirstName`, `LastName`, `BirthDay` (พ.ศ. `YYYYMMDD`), `Laser` | ตรวจสอบสถานะบัตรประชาชนด้วยข้อมูลหน้าบัตรและรหัสหลังบัตร 12 หลัก (`CheckCardByLaser`) |
| **2** | `VerifyByChipAsync(pid, chipNo, bp1No, ct)` | `PID`, `ChipNo`, `bp1no` | ตรวจสอบสถานะบัตรประชาชนจากข้อมูลที่อ่านได้ผ่านเครื่องอ่านสมาร์ทการ์ด (`CheckCardByCID`) |

---

## 4. วิธีติดตั้งและใช้งาน (Quick Start Guide)

### ขั้นตอนที่ 1: ตั้งค่า `appsettings.json` ในโปรเจกต์ Web App / Kiosk API

> **หมายเหตุ:** URL มาตรฐานทั้ง 5 เส้นของ **ThaID** ถูกตั้งค่าเริ่มต้นไว้ใน SDK ให้แล้ว สามารถระบุเฉพาะ `ClientId`, `ClientSecret`, และ `RedirectUri` ได้เลย (กำหนด URL เพิ่มเฉพาะกรณีต้องการเปลี่ยนไปชี้เครื่อง Sandbox/UAT)

```json
{
  "IdentityVerification": {
    "EncryptionKey": "<คีย์ Base64 ขนาด 32-byte สำหรับเข้ารหัส PII Log>",
    "ThaId": {
      "ClientId": "your-thaid-client-id",
      "ClientSecret": "your-thaid-client-secret",
      "RedirectUri": "https://your-kiosk-web.local/api/identity/thaid/callback",
      "Scope": "openid pid title given_name family_name birthdate",
      "AllowUntrustedCertificates": false
    },
    "Dopa": {
      "LaserUrl": "https://dopa-gateway.your-agency.local/CheckCardByLaser",
      "ChipUrl": "https://dopa-gateway.your-agency.local/CheckCardByCID",
      "ApiKey": "your-dopa-gateway-api-key",
      "AllowUntrustedCertificates": false
    }
  }
}
```

### ขั้นตอนที่ 2: ลงทะเบียน Dependency Injection ใน `Program.cs`

```csharp
using Netka.Identity.Application;

var builder = WebApplication.CreateBuilder(args);

// ลงทะเบียน IEncryptionService, IThaIdProvider, IDopaProvider, IVerificationProvider พร้อม Polly Resilience
builder.Services.AddIdentityVerification(builder.Configuration);

var app = builder.Build();
```

### ขั้นตอนที่ 3: เรียกใช้งานใน Controller หรือ Service เพื่อบันทึกลง Database

```csharp
using Netka.Identity.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/identity")]
public class IdentityVerificationController : ControllerBase
{
    private readonly IThaIdProvider _thaId;
    private readonly IDopaProvider _dopa;

    public IdentityVerificationController(IThaIdProvider thaId, IDopaProvider dopa)
    {
        _thaId = thaId;
        _dopa = dopa;
    }

    // 1. ขอลิงก์ไปแสดง QR Code บนหน้าจอตู้ Kiosk
    [HttpGet("thaid/authorize-url")]
    public IActionResult GetAuthorizeUrl(string redirectUri, string state, string? codeChallenge = null)
    {
        string url = _thaId.GetAuthorizationUrl(redirectUri, state, codeChallenge);
        return Ok(new { authorizationUrl = url });
    }

    // 2. นำ Authorization Code ไปแลก Token และดึงข้อมูลประชาชน
    [HttpPost("thaid/exchange")]
    public async Task<IActionResult> ExchangeThaIdCode(
        string code, string redirectUri, string? codeVerifier, CancellationToken ct)
    {
        var tokenResult = await _thaId.ExchangeCodeAsync(code, redirectUri, codeVerifier, ct);
        if (!tokenResult.IsSuccess)
            return BadRequest(tokenResult.Error);

        // นำข้อมูลใน tokenResult.Data ไปบันทึกลง Database หรือเรียก _thaId.GetUserInfoAsync(...) ต่อได้ทันที
        return Ok(tokenResult.Data);
    }

    // 3. ตรวจสอบสถานะบัตรประชาชนด้วยเลขหลังบัตร (Laser ID)
    [HttpPost("dopa/verify-laser")]
    public async Task<IActionResult> VerifyCardByLaser(
        string pid, string firstName, string lastName, string birthDate, string laserId, CancellationToken ct)
    {
        var result = await _dopa.VerifyByLaserAsync(pid, firstName, lastName, birthDate, laserId, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    // 4. ตรวจสอบสถานะบัตรประชาชนผ่านเครื่องอ่านสมาร์ทการ์ด (Chip No)
    [HttpPost("dopa/verify-chip")]
    public async Task<IActionResult> VerifyCardByChip(
        string pid, string chipNo, string? bp1No, CancellationToken ct)
    {
        var result = await _dopa.VerifyByChipAsync(pid, chipNo, bp1No, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
```

---

## 5. การคอมไพล์และรัน Unit Tests

```powershell
# คอมไพล์และรัน Unit Tests ทั้งหมด (24 Tests) ผ่าน Solution
dotnet test Netka.Identity.SDK.sln

# คอมไพล์เฉพาะตัว SDK หลัก (ไม่รวมโปรเจกต์ Tests)
dotnet build Netka.Identity.SDK.csproj --configuration Release
```
