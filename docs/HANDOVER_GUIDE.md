# เอกสารส่งมอบระบบ (System Handover & Integration Guide)
**โปรเจกต์:** `Netka.Identity.SDK`  
**รุ่น Framework ที่รองรับ:** `.NET 5.0`, `.NET 8.0 (LTS)`, และ `.NET 10.0` (Multi-Target)  
**สถาปัตยกรรม:** Clean Architecture / Stateless Pass-Through Identity Verification SDK (Single-DLL Distribution)

---

## 1. บทสรุปผู้บริหารและขอบเขตงานที่ส่งมอบ (Executive Summary)

`Netka.Identity.SDK` คือไลบรารีมาตรฐานกลางของทีมสำหรับเชื่อมต่อระบบพิสูจน์และยืนยันตัวตนภาครัฐ ได้แก่:
1. **DOPA ThaID (`IThaIdProvider`)** — มาตรฐาน OAuth 2.0 / OpenID Connect (OIDC) พร้อม PKCE (`S256`) รองรับข้อมูลประชาชนครบทั้ง **20 Scopes** ตามคู่มือกรมการปกครอง
2. **DOPA Check Card Service (`IDopaProvider`)** — ตรวจสอบสถานะบัตรประจำตัวประชาชนผ่านเลขหลังบัตร (`CheckCardByLaser`) และผ่านเครื่องอ่านสมาร์ทการ์ด (`CheckCardByCID`)

### จุดเด่นสำคัญในการส่งมอบครั้งนี้
- **Single-DLL Zero-Dependency Distribution (`libs/Netka.Identity.SDK.dll`):**
  - ถอด Dependency ภายนอก (`Polly`) ออกและสร้าง `IdentityResilienceHandler` (Exponential Backoff Retry + Circuit Breaker) ไว้ภายใน SDK โดยตรง
  - โปรเจกต์ปลายทางสามารถก๊อปปี้เฉพาะไฟล์ **`Netka.Identity.SDK.dll`** (และ `Netka.Identity.SDK.xml`) ไปวางในโฟลเดอร์ `libs/` แล้ว Commit ขึ้น Git ของโปรเจกต์ปลายทางได้ทันที **ตัดปัญหาเพื่อนร่วมทีม `git clone` แล้ว Build ไม่ผ่าน หรือติดสิทธิ์ Private Repo ข้ามโปรเจกต์ 100%**
- **Database-Backed Configuration & Hot-Reload:**
  - รองรับการดึงค่า Config (`ClientId`, `ClientSecret`, `RedirectUri`, `LaserUrl`, `ApiKey`, `EncryptionKey`) จากทั้ง **`appsettings.json`** และ **Database (`DbContext` / `ISettingRepository` / `IIdentityOptionsProvider`)**
  - ใช้ระบบ **Lazy Validation** (ไม่โยน Exception ตอน Startup หากค่าใน Database ยังเป็นค่าว่าง แต่จะคืน `ErrorCode = "MISSING_CONFIGURATION"` เมื่อถูกเรียกใช้งานจริง)
- **Built-in `UseMock = true` (In-Memory Simulation ในตัว `.dll`) + Standalone `MockServer`:**
  - แม้นักพัฒนาจะมีแค่ไฟล์ `Netka.Identity.SDK.dll` ไฟล์เดียว ก็สามารถเปิด `UseMock = true` เพื่อจำลองการยืนยันตัวตนครบทั้ง **12 มาตรฐาน Use Cases** รวมถึง **Loopback Redirect** และ **การสร้างรูป QR Code** โดยไม่ต้องต่ออินเทอร์เน็ตหรือใช้ Credentials จริง

---

## 2. โครงสร้างไฟล์ทั้งหมดในโปรเจกต์ (Repository Structure)

