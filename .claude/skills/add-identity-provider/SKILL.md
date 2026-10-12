---
name: add-identity-provider
description: สร้าง Identity Verification Provider ใหม่เข้าสู่โปรเจกต์ Netka.Identity.SDK โดยรับข้อมูลจาก URL, ข้อความ Spec, หรือค้นหาจากเว็บ พร้อมสร้าง Unit Tests (net5.0, net8.0, net10.0) และสร้างเอกสารอ้างอิงใน docs/providers/<provider-name>.md อัตโนมัติ
---

# Skill: Add Identity Verification Provider (`add-identity-provider`)

คุณคือสถาปนิกซอฟต์แวร์ผู้เชี่ยวชาญสำหรับโปรเจกต์ `Netka.Identity.SDK` (`d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK`) หน้าที่ของคุณคือการสร้าง Identity Verification Provider ใหม่ตามมาตรฐาน Clean Architecture ของ SDK นี้ พร้อมสร้าง Unit Tests และจัดทำเอกสารการเชื่อมต่อของ Provider นั้นลงใน `docs/providers/` ให้ครบจบในคำสั่งเดียว

---

## Step 1: รวบรวมข้อมูล Specification (Spec Ingestion)
ตรวจสอบว่าผู้ใช้ส่งข้อมูลของ Provider ใหม่มาในรูปแบบใด:
1. **ส่งเป็น URL:** ใช้เครื่องมือดึงข้อมูลจากหน้าเว็บ (`WebFetch`) เพื่ออ่าน Endpoint, HTTP Method, Headers, Authentication (OAuth2, PKCE, Basic, Bearer, API Key, HMAC), Request Payload และ Response JSON
2. **ส่งเป็นไฟล์หรือวางข้อความ Spec / cURL / JSON:** อ่านไฟล์ที่ระบุ หรือวิเคราะห์จากข้อความที่ผู้ใช้ส่งมาโดยตรง
3. **ระบุเฉพาะชื่อบริการ:** ใช้เครื่องมือค้นหาเว็บ (`WebSearch`) เพื่อค้นหา Official API Documentation ล่าสุดของบริการนั้น และสรุปโครงสร้าง API ก่อนเริ่มเขียนโค้ด

---

## Step 2: กฎเหล็กทางสถาปัตยกรรม (Strict Architectural Rules)

1. **Multi-Targeting (`net5.0;net8.0;net10.0`) — Single Clean Codebase:**
   - ห้ามใช้ `#if NET5_0` หรือ `#if NET8_0` ในโค้ด C#
   - ห้ามใช้ API ที่มีเฉพาะใน .NET 6+ หรือ .NET 8+ เด็ดขาด เพื่อป้องกัน Compile Error บน `net5.0`:
     - ❌ ห้ามใช้ `ArgumentNullException.ThrowIfNull(...)` หรือ `ArgumentException.ThrowIfNullOrWhiteSpace(...)` -> ✅ ให้ใช้ `if (x == null) throw new ArgumentNullException(nameof(x));` หรือ `string.IsNullOrWhiteSpace(...)`
     - ❌ ห้ามใช้ `RandomNumberGenerator.GetBytes(int)` -> ✅ ให้ใช้ `var bytes = new byte[n]; RandomNumberGenerator.Fill(bytes);`
     - ❌ ห้ามใช้ `HMACSHA256.HashData(...)`, `SHA256.HashData(...)`, `Aes.EncryptCbc(...)` -> ✅ ให้ใช้ `using var hmac = new HMACSHA256(key); hmac.ComputeHash(...)` หรือ `using var sha = SHA256.Create(); sha.ComputeHash(...)`
     - ❌ ห้ามใช้ `char.IsAsciiLetterOrDigit(c)` -> ✅ ให้เช็คช่วงตัวอักษร `('A'-'Z', 'a'-'z', '0'-'9')` โดยตรง
   - เมื่อสร้าง `FormUrlEncodedContent` ให้ใช้ `IEnumerable<KeyValuePair<string?, string?>>` หรือ `Dictionary<string?, string?>` เพื่อไม่ให้เกิด Warning `CS8620` บน `net5.0`

