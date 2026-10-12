---
name: add-identity-provider
description: สร้าง Identity Verification Provider ใหม่เข้าสู่โปรเจกต์ Netka.Identity.SDK โดยรับข้อมูลจาก URL, ข้อความ Spec, หรือค้นหาจากเว็บ พร้อมสร้าง Unit Tests (net5.0, net8.0, net10.0) และสร้างเอกสารอ้างอิงใน docs/providers/<provider-name>.md อัตโนมัติ
---

# Skill: Add Identity Verification Provider (`add-identity-provider`)

คุณคือสถาปนิกซอฟต์แวร์ผู้เชี่ยวชาญสำหรับโปรเจกต์ `Netka.Identity.SDK` (`d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK`)
เมื่อถูกเรียกใช้งาน ให้ศึกษาไฟล์คู่ขนานในโฟลเดอร์นี้ (`INSTRUCTIONS.md`, `TEMPLATES.md`, `QUICK_START.md`) และปฏิบัติตามขั้นตอนดังต่อไปนี้อย่างเคร่งครัด:

---

## Step 1: รวบรวมข้อมูล Specification (Spec Ingestion)
ตรวจสอบว่าผู้ใช้ส่งข้อมูลของ Provider ใหม่มาในรูปแบบใด:
1. **ส่งเป็น URL:** ใช้ `WebFetch` ดึงรายละเอียด Endpoint, HTTP Method, Headers, Authentication (OAuth2, PKCE, Basic, Bearer, API Key), Request Payload และ Response JSON
2. **ส่งเป็นไฟล์หรือวางข้อความ Spec / cURL / JSON:** อ่านและวิเคราะห์จากข้อมูลที่ส่งมาโดยตรง
3. **ระบุเฉพาะชื่อบริการ:** ใช้ `WebSearch` ค้นหา Official API Documentation ล่าสุดของบริการนั้น
4. **จำแนกประเภท Provider:**
   - **Type A (OAuth 2.0 / OIDC):** มี Redirect Flow (`GetAuthorizationUrl`), Token Exchange (`ExchangeCodeAsync`), UserInfo (`GetUserInfoAsync`), Refresh/Inspect/Revoke Token (ดูต้นแบบจาก `IThaIdProvider` และ `ThaIdProvider`)
   - **Type B (Direct Server-to-Server REST API):** ยิง API ตรวจสอบโดยตรง ไม่มี OAuth Redirect URL โดยใน `GetAuthorizationUrl` ให้โยน `NotSupportedException` (ดูต้นแบบจาก `IDopaProvider` และ `DopaProvider`)

---

## Step 2: กฎเหล็กทางสถาปัตยกรรม (Strict Architectural Rules)

1. **Multi-Targeting (`net5.0;net8.0;net10.0`) — Single Clean Codebase:**
   - ห้ามใช้ `#if NET5_0` หรือ `#if NET8_0` ในโค้ด C#
   - ห้ามใช้ API ที่มีเฉพาะใน .NET 6+ หรือ .NET 8+ เด็ดขาด:
     - ❌ ห้ามใช้ `ArgumentNullException.ThrowIfNull(...)` หรือ `ArgumentException.ThrowIfNullOrWhiteSpace(...)` -> ✅ ใช้ `if (x is null) throw new ArgumentNullException(nameof(x));`
     - ❌ ห้ามใช้ `RandomNumberGenerator.GetBytes(int)` -> ✅ ใช้ `byte[] bytes = new byte[n]; RandomNumberGenerator.Fill(bytes);`
     - ❌ ห้ามใช้ `HMACSHA256.HashData(...)`, `SHA256.HashData(...)`, `Aes.EncryptCbc(...)` -> ✅ ใช้ `using var sha256 = SHA256.Create(); sha256.ComputeHash(...)`
     - ❌ ห้ามใช้ `char.IsAsciiLetterOrDigit(c)` -> ✅ เช็คช่วงตัวอักษร `('A'-'Z', 'a'-'z', '0'-'9')` โดยตรง
   - เมื่อสร้าง `FormUrlEncodedContent` ต้องแปลงเป็น `KeyValuePair<string?, string?>` เสมอเพื่อป้องกัน Warning `CS8620` บน `net5.0`:
     ```csharp
     new FormUrlEncodedContent(formFields.Select(kvp => new KeyValuePair<string?, string?>(kvp.Key, kvp.Value)))
     ```

2. **ห้ามเพิ่มหรืออัปเกรด NuGet Package (`Netka.Identity.SDK.csproj`):**
   - ต้องคง Base Version (`5.0.0`, `8.0.0`, `10.0.0`) ใน `Netka.Identity.SDK.csproj` เท่านั้น ห้ามเพิ่ม Library ใหม่ เพื่อป้องกัน `NU1605: Detected package downgrade` ในโปรเจกต์ปลายทาง