```text
NKS-Kiosk-Identity-SDK/
├── Core/                                         # Domain Layer (Interfaces, Models, Attributes, Exceptions)
│   ├── Attributes/
│   │   └── AuditLogAttribute.cs                  # [AuditLog] Attribute สำหรับระบบ Audit Trail
│   ├── Exceptions/
│   │   └── VerificationException.cs              # Structured Exception พร้อม ErrorCode & HttpStatusCode
│   ├── Interfaces/
│   │   ├── IVerificationProvider.cs              # Base Interface สำหรับทุก Identity Provider
│   │   ├── IThaIdProvider.cs                     # Interface สำหรับ DOPA ThaID (ครบ 6 เมธอด)
│   │   ├── IDopaProvider.cs                      # Interface สำหรับ DOPA Check Card (Laser & Chip)
│   │   ├── IEncryptionService.cs                 # Interface สำหรับเข้ารหัส PII (AES-256-CBC + HMAC-SHA256)
│   │   └── IIdentityOptionsProvider.cs           # Interface สำหรับดึง Config จาก Database แบบ Hot-Reload
│   └── Models/
│       ├── IdentityVerificationOptions.cs        # Options Models (IdentityVerificationOptions, ThaIdOptions, DopaOptions)
│       ├── VerificationRequest.cs                # DTO สำหรับส่งคำขอยืนยันตัวตน
│       ├── VerificationResult.cs                 # DTO มาตรฐานสำหรับผลลัพธ์จากทุก Provider
│       └── VerificationError.cs                  # DTO สำหรับรายละเอียดข้อผิดพลาด
│
├── Application/                                  # DI Registration & Built-in Resilience Pipeline
│   └── DependencyInjection.cs                    # AddIdentityVerification() ครบ 4 Overloads + IdentityResilienceHandler
│
├── Infrastructure/                               # Providers, Security & Helpers
│   ├── Helpers/
│   │   └── ProviderHelper.cs                     # Modulo 11 Checksum, JSON Parser, Masked PII Logger
│   ├── Providers/
│   │   ├── ThaIdProvider.cs                      # DOPA ThaID Adapter + Built-in UseMock Mode
│   │   └── DopaProvider.cs                       # DOPA Check Card Adapter + Built-in UseMock Mode
│   └── Security/
│       ├── AesEncryptionService.cs               # AES-256-CBC + HMAC-SHA256 Encryption Service
│       └── PkceHelper.cs                         # PKCE (RFC 7636) Code Verifier & S256 Challenge Generator
│
├── MockServer/                                   # Standalone Sandbox Mock Server (http://localhost:5199)
│   ├── Netka.Identity.MockServer.csproj          # ASP.NET Core Minimal API จำลอง ThaID & DOPA เสมือนจริง
│   └── Program.cs                                # หน้าเว็บจำลอง QR/Consent + 12 Use Cases Endpoints
│
├── Tests/                                        # Unit Tests (xUnit - 43 Tests x 3 Runtimes = 129 Runs)
│   ├── Netka.Identity.SDK.Tests.csproj           # Multi-Target: net5.0;net8.0;net10.0
│   ├── MockHttpMessageHandler.cs                 # In-Memory HTTP Mock Handler
│   ├── ThaIdProviderTests.cs                     # ทดสอบ ThaID ครบทุก Use Case + Redirect + UseMock
│   ├── DopaProviderTests.cs                      # ทดสอบ DOPA ครบทุก Use Case + UseMock
│   ├── DependencyInjectionTests.cs               # ทดสอบ DI ทั้ง 4 รูปแบบ + Database Hot-Reload + Lazy Validation
│   ├── SecurityTests.cs                          # ทดสอบ AES-256-CBC + HMAC-SHA256 & PKCE
│   ├── ProviderHelperTests.cs                    # ทดสอบตรวจเลขบัตร 13 หลัก (Modulo 11) & JSON Parser
│   └── RuntimeCompatibilityTests.cs              # ทดสอบความเข้ากันได้บน .NET 5.0, .NET 8.0, .NET 10.0
│
├── .claude/skills/                               # AI Agent Skills สำหรับ Claude Code / Antigravity
│   ├── integrate-netka-identity/SKILL.md         # Skill สำหรับติดตั้ง SDK เข้าโปรเจกต์ปลายทางอัตโนมัติ
│   └── add-identity-provider/SKILL.md            # Skill สำหรับสร้าง Provider ใหม่เพิ่มใน SDK
│
├── docs/                                         # เอกสารโครงการและรายงานส่งมอบ
│   ├── HANDOVER_GUIDE.md                         # เอกสารส่งมอบระบบและคู่มือการติดตั้ง (ฉบับนี้)
│   ├── UNIT_TEST_REPORT.md                       # รายงานผลการทดสอบ Unit Tests (43 Test Cases / 129 Executions)
│   ├── USE_CASE_SPECIFICATION.md                 # รายงาน Use Cases ทั้ง 12 กรณี + Flow Redirect & QR Code
│   ├── SECURITY_AND_ARCHITECTURE.md              # สถาปัตยกรรม ความปลอดภัย PDPA และตาราง Error Codes
│   └── providers/                                # เอกสารอ้างอิงรายผู้ให้บริการ (ThaID & DOPA)
│
├── install-global-skills.ps1                     # สคริปต์ติดตั้ง Global Skills ไปยัง ~/.claude/skills/
├── Netka.Identity.SDK.csproj                     # ไฟล์โปรเจกต์หลักของ SDK (net5.0;net8.0;net10.0)
└── Netka.Identity.SDK.sln                        # Solution File
```