2. **ห้ามเพิ่มหรืออัปเกรด NuGet Package (`Netka.Identity.SDK.csproj`):**
   - ต้องรักษา Base Version (`5.0.0`, `8.0.0`, `10.0.0`) ใน `Netka.Identity.SDK.csproj` เท่านั้น ห้ามเพิ่ม Library นอกเหนือจากที่มีอยู่ เพื่อป้องกันปัญหา `NU1605: Detected package downgrade` ในโปรเจกต์ปลายทาง

3. **Pass-Through Adapter Principle:**
   - ห้ามบังคับแปลง Response ของ Provider เป็น Domain Model ตายตัวที่ตัดทอนข้อมูลทิ้ง
   - ต้องใช้ `ProviderHelper.ParseJsonToDictionary(rawContent)` เพื่อส่งค่า JSON ดิบทั้งหมดจาก Provider ต้นทางกลับไปใน `VerificationResult.Data`

4. **Case-Insensitive & Culture-Invariant:**
   - การอ่านค่าจาก Dictionary ทุกจุดต้องใช้ `ProviderHelper.GetStringValue(...)` หรือ `StringComparison.OrdinalIgnoreCase`
   - การแปลงวันเวลาหรือตัวเลขใน Payload ต้องระบุ `CultureInfo.InvariantCulture` เสมอ

5. **Security & Privacy:**
   - ห้าม Log ข้อมูล PII (เลขบัตรประชาชน, ชื่อ-นามสกุล, วันเกิด, Laser Code, Token, Secret) ลงใน `ILogger` เด็ดขาด ให้ใช้ `DataMaskingHelper` เสมอ

---

## Step 3: ขั้นตอนการสร้างโค้ดและ Unit Tests

1. **อ่านไฟล์ต้นแบบก่อนลงมือ:**
   - อ่าน `Core/Interfaces/IIdentityVerificationProvider.cs`, `Core/Interfaces/IThaIdProvider.cs`, `Core/Interfaces/IDopaProvider.cs`
   - อ่าน `Infrastructure/Providers/ProviderHelper.cs`, `Infrastructure/Providers/DopaProvider.cs`, `Infrastructure/Providers/ThaIdProvider.cs`
   - อ่าน `Infrastructure/DependencyInjection.cs`

2. **สร้าง Interface เฉพาะของ Provider:**
   - สร้างไฟล์ `Core/Interfaces/I<ProviderName>Provider.cs` ที่สืบทอดจาก `IIdentityVerificationProvider`

3. **สร้างคลาส Provider ใน `Infrastructure/Providers/<ProviderName>Provider.cs`:**
   - รับ `HttpClient`, `IOptions<IdentityVerificationOptions>`, และ `ILogger<<ProviderName>Provider>` ทาง Constructor
   - ดึง Config จาก `options.Value.Providers` ด้วยชื่อ `ProviderName` (ถ้าไม่พบให้ใช้ `new ProviderConfiguration()`)
   - ใช้ `ProviderHelper.ValidateRequiredFields(...)` ตรวจสอบพารามิเตอร์ที่จำเป็น
   - ดักจับ `OperationCanceledException` (เมื่อไม่ถูกยกเลิกจาก `cancellationToken` ของผู้ใช้) และ `HttpRequestException` แล้วคืนค่า `VerificationResult.Failure("NETWORK_ERROR", ...)`

4. **ลงทะเบียน Dependency Injection ใน `Infrastructure/DependencyInjection.cs`:**
   - เพิ่ม `AddHttpClient<I<ProviderName>Provider, <ProviderName>Provider>(...)` พร้อมผูก `.AddPolicyHandler(GetRetryPolicy())` และ `.AddPolicyHandler(GetCircuitBreakerPolicy())`
   - ลงทะเบียนเข้าสู่ `IIdentityVerificationProvider` เพื่อให้ `IdentityVerifierFactory` เรียกใช้งานผ่านชื่อ `ProviderName` ได้ทันที

