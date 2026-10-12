---
name: integrate-netka-identity
description: ติดตั้งและเชื่อมต่อ Netka.Identity.SDK (ThaID, DOPA และ Identity Providers อื่นๆ) เข้าสู่โปรเจกต์ปลายทาง (.NET 5.0, .NET 8.0, .NET 10.0) โดยค้นหา Path อัตโนมัติ ใช้ Relative Path สำหรับ ProjectReference และห้ามแก้ไขเวอร์ชัน NuGet Package เดิมของโปรเจกต์ปลายทางเด็ดขาด
---

# Skill: Integrate Netka.Identity.SDK (`integrate-netka-identity`)

คุณคือผู้เชี่ยวชาญการเชื่อมต่อระบบยืนยันตัวตนด้วย **Netka.Identity.SDK** เข้าสู่โปรเจกต์ปลายทาง (Host Project)

---

## 🚨 กฎเหล็กสำคัญที่สุด 2 ข้อ

1. **Zero Host Package Mutation Rule (ห้ามแก้ NuGet เดิมของโปรเจกต์ปลายทาง):**
   - ห้ามอัปเกรด, ดาวน์เกรด, หรือลบ `<PackageReference>` เดิมที่มีอยู่ในโปรเจกต์ปลายทางเด็ดขาด เพราะจะกระทบระบบเดิมของโปรเจกต์ปลายทาง
   - `Netka.Identity.SDK` รองรับ `net5.0`, `net8.0`, และ `net10.0` โดยใช้ Base Version (`5.0.0`, `8.0.0`, `10.0.0`) อยู่แล้ว หากเกิดปัญหาความเข้ากันได้ใดๆ ต้องปรับที่ฝั่ง SDK เท่านั้น ห้ามแก้แพ็กเกจของโปรเจกต์ปลายทาง

2. **No Hardcoded Machine Paths (ห้ามใช้ Absolute Path เฉพาะเครื่อง):**
   - ห้ามใส่ Path เฉพาะเครื่อง (เช่น `D:\Work\...` หรือ `C:\Users\...`) ลงในไฟล์ `.csproj` หรือโค้ดของโปรเจกต์ปลายทางเด็ดขาด เพราะแต่ละเครื่องและระบบ CI/CD มีโครงสร้างโฟลเดอร์ไม่เหมือนกัน
   - ต้องค้นหาตำแหน่งของ `Netka.Identity.SDK` แบบ Dynamic และใช้ **Relative Path** เสมอ

---

## ขั้นตอนการติดตั้งและเชื่อมต่อ (Step-by-Step Workflow)

### Step 1: ค้นหาตำแหน่งของ `Netka.Identity.SDK` (`<SDK_ROOT>`) แบบอัตโนมัติ
ให้ค้นหาโฟลเดอร์ของโปรเจกต์ `NKS-Kiosk-Identity-SDK` (`<SDK_ROOT>`) ตามลำดับต่อไปนี้:
1. ตรวจสอบว่าในไฟล์ `.csproj` ของโปรเจกต์ปลายทางมี `<ProjectReference>` ที่ชี้ไปยัง `Netka.Identity.SDK.csproj` อยู่แล้วหรือไม่
2. ค้นหาในโฟลเดอร์ระดับเดียวกัน (Sibling Directory) เช่น `../NKS-Kiosk-Identity-SDK/Netka.Identity.SDK.csproj` หรือ `../../NKS-Kiosk-Identity-SDK/Netka.Identity.SDK.csproj`
3. หากยังไม่พบ ให้ใช้เครื่องมือค้นหาไฟล์ (`Glob` / `Find`) ค้นหาไฟล์ชื่อ `Netka.Identity.SDK.csproj` ในโฟลเดอร์แม่ แล้วคำนวณ **Relative Path** จากโฟลเดอร์ของ `.csproj` ปลายทางไปยัง `Netka.Identity.SDK.csproj` (หรือถาม Path จากผู้ใช้หากอยู่นอก Workspace)

