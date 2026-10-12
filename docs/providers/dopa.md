# เอกสารอ้างอิงการเชื่อมต่อ DOPA (`DOPA`)

- **ProviderName:** `"DOPA"`
- **Config Section:** `"IdentityVerification:Dopa"`
- **Interface:** `Netka.Identity.Core.Interfaces.IDopaProvider` (สืบทอดจาก `IVerificationProvider`)
- **Implementation:** `Netka.Identity.Infrastructure.Providers.DopaProvider`
- **มาตรฐานการเชื่อมต่อ:** Direct Server-to-Server REST API (`POST application/json` พร้อม `Authorization: Bearer <ApiKey>`) สำหรับตรวจสอบสถานะบัตรประชาชนด้วยรหัสหลังบัตร (Laser ID) หรือเลขชิปสมาร์ทการ์ด (Chip No)

---

## 1. การตั้งค่า (`appsettings.json`)

```json
{
  "IdentityVerification": {
    "EncryptionKey": "<BASE64_32_BYTE_AES_KEY>",
    "Dopa": {
      "VerificationUrl": "https://dopa-gateway.your-agency.local/api/v1/card/verify",
      "LaserVerificationUrl": "https://dopa-gateway.your-agency.local/api/v1/card/verify-laser",
      "ChipVerificationUrl": "https://dopa-gateway.your-agency.local/api/v1/card/verify-chip",
      "ApiKey": "<YOUR_DOPA_GATEWAY_API_KEY>",
      "AllowUntrustedCertificates": false
    }
  }
}
```
> **หมายเหตุ:** ต้องกำหนดอย่างน้อย `VerificationUrl` หรือ `LaserUrl` / `LaserVerificationUrl` อย่างใดอย่างหนึ่ง หากไม่ได้แยก URL ของ Laser และ Chip ระบบจะใช้ `VerificationUrl` เป็นค่าเริ่มต้นให้ทั้งคู่

---

## 2. เมธอดและการรับส่งข้อมูล (`IDopaProvider`)

### 2.1 ตรวจสอบด้วยข้อมูลหน้าบัตรและเลขหลังบัตร (`VerifyByLaserAsync`)
```csharp
VerificationResult result = await _dopaProvider.VerifyByLaserAsync(
    pid: "1234567890121",       // เลขบัตรประชาชน 13 หลัก (ตรวจ Checksum Modulo 11 อัตโนมัติ)
    firstName: "สมชาย",          // ชื่อภาษาไทย
    lastName: "ใจดี",            // นามสกุลภาษาไทย
    birthDate: "25350131",      // วันเกิด พ.ศ. รูปแบบ YYYYMMDD
    laserId: "ME0123456789",    // เลขหลังบัตร (Laser ID)
    cancellationToken: ct);
```

### 2.2 ตรวจสอบด้วยเลขบัตรและเลขชิปสมาร์ทการ์ด (`VerifyByChipAsync`)
```csharp
VerificationResult result = await _dopaProvider.VerifyByChipAsync(
    pid: "1234567890121",       // เลขบัตรประชาชน 13 หลัก (ตรวจ Checksum Modulo 11 อัตโนมัติ)
    chipNo: "0011223344556677", // เลข Chip Number ที่อ่านได้จากเครื่องอ่านบัตรสมาร์ทการ์ด
    bp1No: "1234567890",        // (Optional) เลขคำขอมีบัตร (BP1)
    cancellationToken: ct);
```

### 2.3 ตรวจสอบแบบยืดหยุ่นผ่าน Dictionary (`VerifyAsync(VerificationRequest)`)
ส่งพารามิเตอร์ใดๆ ที่ต้องการไปให้ DOPA Gateway ผ่าน `request.AdditionalParameters` (รองรับ Case-Insensitive `OrdinalIgnoreCase`):
```csharp
var request = new VerificationRequest
{
    ProviderName = "DOPA",
    AdditionalParameters =
    {
        ["PID"] = "1234567890121",
        ["FirstName"] = "สมชาย",
        ["LastName"] = "ใจดี",
        ["BirthDay"] = "25350131",
        ["Laser"] = "ME0123456789"
    }
};
VerificationResult result = await _dopaProvider.VerifyAsync(request, ct);
```

---

## 3. ข้อมูลขาออก (`VerificationResult.Data` — Pass-Through Keys)

- `result.IsSuccess`: เป็น `true` เมื่อ HTTP Status เป็น `2xx` และฟิลด์ `"IsError"` ใน JSON ไม่ได้เป็น `true`
- `result.Data` (เก็บ JSON ดิบทั้งหมดจาก DOPA ค้นหา Key แบบ `OrdinalIgnoreCase` หรือใช้ `result.Data.GetStringValue(...)`):
  - `"IsError"`: `false` (เมื่อบัตรปกติ) หรือ `true` (เมื่อสถานะบัตรไม่ปกติ/ข้อมูลไม่ตรง)
  - `"ErrorMessage"` / `"Desc"` / `"message"`: คำอธิบายผลการตรวจสอบจากกรมการปกครอง (เช่น `"สถานะปกติ"`)
  - `"Code"` / `"StCode"`: รหัสสถานะบัตรประชาชนตามมาตรฐาน DOPA

---

## 4. รหัสข้อผิดพลาด (`result.ErrorCode`)
- `INVALID_PID_FORMAT`: เลขประจำตัวประชาชนไม่ครบ 13 หลัก หรือไม่ผ่านการคำนวณ Checksum Modulo 11
- `MISSING_PARAMETERS`: ข้อมูลบังคับไม่ครบถ้วน
- `CARD_VERIFICATION_REJECTED`: เชื่อมต่อ DOPA สำเร็จ แต่ DOPA ตอบกลับว่า `"IsError": true` (ข้อมูลดิบยังคงอยู่ใน `result.Data`)
- `DOPA_API_FAILED`: DOPA ตอบกลับด้วย HTTP Non-2xx (ดู HTTP Status Code ได้จาก `result.Error?.HttpStatusCode`)
- `INVALID_DOPA_RESPONSE`: JSON ที่ได้รับไม่ถูกต้อง
- `NETWORK_ERROR`: เกิดข้อผิดพลาดในการเชื่อมต่อโครงข่าย (`HttpRequestException`)
