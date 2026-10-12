# พิมพ์เขียวสถาปัตยกรรม SDK ยืนยันตัวตนระดับองค์กร (`Netka.Identity.SDK` — Clean Architecture & .NET 10)

## 1. บทสรุปผู้บริหารและวัตถุประสงค์ (Executive Summary & Objective)

เอกสารฉบับนี้คือพิมพ์เขียวสถาปัตยกรรมของ **`Netka.Identity.SDK`** ซึ่งเป็น C# Class Library (`.NET 10`) ของทีม Netka ที่ออกแบบมาเพื่อจัดการการพิสูจน์และยืนยันตัวตนดิจิทัลสำหรับระบบ **Web Application** และ **ตู้ยืนยันตัวตน (Browser-Based Kiosk)** โดยเริ่มต้นรองรับผู้ให้บริการหลักของภาครัฐไทย 2 ช่องทาง ได้แก่:
1. **ThaID (`IThaIdProvider` / `ThaIdProvider`)** — ยืนยันตัวตนดิจิทัลผ่านแอปพลิเคชัน ThaID ของกรมการปกครอง (มาตรฐาน **OAuth 2.0 + OpenID Connect**)
2. **DOPA Check Card Service (`IDopaProvider` / `DopaProvider`)** — ตรวจสอบสถานะบัตรประจำตัวประชาชนโดยตรงกับกรมการปกครอง ทั้งแบบรหัสหลังบัตร (**Laser ID**) และชิปสมาร์ทการ์ด (**Smart Card IC Chip**)

เป้าหมายหลักคือการซ่อนความซับซ้อนของโปรโตคอลและ Network Resilience ไว้ภายใต้ Interface ที่ชัดเจน (`IThaIdProvider`, `IDopaProvider`, และ `IVerificationProvider`) โดยไม่ผูกมัดกับระบบ Cookie หรือ Session ของหน้าเว็บ เพื่อให้ระบบตู้ Kiosk / Web Backend สามารถนำข้อมูลที่ยืนยันตัวตนสำเร็จไป **บันทึกลงฐานข้อมูล (Database)** หรือส่งต่อไปยังระบบปลายทางอื่นๆ ได้อย่างอิสระ และรองรับการขยายผลสู่ Provider อื่นในอนาคต (เช่น Google, Microsoft Entra ID, Internal Company SSO)

---

## 2. หลักการออกแบบสถาปัตยกรรมหลัก (Core Architectural Principles)

* **Clean Architecture (3 Layers):** แยกความรับผิดชอบออกเป็น `Core` (Interfaces, Pure DTOs, Exceptions, Attributes), `Application` (Dependency Injection & Polly Resilience Pipeline), และ `Infrastructure` (External API Providers, Shared Helpers, Security Utilities)
* **Stateless & Cookie-Free Adapter:** ไม่ยุ่งกับ `HttpContext` หรือการฝัง Cookie ลงเบราว์เซอร์ ทำหน้าที่เพียงรับพารามิเตอร์ ยืนยันตัวตนกับต้นทาง และส่งข้อมูลกลับมาใน `VerificationResult`
* **Explicit Provider Interfaces + Unified Output Pattern:**
  * แยก Interface ตามผู้ให้บริการ (`IThaIdProvider`, `IDopaProvider`) เพื่อให้เมธอดและพารามิเตอร์ทุกเส้นชัดเจน ไม่ต้องเดาคีย์ใน Dictionary
  * ทุกเมธอดของทุก Provider คืนค่าเป็น **`VerificationResult` แพทเทิร์นเดียวกันทั้งหมด**
* **Pass-Through Response Integrity (No Data Mutation):**
  * ทำหน้าที่เป็นตัวกลางจัดการระดับ Network (Timeouts, Polly Retries, Circuit Breaker, UAT SSL Certificate Hooks) และประกอบ HTTP Headers/Requests เท่านั้น
  * ส่งคืนข้อมูล JSON Response จากผู้ให้บริการต้นทางกลับมาใน `VerificationResult.Data` **ตามจริง 100% โดยไม่ตัดทอน Token และไม่ยัดไส้ฟิลด์เพิ่ม**
* **Tri-Log Strategy & PDPA Compliance:**
  * *Activity Log:* บันทึกสถานะการทำงานทางเทคนิคและ HTTP
  * *Audit Log:* กำกับด้วย `[AuditLog]` Attribute สำหรับติดตาม Action สำคัญทางธุรกิจ
  * *PII Log:* เข้ารหัสข้อมูลส่วนบุคคล (เช่น เลขบัตรประชาชน 13 หลัก `pid`) ด้วย **AES-256-CBC + HMAC-SHA256** ก่อนเขียนลง `[PII-VAULT]` เสมอ

---

## 3. โครงสร้างโปรเจกต์ (Project Structure Layout)