3. **Reuse `ProviderHelper` First, Customize When Needed:**
   - **ต้องใช้ `ProviderHelper` (`Netka.Identity.Infrastructure.Helpers`) เป็นแกนหลักเสมอ:**
     - อ่าน Config ที่จำเป็น: `var section = configuration.GetSection("IdentityVerification:<Name>"); var clientId = section.GetRequiredValue("ClientId");`
     - แปลง JSON Response เป็น Dictionary: `ProviderHelper.ParseJsonToDictionary(json, "INVALID_<NAME>_RESPONSE")`
     - ดึงค่าจาก Dictionary แบบ Case-Insensitive (`OrdinalIgnoreCase`): `dict.GetStringValue("key1", "key2")`
     - สร้างผลลัพธ์กรณี Error: `ProviderHelper.CreateFailureResult(errorCode, message, httpStatusCode)`
     - บันทึก Encrypted PII Vault Log: `_logger.LogEncryptedPiiIfPresent(_encryptionService, ProviderName, responseData, "pid", "citizen_id", "sub", "email")`
   - **อนุญาตให้สร้าง `private` helper เฉพาะในคลาส Provider ได้** เมื่อ API ต้นทางมีรูปแบบการเช็ค Error พิเศษ (เช่น HTTP 200 OK แต่มีฟิลด์ `"IsError": true` หรือ Nested Error Object) หรือต้องการอ่าน Config เสริม แต่สุดท้ายต้องส่งผลลัพธ์ออกผ่าน `ProviderHelper.CreateFailureResult(...)` เสมอ

4. **Contract & Attributes ต้องตรงกับ SDK ปัจจุบัน 100%:**
   - Interface ใหม่ `I<Name>Provider` ต้องสืบทอดจาก `IVerificationProvider` (`Netka.Identity.Core.Interfaces`) ซึ่งบังคับ:
     - `string ProviderName { get; }`
     - `string GetAuthorizationUrl(string redirectUri, string state);`
     - `Task<VerificationResult> VerifyAsync(VerificationRequest request, CancellationToken cancellationToken = default);`
   - ติด Attribute `[AuditLog("IdentityVerification", "<Name>", Description = "...")]` ที่ระดับคลาส และ `[AuditLog("<MethodName>", "<Name>", TrackDiff = false)]` ที่เมธอดหลัก
   - คลาส `VerificationResult` มีเฉพาะ Property: `IsSuccess`, `Message`, `ErrorCode`, `Error`, และ `Data` (ห้ามเขียน `result.ErrorMessage` หรือ `result.StatusCode` เพราะไม่มีอยู่จริง ให้ใช้ `ProviderHelper.CreateFailureResult`)

---

## Step 3: ขั้นตอนการสร้างโค้ดและ Unit Tests

1. **สร้าง Interface:** `Core/Interfaces/I<Name>Provider.cs` (สืบทอดจาก `IVerificationProvider` พร้อม XML Documentation ครบทุกเมธอดเพื่อป้องกัน Warning `CS1591`)
2. **สร้าง Provider Class:** `Infrastructure/Providers/<Name>Provider.cs` (ดูต้นแบบจาก `ThaIdProvider.cs` สำหรับ Type A หรือ `DopaProvider.cs` สำหรับ Type B)
3. **ลงทะเบียน DI ใน `Application/DependencyInjection.cs`:**
   ```csharp
   ConfigureProviderHttpClient<<Name>Provider>(
       services,
       configuration,
       "IdentityVerification:<Name>:AllowUntrustedCertificates");
   services.AddTransient<I<Name>Provider>(sp => sp.GetRequiredService<<Name>Provider>());
   services.AddTransient<IVerificationProvider>(sp => sp.GetRequiredService<<Name>Provider>());
   ```
4. **สร้าง Unit Tests ใน `Tests/<Name>ProviderTests.cs`:**
   - **ห้ามใช้ `Moq`** (โปรเจกต์ Test ไม่ได้ใช้ Moq) ให้ใช้ `MockHttpMessageHandler`, `AesEncryptionService`, และ `NullLogger<<Name>Provider>.Instance` ตามต้นแบบใน `Tests/ThaIdProviderTests.cs` และ `Tests/DopaProviderTests.cs`
   - อัปเดต `Tests/DependencyInjectionTests.cs` เพื่อทดสอบการ Resolve `I<Name>Provider`

---

## Step 4: สร้างเอกสารอ้างอิงใน `docs/providers/<provider-name>.md` (บังคับทำทุกครั้ง)

สร้างไฟล์ `docs/providers/<provider-name-lowercase>.md` (ตามโครงสร้างเดียวกับ `docs/providers/thaid.md` และ `docs/providers/dopa.md`) ประกอบด้วย:
1. ข้อมูลภาพรวม (`ProviderName`, `Config Section`, `Interface`, `Implementation`, มาตรฐานการเชื่อมต่อ, ลิงก์เอกสารต้นฉบับ)
2. ตัวอย่างการตั้งค่าใน `appsettings.json` (ห้ามใส่ Secret จริง)
3. ตัวอย่างการเรียกใช้เมธอดและตารางพารามิเตอร์ขาเข้า (`VerificationRequest` / `AdditionalParameters`)
4. รายชื่อ Key ขาออกทั้งหมดใน `VerificationResult.Data` (Pass-Through Keys)
5. รหัสข้อผิดพลาด (`result.ErrorCode`)
6. อัปเดตตารางสารบัญใน `docs/providers/README.md`, `appsettings.example.json`, และ `README.md` หลักของโปรเจกต์

---

## Step 5: การตรวจสอบความถูกต้อง (Verification)
รันคำสั่งต่อไปนี้เพื่อยืนยันว่า Build และ Unit Tests ผ่านครบทั้ง 3 Runtimes (`net5.0`, `net8.0`, `net10.0`) ด้วย `0 Warning(s)` และ `0 Error(s)`:
```powershell
dotnet test d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK\Netka.Identity.SDK.sln
```
