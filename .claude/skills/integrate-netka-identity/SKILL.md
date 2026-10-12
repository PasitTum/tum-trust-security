---
name: integrate-netka-identity
description: ติดตั้งและเชื่อมต่อ Netka.Identity.SDK (ThaID, DOPA และ Identity Providers อื่นๆ) เข้าสู่โปรเจกต์ปลายทาง (.NET 5.0, .NET 8.0, .NET 10.0) โดยห้ามแก้ไขหรืออัปเกรดเวอร์ชัน NuGet Package เดิมของโปรเจกต์ปลายทางเด็ดขาด
---

# Skill: Integrate Netka.Identity.SDK (`integrate-netka-identity`)

คุณคือผู้เชี่ยวชาญการเชื่อมต่อระบบยืนยันตัวตนด้วย **Netka.Identity.SDK**
- **ตำแหน่งของ SDK ต้นทาง:** `d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK\Netka.Identity.SDK.csproj`
- **เอกสารอ้างอิงของแต่ละ Provider:** `d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK\docs\providers\` (อ่านไฟล์ในโฟลเดอร์นี้เสมอเพื่อดู Config, Request Fields และ Response Keys ใน `VerificationResult.Data`)

---

## 🚨 กฎเหล็กสำคัญที่สุด: ห้ามแก้ไข NuGet Package ของโปรเจกต์ปลายทางเด็ดขาด (Zero Host Mutation Rule)

1. **ห้ามแตะต้อง `<PackageReference>` เดิมของโปรเจกต์ปลายทาง:**
   - ห้ามอัปเกรด, ดาวน์เกรด, หรือลบ NuGet Package เดิมที่มีอยู่ในโปรเจกต์ปลายทาง (Host Project) โดยเด็ดขาด เพราะอาจทำให้ระบบเดิมของโปรเจกต์ปลายทางพัง
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
   - อ่านไฟล์ของ Provider ที่ต้องใช้ เช่น `docs\providers\thaid.md` และ/หรือ `docs\providers\dopa.md` เพื่อให้ทราบชื่อ Key ใน `appsettings.json`, พารามิเตอร์ขาเข้า `VerificationRequest` และชื่อ Key ขาออกใน `VerificationResult.Data`

### Step 2: ผูก Reference เข้ากับโปรเจกต์ปลายทาง
- เพิ่ม `<ProjectReference>` ในไฟล์ `.csproj` ของโปรเจกต์ปลายทาง (โดยใช้ Relative Path หรือ Path ที่เหมาะสม และห้ามแก้ไข `<PackageReference>` เดิม):
  ```xml
  <ProjectReference Include="d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK\Netka.Identity.SDK.csproj" />
  ```
- ทดสอบรัน `dotnet restore` ที่โปรเจกต์ปลายทางเพื่อยืนยันว่าไม่มี Error `NU1605` หรือ `NU1202`

### Step 3: ตั้งค่า Configuration ใน `appsettings.json`
เพิ่ม Section `"IdentityVerification"` ลงใน `appsettings.json` (หรือ `appsettings.Development.json`) ของโปรเจกต์ปลายทาง โดยใส่เฉพาะ Provider ที่โปรเจกต์นั้นใช้งาน (อ้างอิงโครงสร้างจาก `docs/providers/<provider>.md`):

```json
{
  "IdentityVerification": {
    "EncryptionKey": "",
    "HmacKey": "",
    "Providers": {
      "ThaID": {
        "ClientId": "<YOUR_THAID_CLIENT_ID>",
        "ClientSecret": "<YOUR_THAID_CLIENT_SECRET>",
        "ApiKey": "<YOUR_THAID_API_KEY>",
        "CallbackUrl": "https://localhost:5001/api/identity/thaid/callback",
        "AuthorizeEndpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/",
        "TokenEndpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/token/",
        "RevokeEndpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/revoke/",
        "IntrospectEndpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/introspect/",
        "Scope": "pid given_name family_name birthdate",
        "TimeoutSeconds": 30
      },
      "DOPA": {
        "CheckCardEndpoint": "https://api.dopa.go.th/CheckCardStatus",
        "ApiKey": "<YOUR_DOPA_API_KEY>",
        "TimeoutSeconds": 30,
        "AdditionalHeaders": {
          "ContentType": "form"
        }
      }
    }
  }
}
```

### Step 4: ลงทะเบียน Dependency Injection (`Startup.cs` หรือ `Program.cs`)
เพิ่ม `using Netka.Identity.Infrastructure;` และเรียกใช้งาน `AddIdentityVerification`:
- **กรณีโปรเจกต์ .NET 5.0 (`Startup.cs`):**
  ```csharp
  public void ConfigureServices(IServiceCollection services)
  {
      services.AddIdentityVerification(Configuration);
  }
  ```
- **กรณีโปรเจกต์ .NET 8.0 / .NET 10.0 (`Program.cs`):**
  ```csharp
  builder.Services.AddIdentityVerification(builder.Configuration);
  ```

### Step 5: สร้าง Service หรือ Controller เชื่อมต่อตามความต้องการของโปรเจกต์ปลายทาง

#### 5.1 กรณีเชื่อมต่อ ThaID (`IThaIdProvider` + `PkceHelper`)
- Inject `Netka.Identity.Core.Interfaces.IThaIdProvider`
- **ตอนเริ่ม Login (`/thaid/login`):**
  ```csharp
  var pkce = PkceHelper.GeneratePkcePair();
  var authUrl = await _thaIdProvider.GenerateAuthorizationUrlAsync(
      state: null, // ให้ SDK สุ่ม State ให้อัตโนมัติ
      codeChallenge: pkce.CodeChallenge,
      codeChallengeMethod: pkce.CodeChallengeMethod,
      cancellationToken: ct);
  // บันทึก pkce.CodeVerifier ผูกกับ authUrl.State ลงใน Session / DistributedCache / MemoryCache (TTL 5-10 นาที)
  // แล้ว Redirect ผู้ใช้ไปที่ authUrl.Url
  ```
- **ตอนรับ Callback (`/thaid/callback`):**
  ```csharp
  // 1. ตรวจสอบและดึง codeVerifier จาก Session/Cache ด้วยค่า state (แล้วลบทิ้งทันทีเพื่อป้องกัน Replay Attack)
  // 2. แลก Code เป็นข้อมูลยืนยันตัวตน:
  var result = await _thaIdProvider.VerifyAsync(new VerificationRequest
  {
      AuthorizationCode = code,
      CodeVerifier = codeVerifier
  }, ct);

  if (!result.IsSuccess)
  {
      // จัดการ Error จาก result.ErrorCode และ result.ErrorMessage
  }
  // ดึงข้อมูลจาก result.Data (เช่น result.Data["pid"], result.Data["given_name"])
  ```

#### 5.2 กรณีเชื่อมต่อ DOPA (`IDopaProvider`)
- Inject `Netka.Identity.Core.Interfaces.IDopaProvider`
  ```csharp
  var result = await _dopaProvider.VerifyAsync(new VerificationRequest
  {
      NationalId = request.Pid,       // เลขบัตร 13 หลัก
      FirstName = request.FirstName,  // ชื่อภาษาไทย
      LastName = request.LastName,    // นามสกุลภาษาไทย
      BirthDate = request.BirthDay,   // พ.ศ. YYYYMMDD เช่น "25350131"
      LaserCode = request.LaserCode   // เลขหลังบัตร เช่น "JT0123456789"
  }, ct);
  ```

#### 5.3 กรณีต้องการสลับ Provider แบบ Dynamic (`IdentityVerifierFactory`)
- Inject `Netka.Identity.Application.IdentityVerifierFactory` แล้วเรียก `_factory.GetProvider("ThaID")` หรือ `_factory.GetProvider("DOPA")`

---

## Step 6: กฎความปลอดภัยและการ Build ยืนยันผล
1. **ห้าม Log ข้อมูลส่วนบุคคล (PII):** หากต้องเขียน Log ในโปรเจกต์ปลายทาง ให้ใช้ `DataMaskingHelper.MaskNationalId(...)`, `DataMaskingHelper.MaskLaserCode(...)`, หรือ `DataMaskingHelper.MaskFullName(...)` จาก `Netka.Identity.Core.Security` เสมอ
2. **รัน `dotnet build` ที่โปรเจกต์ปลายทาง:** ยืนยันว่าโปรเจกต์ปลายทาง Build ผ่านเรียบร้อยโดยไม่มี Error และตรวจสอบ `git diff` ของไฟล์ `.csproj` ปลายทางว่าไม่มีการแก้ไข `<PackageReference>` เดิม