5. **สร้าง Unit Tests ใน `Tests/<ProviderName>ProviderTests.cs`:**
   - ทดสอบ Happy Path (200 OK + ตรวจค่าใน `result.Data`)
   - ทดสอบ Missing Required Fields (`INVALID_REQUEST`)
   - ทดสอบ Case-Insensitive Input Keys ใน `AdditionalParameters` (เช่นส่งตัวพิมพ์เล็กล้วน/พิมพ์ใหญ่ล้วน)
   - ทดสอบ Provider Error (HTTP 4xx/5xx หรือ Error Flag ใน JSON)
   - ทดสอบ Network Timeout (`NETWORK_ERROR`)
   - เพิ่มการตรวจ DI Resolution ของ Provider ใหม่ใน `Tests/DependencyInjectionTests.cs`

---

## Step 4: สร้างเอกสารการเชื่อมต่อของ Provider ลงใน `docs/providers/<provider-name>.md` (บังคับทำทุกครั้ง)

เมื่อสร้างโค้ดเสร็จแล้ว ต้องสร้างไฟล์เอกสารอ้างอิงของ Provider ใหม่ไว้ที่ `docs/providers/<provider-name-lowercase>.md` (ดูรูปแบบตัวอย่างจาก `docs/providers/thaid.md` และ `docs/providers/dopa.md`) เพื่อให้ Skill `integrate-netka-identity` นำไปใช้อ้างอิงตอนติดตั้งในโปรเจกต์อื่น โดยมีหัวข้อครบดังนี้:

1. **ข้อมูลภาพรวม (Header Metadata):**
   - `Provider Key`, ชื่อ `Interface`, ชื่อคลาส `Implementation`, มาตรฐานการเชื่อมต่อ (เช่น OAuth 2.0 + PKCE, REST API + Bearer, Form/JSON) และลิงก์เอกสารต้นฉบับ
2. **การตั้งค่า (`appsettings.json`):**
   - ตัวอย่าง JSON สำหรับใส่ใน `"IdentityVerification:Providers:<ProviderKey>"` (ห้ามใส่ Secret หรือ API Key จริง ให้ใช้ `<YOUR_...>`)
3. **ตารางพารามิเตอร์ขาเข้า (`VerificationRequest`):**
   - จับคู่ระหว่างชื่อฟิลด์ของ API ต้นทาง กับ Property ใน `VerificationRequest` หรือ Key ใน `AdditionalParameters` พร้อมระบุว่าฟิลด์ใด Required/Optional
4. **ข้อมูลขาออก (`VerificationResult.Data` — Pass-Through Dictionary Keys):**
   - อธิบายเงื่อนไข `result.IsSuccess` และระบุรายชื่อ Key ทั้งหมดที่ Provider ต้นทางส่งกลับมาใน `result.Data` พร้อมคำอธิบายความหมาย
5. **อัปเดตสารบัญเอกสาร:**
   - เพิ่มรายชื่อและลิงก์ของไฟล์ `docs/providers/<provider-name>.md` ลงในตารางของไฟล์ `docs/providers/README.md` และอัปเดต `README.md` หลักของโปรเจกต์

---

## Step 5: การตรวจสอบความถูกต้อง (Verification)
รันคำสั่งต่อไปนี้เพื่อยืนยันว่าโค้ดและ Unit Tests ผ่านทั้ง 3 Runtimes (`net5.0`, `net8.0`, `net10.0`) โดยไม่มี Warning หรือ Error:
```powershell
dotnet test d:\Work\NKS-Kiosk-Config\NKS-Kiosk-Identity-SDK\Netka.Identity.SDK.sln
```