```text
Netka.Identity.SDK/
│
├── Core/
│   ├── Attributes/
│   │   └── AuditLogAttribute.cs
│   ├── Interfaces/
│   │   ├── IVerificationProvider.cs
│   │   ├── IThaIdProvider.cs
│   │   ├── IDopaProvider.cs
│   │   └── IEncryptionService.cs
│   ├── Models/
│   │   ├── VerificationRequest.cs
│   │   ├── VerificationResult.cs
│   │   └── VerificationError.cs
│   └── Exceptions/
│       └── VerificationException.cs
│
├── Application/
│   └── DependencyInjection.cs
│
├── Infrastructure/
│   ├── Helpers/
│   │   └── ProviderHelper.cs
│   ├── Providers/
│   │   ├── ThaIdProvider.cs       (Phase 1: ThaID OAuth 2.0 + OIDC — ครบทั้ง 6 เส้น)
│   │   └── DopaProvider.cs        (Phase 2: DOPA Citizen Card — Laser ID & Smart Card Chip)
│   └── Security/
│       ├── AesEncryptionService.cs
│       └── PkceHelper.cs
│
├── Tests/
│   └── Netka.Identity.SDK.Tests.csproj (Phase 3: Unit Tests 24 ข้อ — แยกออกจาก .dll หลัก)
│
├── Netka.Identity.SDK.csproj
└── Netka.Identity.SDK.sln
```

---

## 4. สัญญา Interfaces และ Models หลัก (`Netka.Identity.Core`)

### A. Interfaces (`IVerificationProvider`, `IThaIdProvider`, `IDopaProvider`, `IEncryptionService`)

```csharp
namespace Netka.Identity.Core.Interfaces;

public interface IVerificationProvider
{
    string ProviderName { get; }
    string GetAuthorizationUrl(string redirectUri, string state);
    Task<VerificationResult> VerifyAsync(VerificationRequest request, CancellationToken cancellationToken = default);
}

public interface IThaIdProvider : IVerificationProvider
{
    // 1. สร้าง URL ล็อกอิน / QR Code (พร้อมรองรับ PKCE และ OIDC nonce)
    string GetAuthorizationUrl(
        string redirectUri,
        string state,
        string? codeChallenge,
        string codeChallengeMethod = "S256",
        string? nonce = null);

    // 2. นำ Authorization Code ไปแลก Access Token / ID Token
    Task<VerificationResult> ExchangeCodeAsync(
        string authorizationCode,
        string? redirectUri = null,
        string? codeVerifier = null,
        CancellationToken cancellationToken = default);

    // 3. ต่ออายุ Access Token ด้วย Refresh Token
    Task<VerificationResult> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    // 4. ตรวจสอบสถานะความถูกต้องของ Token (Token Introspection)
    Task<VerificationResult> InspectTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    // 5. ยกเลิกการใช้งาน Token (Token Revocation)
    Task<VerificationResult> RevokeTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    // 6. ดึงข้อมูลโปรไฟล์ประชาชนจากเส้น /userinfo
    Task<VerificationResult> GetUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}

public interface IDopaProvider : IVerificationProvider
{
    // 1. ตรวจสอบสถานะบัตรด้วยข้อมูลหน้าบัตร + รหัสหลังบัตร (CheckCardByLaser)
    Task<VerificationResult> VerifyByLaserAsync(
        string pid,
        string firstName,
        string lastName,
        string birthDate,
        string laserId,
        CancellationToken cancellationToken = default);

    // 2. ตรวจสอบสถานะบัตรด้วยเลขชิปสมาร์ทการ์ด (CheckCardByCID)
    Task<VerificationResult> VerifyByChipAsync(
        string pid,
        string chipNo,
        string? bp1No = null,
        CancellationToken cancellationToken = default);
}

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
```

### B. Hybrid Models (`VerificationRequest`, `VerificationResult`, `VerificationError`)

```csharp
namespace Netka.Identity.Core.Models;

public class VerificationRequest
{
    public string ProviderName { get; set; } = string.Empty;
    public string? AuthorizationCode { get; set; }
    public string? RedirectUri { get; set; }
    public string? State { get; set; }
    public string? Nonce { get; set; }
    public IDictionary<string, object> AdditionalParameters { get; set; } = new Dictionary<string, object>();
}

public class VerificationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public VerificationError? Error { get; set; }
    
    // ข้อมูล JSON Response ต้นฉบับจากผู้ให้บริการ (ไม่ถูกดัดแปลงหรือตัดทอน)
    public IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}

public class VerificationError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
}
```

---

## 5. มาตรฐานการเขียนโค้ดและการดูแลรักษา (Enforced Coding Standards)

1. **Pure DTO Models Only (`Core/Models/`):**
   * คลาสใน `Core/Models/` ต้องเป็น Pure POCOs (`get; set;` เท่านั้น) ห้ามใส่ Business Logic และห้ามสร้างคลาสโมเดลย่อยปะปนในโฟลเดอร์ `Infrastructure/Providers/`
2. **One File = One Public Class:**
   * 1 ไฟล์ `.cs` ต้องมี Public Class เพียงตัวเดียวเท่านั้น
3. **Shared Provider Helpers (`Infrastructure/Helpers/ProviderHelper.cs`):**
   * รวมฟังก์ชันที่ใช้ซ้ำไว้ที่ `ProviderHelper` (`pid.IsValidThaiNationalId()`, `section.GetRequiredValue()`, `parameters.GetStringValue()`, `CreateFailureResult()`, `ParseJsonToDictionary()`, `LogEncryptedPiiIfPresent()`)
4. **Modern .NET 10 Cryptography:**
   * ใช้ API มาตรฐานในตัวของ .NET 10 (`Aes.EncryptCbc`, `Aes.DecryptCbc`, `HMACSHA256.HashData`, `CryptographicOperations.FixedTimeEquals`) ที่อ่านเข้าใจง่ายและปลอดภัยสูงสุด