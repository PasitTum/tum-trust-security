# คู่มือการเชื่อมต่อ TUM TRUST SECURITY (`IBorderlessTrustProvider`)

> **ระบบ:** TUM TRUST SECURITY Universal Sovereign Trust Engine  
> **มาตรฐานสากล:** ICAO Doc 9303 (ePassport), ISO/IEC 18013-5 (mDL), ISO/IEC 30107 (Liveness PAD)  
> **Interface:** `IBorderlessTrustProvider` (สืบทอดจาก `IVerificationProvider`)  
> **ไฟล์ทดสอบ REST Client:** [`borderless.http`](./borderless.http)  

---

## 1. ขอบเขตการทำงาน (Capabilities)

1. **ePassport NFC Verification (150+ ประเทศ):**
   * รองรับการตรวจสอบ ePassport สากล (USA, GBR, DEU, JPN, THA และประเทศภาคี ICAO)
   * คำนวณและตรวจสอบ Check Digit (Modulo 10 ถ่วงน้ำหนัก 7-3-1)
   * ตรวจสอบลายเซ็นดิจิทัลของรัฐบาล (Passive Authentication) และป้องกันการโคลนนิ่งชิป (Active Authentication)
2. **Apple Wallet & Google Wallet (mDL):**
   * ตรวจสอบใบขับขี่และบัตรประชาชนดิจิทัล ISO/IEC 18013-5 จากสมาร์ตโฟนผ่าน NFC/BLE
3. **Neuromorphic Edge AI Deepfake Shield:**
   * ตรวจสอบความมีชีวิตจริงของมนุษย์ (Liveness Detection) สกัดกั้นหน้ากากซิลิโคนและภาพ Deepfake ใน < 80ms
4. **Zero-Knowledge Sovereign Pass:**
   * ออกโทเคน `ZeroKnowledgeTrustToken` (`ntk_zkp_...`) เพื่อยืนยันสิทธิ์โดยไม่ต้องบันทึกข้อมูล PII ของประชาชน

---

## 2. ตัวอย่างการลงทะเบียนผ่าน Dependency Injection

```csharp
// Program.cs
using TUM.Identity.Application;
using TUM.Identity.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ลงทะเบียน TUM Identity พร้อมระบบ Borderless Trust Provider อัตโนมัติ
builder.Services.AddIdentityVerification(options =>
{
    options.UseMock = true; // เปิดโหมดจำลองสำหรับทดสอบ
});

var app = builder.Build();
```

---

## 3. ตัวอย่างการเรียกใช้งานใน Controller / Minimal API

```csharp
app.MapPost("/api/verify-guest-passport", async (
    PassportVerificationRequest request, 
    IBorderlessTrustProvider borderless) =>
{
    // Live camera frame bytes (optional สำหรับทำ 1:1 Biometric Face Match)
    byte[]? liveCameraFrame = GetCameraFrame();

    var result = await borderless.VerifyPassportAsync(request, liveCameraFrame);

    if (result.IsSuccess && result.IsLiveHuman)
    {
        return Results.Ok(new
        {
            Status = "AUTHORIZED",
            Country = result.IssuingCountry,
            Holder = result.Mrz?.GivenNames + " " + result.Mrz?.Surname,
            ZkpToken = result.ZeroKnowledgeTrustToken
        });
    }

    return Results.BadRequest(new { Error = result.Message });
});
```

---

## 4. โครงสร้างข้อมูลที่ได้รับคืน (`PassportVerificationResult`)

```json
{
  "is_success": true,
  "message": "Sovereign ePassport verified successfully (USA).",
  "issuing_country": "USA",
  "is_passive_auth_valid": true,
  "is_active_auth_valid": true,
  "is_biometric_match": true,
  "biometric_match_score": 0.985,
  "is_live_human": true,
  "liveness_score": 0.994,
  "zero_knowledge_trust_token": "ntk_zkp_eyJpc3MiOiJodHRwczovL3RydXN0Lm5ldGthLmlvIiwic3ViIjoic292ZXJlaWduLXVzZXItdXNhLTAwMSIsImNvdW50cnkiOiJVU0EiLCJpc19yZWFsX2h1bWFuIjp0cnVlLCJpc19zb3ZlcmVpZ25fdmVyaWZpZWQiOnRydWUsImFnZV9vdmVyXzIxIjp0cnVlLCJ6a3BfZ3VhcmFudGVlIjoiWkVST19SQVdfUElJX1BFUlNJU1RFRCIsImV4cCI6MTc2MDUyNjYwMH0=",
  "data": {
    "country": "USA",
    "document_type": "P",
    "passive_auth": "PASSED_CSCA_VERIFIED",
    "active_auth": "PASSED_CHIP_UNCLONED",
    "liveness_standard": "ISO_30107_LEVEL_2_COMPLIANT",
    "zkp_pass": "ntk_zkp_..."
  }
}
```
