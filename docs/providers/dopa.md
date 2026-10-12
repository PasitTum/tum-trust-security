# เอกสารอ้างอิงการเชื่อมต่อ DOPA (`DOPA`)

- **Provider Key:** `"DOPA"`
- **Interface:** `Netka.Identity.Core.Interfaces.IDopaProvider` (สืบทอดจาก `IIdentityVerificationProvider`)
- **Implementation:** `Netka.Identity.Infrastructure.Providers.DopaProvider`
- **มาตรฐานการเชื่อมต่อ:** REST API ตรวจสอบสถานะบัตรประชาชน (CheckCardStatus / Laser ID) รองรับทั้ง `application/x-www-form-urlencoded` และ `application/json`

---

## 1. การตั้งค่า (`appsettings.json`)

```json
{
  "IdentityVerification": {
    "Providers": {
      "DOPA": {
        "CheckCardEndpoint": "https://api.dopa.go.th/CheckCardStatus",
        "ApiKey": "<YOUR_BEARER_TOKEN_OR_API_KEY>",
        "TimeoutSeconds": 30,
        "AdditionalHeaders": {
          "ContentType": "form",
          "X-Custom-Header": "optional-value"
        }
      }
    }
  }
}
```
> **หมายเหตุ:**
> - หาก `AdditionalHeaders["ContentType"]` ตั้งเป็น `"json"` จะส่ง Payload แบบ `application/json`
> - ค่าเริ่มต้น (Default) คือส่งแบบ `application/x-www-form-urlencoded`

---

## 2. การรับส่งข้อมูล (`IDopaProvider.VerifyAsync`)

### 2.1 พารามิเตอร์ขาเข้า (`VerificationRequest`)
สามารถส่งผ่าน Property หลักของ `VerificationRequest` หรือส่งผ่าน `AdditionalParameters` (ไม่สนตัวพิมพ์เล็ก-ใหญ่):

| ฟิลด์ที่ส่งให้ DOPA | Property ใน `VerificationRequest` | หรือ Key ใน `AdditionalParameters` | คำอธิบาย |
| :--- | :--- | :--- | :--- |
| `PID` | `request.NationalId` | `"PID"` หรือ `"pid"` | เลขประจำตัวประชาชน 13 หลัก (**Required**) |
| `FirstName` | `request.FirstName` | `"FirstName"` | ชื่อจริงภาษาไทย (**Required**) |
| `LastName` | `request.LastName` | `"LastName"` | นามสกุลภาษาไทย (**Required**) |
| `BirthDay` | `request.BirthDate` | `"BirthDay"` | วันเดือนปีเกิด พ.ศ. รูปแบบ `YYYYMMDD` เช่น `"25350131"` (**Required**) |
| `Laser` | `request.LaserCode` | `"Laser"` | เลขหลังบัตรประชาชน (Laser ID 12 หลัก เช่น `"JT0123456789"`) (**Required**) |
| *(ฟิลด์เสริมอื่นๆ)* | - | Key ใดก็ได้ใน `AdditionalParameters` | SDK จะแนบต่อท้ายไปให้ DOPA อัตโนมัติ (Pass-Through) |

### 2.2 ผลลัพธ์ (`VerificationResult` — Pass-Through Dictionary ใน `result.Data`)
- `result.IsSuccess`: `true` เมื่อ HTTP Status 2xx และไม่พบฟิลด์ `IsError = true` หรือ `error` ใน JSON
- `result.Data` (เก็บค่า JSON ดิบทั้งหมดที่ DOPA ตอบกลับมา ค้นหา Key แบบ `OrdinalIgnoreCase`):
  - `"IsError"`: `false` (เมื่อสำเร็จ) หรือ `true` (เมื่อเกิดข้อผิดพลาด)
  - `"ErrorMessage"`: ข้อความแจ้งข้อผิดพลาดจากกรมการปกครอง (ถ้ามี)
  - `"Code"` / `"StCode"`: รหัสสถานะบัตร (เช่น `0` = ปกติ)
  - `"Desc"`: คำอธิบายสถานะบัตรประชาชน (เช่น `"สถานะปกติ"`)
