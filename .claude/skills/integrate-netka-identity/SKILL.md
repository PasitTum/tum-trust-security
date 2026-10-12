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
- **กฎความปลอดภัย:** ห้ามแก้ไขหรือใส่โค้ด Mock เข้าไปในโปรเจกต์ `Netka.Identity.SDK` เด็ดขาด เพื่อป้องกันความเสี่ยง Auth Bypass บน Production
- หากผู้ใช้ต้องการทดสอบระบบยืนยันตัวตนบนเครื่องนักพัฒนา (ขณะยังไม่มี `ClientId` จริงของ ThaID หรือไม่ได้ต่อ VPN เข้า DOPA Gateway) ให้สร้างคลาส Mock ไว้**เฉพาะภายในโปรเจกต์ปลายทาง** ดังนี้:
  1. สร้างคลาส `MockThaIdProvider : IThaIdProvider` และ/หรือ `MockDopaProvider : IDopaProvider` ในโปรเจกต์ปลายทาง
  2. อ่านรายชื่อ Key ขาออกจาก `<SDK_ROOT>/docs/providers/<provider>.md` เพื่อจำลองค่าลงใน `VerificationResult.Data` ให้ตรงกับของจริง 100% (หากเข้าถึงไฟล์ไม่ได้ ให้ใช้ชุด Key มาตรฐานด้านล่างนี้):
     - **ThaID Keys (20 Official Scopes + Tokens):** `access_token`, `refresh_token`, `id_token`, `token_type` (`"Bearer"`), `expires_in` (`3600`), `scope`, `sub`, `pid` (`"1234567890121"`), `title` (`"นาย"`), `title_en` (`"Mr."`), `given_name` (`"สมชาย"`), `middle_name` (`""`), `family_name` (`"ใจดี"`), `name` (`"นาย สมชาย ใจดี"`), `given_name_en` (`"Somchai"`), `middle_name_en` (`""`), `family_name_en` (`"Jaidee"`), `name_en` (`"Mr. Somchai Jaidee"`), `gender` (`"male"`), `birthdate` (`"1990-01-31"`), `address`, `house_address`, `smartcard_code` (`"ME0123456789"`), `date_of_issuance`, `date_of_expiry`, `ial` (`2.3`), `active` (`true`)
     - **DOPA Keys:** `IsError` (`false`), `ErrorMessage` (`""`), `Code` (`0`), `StCode` (`"00"`), `Desc` (`"สถานะปกติ"`), `PID` (`"1234567890121"`)
  3. รองรับ Trigger จำลองเคส Error (เช่น หากส่ง `code == "mock_error"` หรือ `laserId == "MOCK_ERROR"` ให้คืนค่า `ProviderHelper.CreateFailureResult(...)`) เพื่อให้ทีมหน้าบ้านทดสอบหน้าจอแจ้งเตือนบนตู้ Kiosk ได้ครบทุกกรณี
  4. **ล็อกเงื่อนไข DI ใน `Program.cs` / `Startup.cs` ของโปรเจกต์ปลายทางให้ทำงานเฉพาะ `IsDevelopment()` เท่านั้น:**
     ```csharp
     services.AddIdentityVerification(configuration);

     if (env.IsDevelopment() && configuration.GetValue<bool>("IdentityVerification:UseMock"))
     {
         services.AddTransient<IThaIdProvider, MockThaIdProvider>();
         services.AddTransient<IDopaProvider, MockDopaProvider>();
     }
     ```

---

## Step 7: การตรวจสอบและ Build ยืนยันผล
1. รัน `dotnet build` ที่โปรเจกต์ปลายทาง ยืนยันว่า Build ผ่านเรียบร้อยโดยไม่มี Error
2. ตรวจสอบ `git diff` ของไฟล์ `.csproj` ในโปรเจกต์ปลายทางว่าใช้ **Relative Path** สำหรับ `<ProjectReference>` และไม่มีการแก้ไข `<PackageReference>` เดิม