### Step 2: สำรวจโปรเจกต์ปลายทาง และอ่านเอกสาร Provider จาก `<SDK_ROOT>/docs/providers/`
1. ตรวจสอบไฟล์ `.csproj` ของโปรเจกต์ปลายทาง:
   - `<TargetFramework>` คือเวอร์ชันใด (`net5.0`, `net8.0`, `net10.0`)
   - รูปแบบการเขียน Startup (`Startup.cs` แบบ .NET 5 หรือ `Program.cs` แบบ .NET 8/10)
2. **อ่านเอกสาร Spec ของ Provider จาก `<SDK_ROOT>/docs/providers/`:**
   - อ่าน `<SDK_ROOT>/docs/providers/README.md` เพื่อดูรายชื่อ Providers ทั้งหมดที่มีใน SDK
   - อ่านไฟล์ `<SDK_ROOT>/docs/providers/thaid.md` และ/หรือ `<SDK_ROOT>/docs/providers/dopa.md` (หรือไฟล์ของ Provider อื่นๆ ที่ต้องการเชื่อม) เพื่อดู Config, เมธอดของ Interface และรายชื่อ Key ใน `VerificationResult.Data`

### Step 3: ผูก Reference ด้วย Relative Path
- เพิ่ม `<ProjectReference>` ในไฟล์ `.csproj` ของโปรเจกต์ปลายทางโดยใช้ **Relative Path** เท่านั้น (เช่น เมื่อวางอยู่ในโฟลเดอร์แม่เดียวกัน):
  ```xml
  <ProjectReference Include="..\NKS-Kiosk-Identity-SDK\Netka.Identity.SDK.csproj" />
  ```
- ทดสอบรัน `dotnet restore` ที่โปรเจกต์ปลายทางเพื่อยืนยันว่าไม่มี Error `NU1605` หรือ `NU1202`

### Step 4: ตั้งค่า Configuration ใน `appsettings.json`
เพิ่ม Section `"IdentityVerification"` ลงใน `appsettings.json` ของโปรเจกต์ปลายทาง:
- **`EncryptionKey` (บังคับต้องมีเสมอ):** Base64 string ของคีย์ขนาด 32 bytes (256-bit) สำหรับ `AesEncryptionService` (ใน `appsettings.Development.json` สามารถใช้คีย์จำลอง 32-byte เช่น `"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="`)

```json
{
  "IdentityVerification": {
    "EncryptionKey": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
    "ThaId": {
      "ClientId": "<YOUR_THAID_CLIENT_ID>",
      "ClientSecret": "<YOUR_THAID_CLIENT_SECRET>",
      "AuthorizationUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/",
      "TokenUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/token/",
      "InspectUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/introspect/",
      "RevokeUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/revoke/",
      "UserInfoUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/userinfo/",
      "RedirectUri": "https://localhost:5001/api/identity/thaid/callback",
      "Scope": "openid pid profile",
      "AllowUntrustedCertificates": false
    },
    "Dopa": {
      "VerificationUrl": "https://dopa-gateway.your-agency.local/api/v1/card/verify",
      "LaserVerificationUrl": "https://dopa-gateway.your-agency.local/api/v1/card/verify-laser",
      "ChipVerificationUrl": "https://dopa-gateway.your-agency.local/api/v1/card/verify-chip",
      "ApiKey": "<YOUR_DOPA_API_KEY>",
      "AllowUntrustedCertificates": false
    }
  }
}
```

### Step 5: ลงทะเบียน Dependency Injection (`Startup.cs` หรือ `Program.cs`)
เพิ่ม `using Netka.Identity.Application;` และเรียกใช้งาน `AddIdentityVerification`:
- **กรณีโปรเจกต์ .NET 5.0 (`Startup.cs`):**
  ```csharp
  using Netka.Identity.Application;

  public void ConfigureServices(IServiceCollection services)
  {
      services.AddIdentityVerification(Configuration);
  }
  ```
- **กรณีโปรเจกต์ .NET 8.0 / .NET 10.0 (`Program.cs`):**
  ```csharp
  using Netka.Identity.Application;

  builder.Services.AddIdentityVerification(builder.Configuration);
  ```

