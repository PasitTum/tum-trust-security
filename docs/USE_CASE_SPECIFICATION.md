# รายงาน Use Case และข้อกำหนดการจำลอง (Use Case & Scenario Specification Report)
**โปรเจกต์:** `Netka.Identity.SDK`  
**ขอบเขต:** มาตรฐาน 12 Use Cases สำหรับการพิสูจน์และยืนยันตัวตนผ่าน **DOPA ThaID (6 Use Cases)** และ **DOPA Check Card Service (6 Use Cases)**  
**รองรับทั้งใน:**
1. โหมดจำลองในตัวไลบรารี **`UseMock = true`** (ภายในไฟล์ `Netka.Identity.SDK.dll` โดยไม่ต้องต่ออินเทอร์เน็ต)
2. เซิร์ฟเวอร์จำลองแยกอิสระ **`MockServer`** (`http://localhost:5199`)
3. ชุดทดสอบอัตโนมัติ **`Tests`** (`Netka.Identity.SDK.Tests`)

---

## 1. ภาพรวมการทำงานของ OAuth 2.0 Redirect & QR Code (ThaID Flow)

### 1.1 กรณีใช้งานจริงกับกรมการปกครอง (`UseMock = false`)
```text
[ตู้ Kiosk / Web App]                                    [เซิร์ฟเวอร์กรมการปกครอง ThaID]
       │                                                               │
       ├─ 1. เรียก GetAuthorizationUrl(redirectUri, state, pkce)       │
       │     ได้ URL: https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/?...
       │                                                               │
       ├─ 2. นำ URL ไปแสดง QR Code หรือสั่ง Redirect Browser ─────────►│
       │                                                               ├─ 3. ประชาชนสแกน QR ด้วยแอป ThaID
       │                                                               │     และกด "ยินยอม (Consent)"
       │◄─ 4. HTTP 302 Redirect กลับมายัง redirect_uri พร้อม ?code=...&state=...
       │                                                               │
       ├─ 5. เรียก ExchangeCodeAsync(code, redirectUri, codeVerifier) ─►│
       │◄─ 6. คืน JSON ข้อมูลประชาชนตาม Scope (สูงสุด 20 Scopes) ───────┤
```

### 1.2 กรณีเปิดโหมดจำลองในตัว `.dll` (`UseMock = true` — Loopback Redirect)
เมื่อเปิด `UseMock = true` ตัวเมธอด `GetAuthorizationUrl(...)` จะคืนค่า **Loopback Callback URL** ชี้กลับมายัง `redirect_uri` ของโปรเจกต์ปลายทางโดยตรง:
- **ถ้าตู้ Kiosk นำ URL ไปวาดรูป QR Code บนหน้าจอเอง:** จะได้รูป QR Code ปกติ 100% เมื่อเปิดลิงก์หรือจำลองการสแกนจะวิ่งเข้า Callback ทันที
- **ถ้าตู้ Kiosk สั่ง `Redirect(authUrl)`:** Browser จะวิ่งวนเข้าหน้า Callback (`redirect_uri`) ของตัวเองทันทีเสมือนว่าประชาชนสแกนเสร็จแล้ว

---

## 2. รายละเอียด 6 Use Cases ของ DOPA ThaID (`IThaIdProvider`)

