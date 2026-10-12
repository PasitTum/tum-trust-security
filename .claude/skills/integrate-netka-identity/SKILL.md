---
name: integrate-netka-identity
description: ติดตั้งและเชื่อมต่อ Netka.Identity.SDK (ThaID, DOPA และ Identity Providers อื่นๆ) เข้าสู่โปรเจกต์ปลายทาง (.NET 5.0, .NET 8.0, .NET 10.0) โดยห้ามแก้ไขหรืออัปเกรดเวอร์ชัน NuGet Package เดิมของโปรเจกต์ปลายทางเด็ดขาด
---

# Skill: Integrate Netka.Identity.SDK (`integrate-netka-identity`)

คุณคือผู้เชี่ยวชาญการเชื่อมต่อระบบยืนยันตัวตนด้วย **Netka.Identity.SDK**
- **ตำแหน่งของ SDK ต้นทาง:** `d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK\Netka.Identity.SDK.csproj`
- **เอกสารอ้างอิงของแต่ละ Provider:** `d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK\docs\providers\` (อ่านไฟล์ในโฟลเดอร์นี้เสมอเพื่อดู Config, เมธอดของ Interface, และชื่อ Key ใน `VerificationResult.Data`)

---

## 🚨 กฎเหล็กสำคัญที่สุด: ห้ามแก้ไข NuGet Package ของโปรเจกต์ปลายทางเด็ดขาด (Zero Host Mutation Rule)

1. **ห้ามแตะต้อง `<PackageReference>` เดิมของโปรเจกต์ปลายทาง:**
   - ห้ามอัปเกรด, ดาวน์เกรด, หรือลบ NuGet Package เดิมที่มีอยู่ในโปรเจกต์ปลายทาง (Host Project) โดยเด็ดขาด เพราะจะกระทบระบบเดิมของโปรเจกต์ปลายทาง
2. **SDK ต้องเป็นฝ่ายปรับตัวตามโปรเจกต์ปลายทางเสมอ:**
   - `Netka.Identity.SDK` รองรับ `net5.0`, `net8.0`, และ `net10.0` โดยใช้ Base Version (`5.0.0`, `8.0.0`, `10.0.0`) เพื่อให้เข้ากับโปรเจกต์ปลายทางได้ทุกเวอร์ชันย่อย
   - หากเกิดกรณีที่โปรเจกต์ปลายทางใช้ TargetFramework อื่น (เช่น `net6.0` หรือ `net7.0`) หรือเกิด Package Conflict ใดๆ **ห้ามแก้ที่โปรเจกต์ปลายทาง** ให้แก้ไขหรือเพิ่ม TargetFramework ที่ไฟล์ `d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK\Netka.Identity.SDK.csproj` เท่านั้น

---

## ขั้นตอนการติดตั้งและเชื่อมต่อ (Step-by-Step Workflow)

### Step 1: สำรวจโปรเจกต์ปลายทาง และอ่านเอกสาร Provider (Pre-flight Inspection)
1. ค้นหาไฟล์ `.csproj` ของโปรเจกต์ปลายทางที่จะติดตั้ง แล้วตรวจสอบ:
   - `<TargetFramework>` คือเวอร์ชันใด (`net5.0`, `net8.0`, `net10.0`)
   - รูปแบบการเขียน Startup ของโปรเจกต์ (`Startup.cs` แบบ .NET 5 หรือ Minimal Hosting `Program.cs` แบบ .NET 8/10)
2. ตรวจสอบว่าผู้ใช้ต้องการเชื่อมโยง Provider ตัวใด (เช่น `ThaID`, `DOPA` หรือทั้งคู่)
3. **อ่านเอกสาร Spec ของ Provider จากโฟลเดอร์ SDK:**
   - อ่าน `d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK\docs\providers\README.md`
   - อ่านไฟล์ของ Provider ที่ต้องใช้ เช่น `docs\providers\thaid.md` และ/หรือ `docs\providers\dopa.md` เพื่อให้ทราบโครงสร้าง `appsettings.json`, เมธอดของ Interface และชื่อ Key ขาออกใน `VerificationResult.Data`

### Step 2: ผูก Reference เข้ากับโปรเจกต์ปลายทาง
- เพิ่ม `<ProjectReference>` ในไฟล์ `.csproj` ของโปรเจกต์ปลายทาง (โดยห้ามแก้ไข `<PackageReference>` เดิม):
  ```xml
  <ProjectReference Include="d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK\Netka.Identity.SDK.csproj" />
  ```
- ทดสอบรัน `dotnet restore` ที่โปรเจกต์ปลายทางเพื่อยืนยันว่าไม่มี Error `NU1605` หรือ `NU1202`

### Step 3: ตั้งค่า Configuration ใน `appsettings.json`
เพิ่ม Section `"IdentityVerification"` ลงใน `appsettings.json` ของโปรเจกต์ปลายทาง:
- **`EncryptionKey` (บังคับต้องมีเสมอ):** Base64 string ของคีย์ขนาด 32 bytes (256-bit) สำหรับ `AesEncryptionService` (หากในโปรเจกต์ปลายทางยังไม่มี สามารถสร้างคีย์จำลอง Base64 32-byte ใส่ไว้ใน `appsettings.Development.json` เช่น `"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="`)
- ใส่ Config ของ Provider ตามที่ระบุใน `docs/providers/<provider>.md`:

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

### Step 4: ลงทะเบียน Dependency Injection (`Startup.cs` หรือ `Program.cs`)
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

### Step 5: สร้าง Service หรือ Controller เชื่อมต่อตามความต้องการของโปรเจกต์ปลายทาง

#### 5.1 กรณีเชื่อมต่อ ThaID (`IThaIdProvider` + `PkceHelper`)
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

#### 5.2 กรณีเชื่อมต่อ DOPA (`IDopaProvider`)
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

#### 5.3 กรณีต้องการดึง Provider จากรายการทั้งหมด (`IEnumerable<IVerificationProvider>`)
- สามารถ Inject `IEnumerable<IVerificationProvider>` แล้วค้นหาด้วย `providers.FirstOrDefault(p => string.Equals(p.ProviderName, "ThaID", StringComparison.OrdinalIgnoreCase))`

---

## Step 6: การตรวจสอบและ Build ยืนยันผล
1. รัน `dotnet build` ที่โปรเจกต์ปลายทาง ยืนยันว่า Build ผ่านเรียบร้อยโดยไม่มี Error
2. ตรวจสอบ `git diff` ของไฟล์ `.csproj` ในโปรเจกต์ปลายทางว่ามีเฉพาะบรรทัด `<ProjectReference>` ที่เพิ่มเข้าไป โดยไม่มีการแก้ไข `<PackageReference>` เดิม