---

## 3. วิธีการนำ SDK ไปติดตั้งในโปรเจกต์ปลายทาง (Single-DLL Integration)

เพื่อป้องกันปัญหา `git clone` ข้าม Repo หรือปัญหาสิทธิ์ Private Repo แนะนำให้นำเฉพาะไฟล์ `.dll` ไปวางในโฟลเดอร์ `libs/` ของโปรเจกต์ปลายทางตามขั้นตอนดังนี้:

### ขั้นตอนที่ 1: Build ไฟล์ Release `.dll` จากโปรเจกต์ SDK
```powershell
dotnet build "Netka.Identity.SDK.csproj" -c Release
```
ไฟล์ที่ได้จะอยู่ใน:
- `bin/Release/net5.0/Netka.Identity.SDK.dll` (สำหรับโปรเจกต์ .NET 5.0)
- `bin/Release/net8.0/Netka.Identity.SDK.dll` (สำหรับโปรเจกต์ .NET 6.0 / 7.0 / 8.0)
- `bin/Release/net10.0/Netka.Identity.SDK.dll` (สำหรับโปรเจกต์ .NET 9.0 / 10.0)

### ขั้นตอนที่ 2: คัดลอกไฟล์ไปไว้ที่ `libs/` ของโปรเจกต์ปลายทาง
คัดลอกไฟล์ `Netka.Identity.SDK.dll` และ `Netka.Identity.SDK.xml` ไปไว้ที่ `<โปรเจกต์ปลายทาง>/libs/`

### ขั้นตอนที่ 3: อ้างอิงในไฟล์ `.csproj` ของโปรเจกต์ปลายทาง
```xml
<ItemGroup>
  <Reference Include="Netka.Identity.SDK">
    <HintPath>libs\Netka.Identity.SDK.dll</HintPath>
  </Reference>
</ItemGroup>
```

---

## 4. คู่มือการลงทะเบียน Dependency Injection ทั้ง 4 รูปแบบ

ใน `Program.cs` (หรือ `Startup.cs`) ของโปรเจกต์ปลายทาง สามารถเลือกวิธีตั้งค่าได้ตามสถาปัตยกรรมของโปรเจกต์นั้นๆ:

### รูปแบบที่ 1: ดึงค่าจาก `appsettings.json` (`IConfiguration`)
```csharp
using Netka.Identity.Application;

builder.Services.AddIdentityVerification(builder.Configuration);
```