| รหัส Use Case | ชื่อสถานการณ์ (Scenario) | วิธีทริกเกอร์ใน `UseMock = true` (ใน `.dll`) | วิธีทริกเกอร์บน `MockServer` (`localhost:5199`) | ผลลัพธ์ที่ได้จาก SDK (`VerificationResult`) |
| :--- | :--- | :--- | :--- | :--- |
| **`UC-THAID-01`** | **Happy Path (ยืนยันตัวตนสำเร็จ + ครบ 20 Scopes)** | `MockScenario = "success"` *(ค่าเริ่มต้น)* หรือส่ง `code = "mock-auth-code-001"` | กดปุ่ม `✅ ยินยอมสำเร็จ` หรือส่ง `?scenario=success` | `IsSuccess = true` พร้อม `Data` ครบทั้ง **20 Scopes** + `access_token`, `refresh_token`, `id_token`, `expires_in` |
| **`UC-THAID-02`** | **Invalid / Mismatched OAuth State (ป้องกัน CSRF)** | `MockScenario = "invalid_state"` หรือส่ง `expected_state` ไม่ตรงกับ `Request.State` | กดปุ่ม `⚠️ จำลองค่า State ไม่ตรง` หรือส่ง `?scenario=invalid_state` | `IsSuccess = false`, `ErrorCode = "INVALID_STATE"` (บล็อกทันทีโดยไม่แลก Token) |
| **`UC-THAID-03`** | **Expired / Invalid Authorization Code (`invalid_grant`)** | `MockScenario = "expired_code"` หรือส่ง `code = "mock-expired-code"` | กดปุ่ม `⏰ จำลอง Code หมดอายุ` หรือส่ง `?scenario=expired_code` | `IsSuccess = false`, `ErrorCode = "TOKEN_EXCHANGE_FAILED"`, `HttpStatusCode = 400` |
| **`UC-THAID-04`** | **ThaID Server / Network Timeout** | `MockScenario = "timeout"` หรือส่ง `code = "mock-timeout"` | กดปุ่ม `🔌 จำลองโครงข่ายล่ม` หรือส่ง `?scenario=timeout` | `IsSuccess = false`, `ErrorCode = "NETWORK_ERROR"` |
| **`UC-THAID-05`** | **Token Introspection & Revocation** | เรียก `InspectTokenAsync(token)` หรือ `RevokeTokenAsync(token)` (ถ้าส่ง `"revoked-token"` จะได้ `active = false`) | เรียก `/api/v2/oauth2/introspect/` หรือ `/api/v2/oauth2/revoke/` | `IsSuccess = true`, `Data["active"] = true/false` และยกเลิก Token สำเร็จ |
| **`UC-THAID-06`** | **User Denied Consent (ประชาชนกดยกเลิกในแอป ThaID)** | `MockScenario = "deny"` (`GetAuthorizationUrl` คืน `?error=access_denied`) | กดปุ่ม `❌ ประชาชนกดยกเลิก` หรือส่ง `?scenario=deny` | Redirect กลับมาพร้อม `error=access_denied` (ไม่มี `code`) หากส่งเข้า `VerifyAsync` จะได้ `ErrorCode = "MISSING_AUTH_CODE"` |

### ตัวอย่างข้อมูล JSON ครบ 20 Scopes ที่ได้รับจาก `UC-THAID-01` (`VerificationResult.Data`)
```json
{
  "sub": "thaid-mock-sub-001",
  "pid": "1100501234561",
  "title": "นาย",
  "title_en": "Mr.",
  "name": "นาย สมชาย ใจดี",
  "name_en": "Mr. Somchai Jaidee",
  "given_name": "สมชาย",
  "middle_name": "",
  "family_name": "ใจดี",
  "given_name_en": "Somchai",
  "middle_name_en": "",
  "family_name_en": "Jaidee",
  "gender": "male",
  "birthdate": "1990-05-15",
  "address": {
    "formatted": "99/9 หมู่ที่ 1 ถนนแจ้งวัฒนะ ตำบลคลองเกลือ อำเภอปากเกร็ด จังหวัดนนทบุรี 11120",
    "house_number": "99/9",
    "moo": "หมู่ที่ 1",
    "trok": "",
    "soi": "",
    "road": "ถนนแจ้งวัฒนะ",
    "subdistrict": "คลองเกลือ",
    "district": "ปากเกร็ด",
    "province": "นนทบุรี",
    "postal_code": "11120"
  },
  "house_address": {
    "raw": "99/9#1####ถนนแจ้งวัฒนะ#คลองเกลือ#ปากเกร็ด#นนทบุรี"
  },
  "smartcard_code": "1001012345678901",
  "date_of_issuance": "2021-05-15",
  "date_of_expiry": "2030-05-14",
  "ial": 2.3,
  "access_token": "mock-thaid-access-token-2026",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "mock-thaid-refresh-token-2026",
  "id_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.mock-id-token"
}
```