### Step 6: สร้าง Service หรือ Controller เชื่อมต่อตามความต้องการของโปรเจกต์ปลายทาง

#### 6.1 กรณีเชื่อมต่อ ThaID (`IThaIdProvider` + `PkceHelper`)
- Namespaces ที่ใช้:
  ```csharp
  using Netka.Identity.Core.Interfaces;
  using Netka.Identity.Core.Models;
  using Netka.Identity.Infrastructure.Helpers;
  using Netka.Identity.Infrastructure.Security;
  ```
- **ตอนเริ่ม Login (`/thaid/login`):**
  ```csharp
  string codeVerifier = PkceHelper.GenerateCodeVerifier();
  string codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);
  string state = Guid.NewGuid().ToString("N");

  string authUrl = _thaIdProvider.GetAuthorizationUrl(
      redirectUri: "https://localhost:5001/api/identity/thaid/callback",
      state: state,
      codeChallenge: codeChallenge,
      codeChallengeMethod: "S256");

  // บันทึก codeVerifier ผูกกับ state ลงใน Session / DistributedCache / MemoryCache (TTL 5-10 นาที)
  // แล้ว Redirect ผู้ใช้ไปที่ authUrl
  ```
- **ตอนรับ Callback (`/thaid/callback`):**
  ```csharp
  // 1. ตรวจสอบและดึง codeVerifier จาก Session/Cache ด้วยค่า state (แล้วลบทิ้งทันทีเพื่อป้องกัน Replay Attack)
  // 2. แลก Code เป็น Token และข้อมูลยืนยันตัวตน:
  VerificationResult result = await _thaIdProvider.ExchangeCodeAsync(
      authorizationCode: code,
      redirectUri: "https://localhost:5001/api/identity/thaid/callback",
      codeVerifier: codeVerifier,
      cancellationToken: ct);

  if (!result.IsSuccess)
  {
      // จัดการ Error จาก result.ErrorCode, result.Message, และ result.Error?.HttpStatusCode
  }

  // ดึงข้อมูลจาก result.Data (Case-Insensitive OrdinalIgnoreCase หรือใช้ .GetStringValue)
  string? pid = result.Data.GetStringValue("pid");
  string? accessToken = result.Data.GetStringValue("access_token");
  ```

#### 6.2 กรณีเชื่อมต่อ DOPA (`IDopaProvider`)
- Inject `Netka.Identity.Core.Interfaces.IDopaProvider`
- **ตรวจสอบด้วยข้อมูลหน้าบัตร + เลขหลังบัตร (Laser ID):**
  ```csharp
  VerificationResult result = await _dopaProvider.VerifyByLaserAsync(
      pid: request.Pid,           // เลขบัตร 13 หลัก (SDK เช็ค Modulo 11 ให้ในตัว)
      firstName: request.FirstName,
      lastName: request.LastName,
      birthDate: request.BirthDay, // พ.ศ. YYYYMMDD เช่น "25350131"
      laserId: request.LaserCode,  // เลขหลังบัตร เช่น "ME0123456789"
      cancellationToken: ct);
  ```
- **ตรวจสอบด้วยเลขบัตร + เลขชิปสมาร์ทการ์ด (Smart Card Chip):**
  ```csharp
  VerificationResult result = await _dopaProvider.VerifyByChipAsync(
      pid: request.Pid,
      chipNo: request.ChipNo,
      bp1No: request.Bp1No,
      cancellationToken: ct);
  ```

#### 6.3 กรณีต้องการดึง Provider จากรายการทั้งหมด (`IEnumerable<IVerificationProvider>`)
- สามารถ Inject `IEnumerable<IVerificationProvider>` แล้วค้นหาด้วย `providers.FirstOrDefault(p => string.Equals(p.ProviderName, "ThaID", StringComparison.OrdinalIgnoreCase))`

#### 6.4 กรณีผู้ใช้ต้องการทำ Mock Data / Sandbox สำหรับทดสอบในโปรเจกต์ปลายทาง (Optional)