### รูปแบบที่ 2: ดึงค่าจาก Database ผ่าน `IServiceProvider` (รองรับ Scoped `DbContext` / Repository อัตโนมัติ)
```csharp
using Netka.Identity.Application;

builder.Services.AddIdentityVerification((sp, options) =>
{
    // SDK สร้าง Scope ให้แล้ว สามารถดึง Scoped DbContext หรือ Repository ได้ทันที
    var settingRepo = sp.GetRequiredService<ISystemSettingRepository>();

    options.EncryptionKey = settingRepo.GetValue("Identity:EncryptionKey");
    options.UseMock = settingRepo.GetBool("Identity:UseMock"); // เปิด/ปิดโหมดจำลองจาก DB ได้ทันที

    options.ThaId.ClientId = settingRepo.GetValue("ThaID:ClientId");
    options.ThaId.ClientSecret = settingRepo.GetValue("ThaID:ClientSecret");
    options.ThaId.RedirectUri = settingRepo.GetValue("ThaID:RedirectUri");
    options.ThaId.Scope = "openid pid title title_en name name_en given_name family_name gender birthdate address house_address smartcard_code date_of_issuance date_of_expiry ial";

    options.Dopa.LaserUrl = settingRepo.GetValue("Dopa:LaserUrl");
    options.Dopa.ChipUrl = settingRepo.GetValue("Dopa:ChipUrl");
    options.Dopa.ApiKey = settingRepo.GetValue("Dopa:ApiKey");
});
```

### รูปแบบที่ 3: ดึงค่าจาก Database แบบ Hot-Reload ทุกครั้งที่เรียกใช้ (`IIdentityOptionsProvider`)
เหมาะสำหรับระบบที่มีหน้า Admin Backoffice ให้แก้ไข `ClientId`, `ClientSecret`, หรือสลับ `UseMock` ในฐานข้อมูลแล้วต้องการให้มีผลทันทีโดยไม่ต้อง Restart IIS/Docker:
```csharp
using Netka.Identity.Core.Interfaces;
using Netka.Identity.Core.Models;

public class DbIdentityOptionsProvider : IIdentityOptionsProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    public DbIdentityOptionsProvider(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public IdentityVerificationOptions GetOptions()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // ดึงค่าล่าสุดจากตาราง Settings ใน Database
        return new IdentityVerificationOptions
        {
            UseMock = db.Settings.FirstOrDefault(x => x.Key == "Identity:UseMock")?.Value == "true",
            EncryptionKey = db.Settings.FirstOrDefault(x => x.Key == "Identity:EncryptionKey")?.Value
        };
    }
}

// ลงทะเบียนใน Program.cs:
builder.Services.AddIdentityVerification<DbIdentityOptionsProvider>();
```

### รูปแบบที่ 4: กำหนดค่าผ่าน `Action<IdentityVerificationOptions>` โดยตรง
```csharp
builder.Services.AddIdentityVerification(options =>
{
    options.UseMock = true;
    options.ThaId.RedirectUri = "https://localhost:5001/api/auth/thaid/callback";
});
```

---

## 5. การใช้งาน AI Skills อัตโนมัติ (Claude Code / Antigravity)

ในโปรเจกต์นี้มี AI Skills มาตรฐานอยู่ในโฟลเดอร์ `.claude/skills/` ซึ่งสามารถซิงค์ไปยังระดับ Global ของเครื่องนักพัฒนาด้วยคำสั่ง:
```powershell
.\install-global-skills.ps1
```
1. **`/integrate-netka-identity`** — สั่งให้ AI วิเคราะห์โปรเจกต์ปลายทาง, Build และคัดลอก `Netka.Identity.SDK.dll` ไปยัง `libs/`, ผูก DI (รองรับทั้ง Database และ `appsettings.json`), และเชื่อมต่อเข้ากับ Controller/Service ของโปรเจกต์ปลายทางอัตโนมัติ
2. **`/add-identity-provider`** — สั่งให้ AI สร้าง Identity Provider ใหม่เพิ่มเข้าไปใน SDK ตามมาตรฐาน Clean Architecture + Tri-Log Strategy