---

## 3. รายละเอียด 6 Use Cases ของ DOPA Check Card Service (`IDopaProvider`)

รองรับทั้งการตรวจสอบด้วยเลขหลังบัตร (`VerifyByLaserAsync`) และการตรวจสอบผ่านชิปสมาร์ทการ์ด (`VerifyByChipAsync`):

| รหัส Use Case | ชื่อสถานการณ์ (Scenario) | เงื่อนไข Input ที่ใช้ทริกเกอร์ (ใช้ได้ทั้ง `UseMock = true` และ `MockServer`) | ผลลัพธ์ที่ได้จาก SDK (`VerificationResult`) |
| :--- | :--- | :--- | :--- |
| **`UC-DOPA-01`** | **Happy Path (สถานะบัตรปกติ)** | `pid` 13 หลักถูกต้องตามสูตร Modulo 11 (เช่น `"1234567890121"`) + `laserId` ปกติ (เช่น `"ME0123456789"`) หรือ `chipNo` ปกติ | `IsSuccess = true`, `Data["Code"] = 0`, `Data["Desc"] = "สถานะปกติ"`, `Data["IsError"] = false` |
| **`UC-DOPA-02`** | **Invalid PID Format / Checksum (เลขบัตรผิดหลัก Modulo 11)** | `pid` ไม่ครบ 13 หลัก หรือหลักที่ 13 คำนวณ Modulo 11 ไม่ตรง (เช่น `"1234567890122"`) | `IsSuccess = false`, `ErrorCode = "INVALID_PID_FORMAT"` (SDK บล็อกทันทีโดยไม่ยิง API) |
| **`UC-DOPA-03`** | **Missing Required Parameters (ส่งข้อมูลไม่ครบ)** | ส่ง `firstName`, `lastName`, `birthDate`, `laserId`, หรือ `chipNo` เป็นค่าว่าง (`""` หรือ `null`) | `IsSuccess = false`, `ErrorCode = "MISSING_PARAMETERS"` (SDK บล็อกทันทีโดยไม่ยิง API) |
| **`UC-DOPA-04`** | **Card Verification Rejected (เลขหลังบัตรไม่ตรง / บัตรถูกยกเลิก)** | ส่ง `laserId = "ME9999999999"` หรือ `chipNo = "0000000000000000"` | `IsSuccess = false`, `ErrorCode = "CARD_VERIFICATION_REJECTED"`, พร้อม `Data["IsError"] = true`, `Data["Code"] = 1` |
| **`UC-DOPA-05`** | **DOPA Gateway Unavailable (HTTP 503 Service Unavailable)** | ส่ง `laserId = "ME5000000000"` หรือ `chipNo = "5000000000000000"` | `IsSuccess = false`, `ErrorCode = "DOPA_API_FAILED"`, `HttpStatusCode = 503` |
| **`UC-DOPA-06`** | **Network / VPN Connection Timeout** | ส่ง `laserId = "METIMEOUT000"` หรือ `chipNo = "TIMEOUT000000000"` | `IsSuccess = false`, `ErrorCode = "NETWORK_ERROR"` |

### ตัวอย่างข้อมูล JSON ที่ได้รับจาก `UC-DOPA-01` (บัตรสถานะปกติ)
```json
{
  "Code": 0,
  "Desc": "สถานะปกติ",
  "IsError": false,
  "ErrorMessage": ""
}
```

### ตัวอย่างข้อมูล JSON ที่ได้รับจาก `UC-DOPA-04` (บัตรไม่ผ่านการตรวจสอบ / เลขหลังบัตรไม่ตรง)
```json
{
  "Code": 1,
  "Desc": "สถานะบัตรไม่ปกติ หรือข้อมูลไม่ตรงกับฐานข้อมูลทะเบียนราษฎร",
  "IsError": true,
  "ErrorMessage": "ข้อมูลเลขหลังบัตร (Laser ID) หรือหมายเลขชิปไม่ถูกต้อง"
}
```