##### 🔒 กฎเหล็กป้องกันโค้ด Mock หลุดขึ้น Production (Compile-Time `#if DEBUG` Guard)
เพื่อป้องกันกรณีที่นักพัฒนาลืมลบไฟล์ Mock หรือเผลอตั้งค่า `"UseMock": true` ค้างไว้ใน `appsettings.json` ตอน Deploy ขึ้น Production **บังคับให้ใช้ `#if DEBUG` ล็อกไว้ 2 ชั้นเสมอ** (เมื่อสั่ง `dotnet publish -c Release` คอมไพเลอร์ C# จะตัดโค้ด Mock ทิ้งออกจากไฟล์ `.dll` ไปเลย 100%):
1. **ชั้นที่ 1 — ครอบทั้งไฟล์ `MockThaIdProvider.cs` และ `MockDopaProvider.cs` ด้วย `#if DEBUG ... #endif`:**
   ```csharp
   #if DEBUG
   namespace YourHostProject.Infrastructure.Mocks;

   public sealed class MockThaIdProvider : IThaIdProvider
   {
       // ... โค้ด Mock ทั้งหมดอยู่ภายใน #if DEBUG เท่านั้น ...
   }
   #endif
   ```
   *(ผลลัพธ์: หากใครเผลอเรียกใช้ `MockThaIdProvider` นอกบล็อก `#if DEBUG` ตอนสั่ง Build แบบ `-c Release` จะเกิด Compile Error ทันที ไม่มีทางหลุดขึ้น Production)*
2. **ชั้นที่ 2 — ครอบจุดลงทะเบียน DI ใน `Program.cs` / `Startup.cs` ด้วย `#if DEBUG` คู่กับ `env.IsDevelopment()`:**
   ```csharp
   services.AddIdentityVerification(configuration);

   #if DEBUG
   if (env.IsDevelopment() && configuration.GetValue<bool>("IdentityVerification:UseMock"))
   {
       services.AddTransient<IThaIdProvider, MockThaIdProvider>();
       services.AddTransient<IDopaProvider, MockDopaProvider>();
   }
   #endif
   ```

##### 📋 รายการ Use Cases ที่ต้องสร้างให้ครบในคลาส Mock (รวม 12 Use Cases)

1. **ฝั่ง `MockThaIdProvider : IThaIdProvider` (6 Use Cases):**
   - **UC-THAID-01 (Happy Path — ยืนยันตัวตนสำเร็จ):** ส่ง `code` ปกติ -> คืนค่า `IsSuccess = true` พร้อมข้อมูลครบ 20 Scopes ของ ThaID ใน `result.Data`:
     `access_token`, `refresh_token`, `id_token`, `token_type` (`"Bearer"`), `expires_in` (`3600L`), `scope`, `sub`, `pid` (`"1234567890121"`), `title` (`"นาย"`), `title_en` (`"Mr."`), `given_name` (`"สมชาย"`), `middle_name` (`""`), `family_name` (`"ใจดี"`), `name` (`"นาย สมชาย ใจดี"`), `given_name_en` (`"Somchai"`), `middle_name_en` (`""`), `family_name_en` (`"Jaidee"`), `name_en` (`"Mr. Somchai Jaidee"`), `gender` (`"male"`), `birthdate` (`"1990-01-31"`), `address`, `house_address`, `smartcard_code` (`"ME0123456789"`), `date_of_issuance` (`"2022-01-31"`), `date_of_expiry` (`"2030-01-30"`), `ial` (`2.3`)
   - **UC-THAID-02 (CSRF State Mismatch):** ใน `VerifyAsync` หาก `request.State != expected_state` -> คืนค่า `ProviderHelper.CreateFailureResult("INVALID_STATE", "OAuth state validation failed.")`
   - **UC-THAID-03 (Code หมดอายุ / ไม่ถูกต้อง):** ส่ง `code == "mock_error"` หรือ `"mock_expired_code"` -> คืนค่า `ProviderHelper.CreateFailureResult("TOKEN_EXCHANGE_FAILED", "Mock invalid_grant", 400)`
   - **UC-THAID-04 (Network Timeout / ระบบ ThaID ล่ม):** ส่ง `code == "mock_timeout"` -> คืนค่า `ProviderHelper.CreateFailureResult("NETWORK_ERROR", "Simulated ThaID network timeout.")`
   - **UC-THAID-05 (Inspect Token Active vs Expired):** ใน `InspectTokenAsync` ถ้าส่ง `accessToken == "mock_inactive_token"` ให้คืน `Data["active"] = false` นอกนั้นคืน `Data["active"] = true`
   - **UC-THAID-06 (Refresh & Revoke Token):** ใน `RefreshTokenAsync` ถ้าส่ง `"mock_expired_refresh"` ให้คืน `TOKEN_REFRESH_FAILED` (400) นอกนั้นคืน Token ใหม่ และใน `RevokeTokenAsync` คืน `Data["message"] = "revoked"`

2. **ฝั่ง `MockDopaProvider : IDopaProvider` (6 Use Cases):**
   - **UC-DOPA-01 (Happy Path — สถานะบัตรปกติ ทั้ง Laser และ Chip):** ส่งเลขบัตร 13 หลักที่ถูก Checksum + ข้อมูลปกติ -> คืนค่า `IsSuccess = true`, `Data`: `IsError = false`, `ErrorMessage = ""`, `Code = 0L`, `StCode = "00"`, `Desc = "สถานะปกติ"`, `PID = pid`
   - **UC-DOPA-02 (เลขบัตรผิดรูปแบบ / ไม่ผ่าน Modulo 11):** เรียก `if (!pid.IsValidThaiNationalId())` -> คืนค่า `ProviderHelper.CreateFailureResult("INVALID_PID_FORMAT", "...")`
   - **UC-DOPA-03 (พารามิเตอร์บังคับไม่ครบ):** ตรวจค่าว่างของ `firstName`, `lastName`, `birthDate`, `laserId` หรือ `chipNo` -> คืนค่า `ProviderHelper.CreateFailureResult("MISSING_PARAMETERS", "...")`
   - **UC-DOPA-04 (สถานะบัตรไม่ปกติ / บัตรหมดอายุ / เลขหลังบัตรไม่ตรง):** ส่ง `laserId == "MOCK_REJECTED"` หรือ `"MOCK_ERROR"` (หรือ `chipNo == "MOCK_REJECTED"`) -> คืนค่า `CARD_VERIFICATION_REJECTED` พร้อม `Data["IsError"] = true`, `Data["Code"] = 1L`, `Data["StCode"] = "01"`, `Data["Desc"] = "สถานะบัตรไม่ปกติ หรือข้อมูลไม่ตรงกับฐานข้อมูลกรมการปกครอง"`
   - **UC-DOPA-05 (DOPA Gateway ตอบ HTTP 503 / 401):** ส่ง `laserId == "MOCK_API_FAIL"` (หรือ `chipNo == "MOCK_API_FAIL"`) -> คืนค่า `ProviderHelper.CreateFailureResult("DOPA_API_FAILED", "Simulated DOPA HTTP 503", 503)`
   - **UC-DOPA-06 (สายเชื่อมต่อกรมการปกครองหลุด / Network Timeout):** ส่ง `laserId == "MOCK_TIMEOUT"` (หรือ `chipNo == "MOCK_TIMEOUT"`) -> คืนค่า `ProviderHelper.CreateFailureResult("NETWORK_ERROR", "Simulated DOPA network timeout.")`

---

## Step 7: การตรวจสอบและ Build ยืนยันผล
1. รัน `dotnet build` (และ `dotnet build -c Release` หากมีการสร้างคลาส Mock เพื่อให้มั่นใจว่าบล็อก `#if DEBUG` ทำงานถูกต้องและไม่ทำให้ Release Build พัง)
2. ตรวจสอบ `git diff` ของไฟล์ `.csproj` ในโปรเจกต์ปลายทางว่าใช้ **Relative Path** สำหรับ `<ProjectReference>` และไม่มีการแก้ไข `<PackageReference>` เดิม

