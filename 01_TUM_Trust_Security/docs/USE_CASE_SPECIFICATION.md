# รายงาน Use Case และข้อกำหนดการจำลอง (Use Case & Scenario Specification Report)
**โปรเจกต์:** `TUM.Identity.SDK`  
**ขอบเขต:** มาตรฐาน 12 Use Cases สำหรับการพิสูจน์และยืนยันตัวตนผ่าน **DOPA ThaID (6 Use Cases)** และ **DOPA Check Card Service (6 Use Cases)**  
**รองรับทั้งใน:**
1. โหมดจำลองในตัวไลบรารี **`UseMock = true`** (ภายในไฟล์ `TUM.Identity.SDK.dll` โดยไม่ต้องต่ออินเทอร์เน็ต)
2. เซิร์ฟเวอร์จำลองแยกอิสระ **`MockServer`** (`http://localhost:5199`)
3. ชุดทดสอบอัตโนมัติ **`Tests`** (`TUM.Identity.SDK.Tests`)


---

## 0. แหล่งที่มาและมาตรฐานอ้างอิงทางการ (Official Sources & Standards References)

Use Case ทั้งหมด 12 ข้อ ถูกสังเคราะห์และถอดแบบมาจากมาตรฐานสากลและข้อกำหนดของหน่วยงานภาครัฐไทยโดยตรง ดังนี้:

### 0.1 แหล่งอ้างอิงฝั่ง ThaID (กรมการปกครอง - BORA)
1. **ThaID OpenID Connect Discovery & JWKS (ระบบจริง):**
   - Live Discovery Endpoint: [https://imauth.bora.dopa.go.th/.well-known/openid-configuration](https://imauth.bora.dopa.go.th/.well-known/openid-configuration)
   - Live JWKS Key Endpoint: [https://imauth.bora.dopa.go.th/jwks/](https://imauth.bora.dopa.go.th/jwks/)
   - ขอบเขตข้อมูล (20 Scopes ทางการ): `openid`, `pid`, `title`, `title_en`, `given_name`, `middle_name`, `family_name`, `name`, `given_name_en`, `middle_name_en`, `family_name_en`, `name_en`, `gender`, `birthdate`, `address`, `house_address`, `smartcard_code`, `date_of_issuance`, `date_of_expiry`, `ial`
2. **มาตรฐานสากล RFC / OpenID Foundation:**
   - **RFC 7636 (PKCE):** [Proof Key for Code Exchange by OAuth Public Clients](https://datatracker.ietf.org/doc/html/rfc7636) (บังคับใช้ `code_challenge_method=S256`)
   - **RFC 6749 (OAuth 2.0):** [The OAuth 2.0 Authorization Framework](https://datatracker.ietf.org/doc/html/rfc6749) (กระบวนการ Authorization Code Flow, State CSRF Protection, และ Error Response เช่น `access_denied`, `invalid_grant`)
   - **RFC 7662 (Token Introspection):** [OAuth 2.0 Token Introspection](https://datatracker.ietf.org/doc/html/rfc7662) (`/api/v2/oauth2/introspect/`)
   - **RFC 7009 (Token Revocation):** [OAuth 2.0 Token Revocation](https://datatracker.ietf.org/doc/html/rfc7009) (`/api/v2/oauth2/revoke/`)
   - **OpenID Connect Core 1.0:** [OpenID Connect Core 1.0 incorporating errata set 1](https://openid.net/specs/openid-connect-core-1_0.html)

### 0.2 แหล่งอ้างอิงฝั่ง DOPA Check Card Service (กรมการปกครอง & สพร. DGA GDX)
1. **คู่มือและมาตรฐาน API กรมการปกครอง โดยสำนักงานพัฒนารัฐบาลดิจิทัล (สพร. / DGA):**
   - **แนวทางการพัฒนาระบบโดยใช้ API Laser Code:** [https://kb.dga.or.th/gdx/5api-lasercode/](https://kb.dga.or.th/gdx/5api-lasercode/)
   - **แนวทางการพัฒนาระบบโดยใช้ API สำหรับผู้พัฒนาหน่วยงาน:** [https://kb.dga.or.th/gdx/4api/](https://kb.dga.or.th/gdx/4api/)
   - **พจนานุกรมข้อมูลภาครัฐ (Data Dictionary Detail):** [https://gdx.dga.or.th/DataCatalog/Dictionary/Detail?id=4996ceda-33a8-49ad-9841-e169fc9ecc4a](https://gdx.dga.or.th/DataCatalog/Dictionary/Detail?id=4996ceda-33a8-49ad-9841-e169fc9ecc4a)
2. **มาตรฐานบัตรประจำตัวประชาชน สำนักบริหารการทะเบียน กรมการปกครอง (BORA):**
   - **มาตรฐานรหัสหลังบัตร (Laser Code Format):** [https://stat.bora.dopa.go.th/card/smartcard4_5.htm](https://stat.bora.dopa.go.th/card/smartcard4_5.htm) (ตัวอักษร 2 หลัก + ตัวเลข 10 หลัก)
   ข้อความจากเว็บ หน้าเว็บเป็นเว็บแบบเก่า เข้ารหัสด้วย window-874 (https://stat.bora.dopa.go.th/card/smartcard4_5.htm)
   บัตรประจำตัวประชาชนแบบอเนกประสงค์ (Smart card) รุ่นที่ 4-5

   บัตรประจำตัวประชาชนอเนกประสงค์ (Smart card) รุ่นที่ 4:
   จำนวน 3.9 ล้านใบ เริ่มออกบัตรตั้งแต่ปี 2555 มีพื้นที่จัดเก็บข้อมูล (EEPROM) เป็น 80 Kbytes และยังมีการจัดเก็บข้อมูลของส่วนราชการอื่นเหมือนกับรุ่นที่ 3 รูปแบบบัตรเหมือนกับรุ่นที่ 3 แต่เลขเลเซอร์หลังบัตรขึ้นต้นด้วย KN0-XXXXXXX-XX

   บัตรประจำตัวประชาชนอเนกประสงค์ (Smart card) รุ่นที่ 5:
   จำนวน 16 ล้านใบ เริ่มออกบัตรตั้งแต่ปี 2555 มีพื้นที่จัดเก็บข้อมูล (EEPROM) เป็น 80 Kbytes และยังมีการจัดเก็บข้อมูลของส่วนราชการอื่นเหมือนกับรุ่นที่ 3 รูปแบบบัตรเหมือนกับรุ่นที่ 3 แต่เลขเลเซอร์หลังบัตรขึ้นต้นด้วย JT0-XXXXXXX-XX

3. **พ.ร.บ. การทะเบียนราษฎร และมาตรฐาน ISO/IEC 7064:**
   - **สูตรคำนวณตรวจสอบเลขประจำตัวประชาชน 13 หลัก (Modulo 11 Check Digit Algorithm):** กฎการถ่วงน้ำหนักตำแหน่งที่ 1-12 เพื่อคำนวณเลขตรวจสอบหลักที่ 13
4. **มาตรฐานสถาปัตยกรรม High Availability & Fault Tolerance:**
   - **RFC 7231 (HTTP 503 Service Unavailable):** การตรวจจับและจัดการข้อผิดพลาดเมื่อ Gateway หรือ VPN ภาครัฐหยุดให้บริการชั่วคราว

### 0.3 ขอบเขตความจริงระหว่าง Mock Mode กับการขึ้นใช้งานจริง (Production Boundary)
1. **บทบาทของ Mock Mode:**
   - สร้างขึ้นเพื่อ**ปลดล็อกงานพัฒนาฝั่ง Client/Host และการทำ Unit Test** ให้ทีมสามารถดำเนินงานต่อได้ทันทีโดยไม่ต้องรอ Credential จากภาครัฐ
   - เป็นการจำลองพฤติกรรมตามข้อกำหนดมาตรฐานสากล (RFC/OIDC) และเอกสาร DGA
2. **สิ่งที่ยังต้องรอจาก DOPA ก่อนขึ้น Production จริง:**
   - ได้รับการขึ้นทะเบียนเป็น Relying Party และได้รับ `client_id`, `client_secret`, และ `api_key` ทางการ
   - ลงทะเบียน `redirect_uri` (HTTPS สาธารณะ) กับ DOPA
   - การยืนยันผลการอนุมัติ Scopes (สูงสุด 20 Scopes)
   - การยืนยันโครงสร้างฟิลด์ `userinfo` และระดับ `ial` (ค่า `ial: 2.3` ใน Mock เป็นค่าสมมติสำหรับการทดสอบตามมาตรฐาน ETDA/DOPA Level 2.3)
3. **การเข้าถึงข้อมูลที่อยู่ (Address Claim):**
   - ตามมาตรฐาน OIDC Core 1.0 ฟิลด์ `address` ถูกส่งคืนเป็น JSON Object / Dictionary
   - SDK มี Helper Method `result.GetFormattedAddress()` และ `data.GetFormattedAddress()` ให้ผู้พัฒนาดึงข้อความที่อยู่ภาษาไทย (`formatted` หรือ component fields) ได้ทันทีโดยไม่เกิดปัญหา Type Serialization

### 0.4 กระบวนการเปลี่ยนผ่านจาก "สมมติฐานที่มีหลักฐานรองรับ (Evidence-Based Hypothesis)" สู่ "การตรวจรับรองด้วยระบบจริง (Live System Verification)"
1. **มุมมองเชิงการตรวจรับและควบคุมคุณภาพ (Auditor's Perspective):**
   - จนกว่าจะได้รับ Credentials ทางการ, คู่มือสเปกฉบับลงนาม, และได้ยิงทดสอบจริงกับ Endpoint ของ DOPA ทุกอย่างยังถือเป็น "สมมติฐาน (Hypothesis)" ที่ต้องรอการตรวจรับรอง (Verification & Validation)
2. **มุมมองเชิงวิศวกรรมการพัฒนา (Engineering Stance):**
   - ทีมวิศวกรรมมีหน้าที่ "หนักแน่นบนหลักวิชาการและหลักฐานเชิงประจักษ์" โดยสืบค้นมาตรฐาน RFC, OpenID Foundation, และแกะรอยค่าจาก Live Discovery Endpoint จริง (`.well-known/openid-configuration`) เพื่อให้สมมติฐานใกล้เคียงความจริงที่สุด ไม่ใช่การคาดเดาอย่างไร้ทิศทาง
3. **แผนการเทียบยืนยัน (Verification Checklist เมื่อได้รับ Credentials & คู่มือ):**
   - **Step 1: Scope Audit** — ตรวจสอบว่าในเอกสารอนุมัติของ DOPA อนุญาต Scopes ตัวใดบ้าง นำมากำหนดใน Database (`kiosk_thaid_scope`)
   - **Step 2: UserInfo Field Mapping Verification** — นำก้อน JSON ที่ได้จาก `/api/v2/oauth2/userinfo/` ของจริงมาเทียบกับ Model `Data` ของ SDK และตรวจสอบฟิลด์ `address`, `house_address`, `ial`
   - **Step 3: Signature & JWKS Verification** — ตรวจสอบการถอดรหัสและตรวจ Signature ของ ID Token กับกุญแจจริงจาก `https://imauth.bora.dopa.go.th/jwks/`
   - **Step 4: Error Scenarios Verification** — ทดสอบการกดยกเลิกในแอป ThaID เพื่อยืนยันว่าได้ `access_denied` ตามข้อกำหนด RFC จริง
   - **Step 5: Zero-Code Tuning** — หากพบฟิลด์หรือเงื่อนไขใดที่ DOPA ปรับเปลี่ยน สามารถปรับแก้ผ่านตาราง Config หรืออัปเดต SDK ได้ทันทีโดยไม่กระทบสถาปัตยกรรมหลักของตู้ Kiosk

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

| รหัส Use Case | ชื่อสถานการณ์ (Scenario) | แหล่งที่มาและมาตรฐานอ้างอิง (Source & Standard) | วิธีทริกเกอร์ใน `UseMock = true` (ใน `.dll`) | วิธีทริกเกอร์บน `MockServer` (`localhost:5199`) | ผลลัพธ์ที่ได้จาก SDK (`VerificationResult`) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`UC-THAID-01`** | **Happy Path (ยืนยันตัวตนสำเร็จ + ครบ 20 Scopes)** | • [BORA ThaID OIDC Discovery](https://imauth.bora.dopa.go.th/.well-known/openid-configuration)<br>• [RFC 7636 (PKCE S256)](https://datatracker.ietf.org/doc/html/rfc7636)<br>• [RFC 6749 Sec 4.1](https://datatracker.ietf.org/doc/html/rfc6749#section-4.1) | `MockScenario = "success"` *(ค่าเริ่มต้น)* หรือส่ง `code = "mock-auth-code-001"` | กดปุ่ม `✅ ยินยอมสำเร็จ` หรือส่ง `?scenario=success` | `IsSuccess = true` พร้อม `Data` ครบทั้ง **20 Scopes** + `access_token`, `refresh_token`, `id_token`, `expires_in` |
| **`UC-THAID-02`** | **Invalid / Mismatched OAuth State (ป้องกัน CSRF)** | • [RFC 6749 Sec 10.12 (Cross-Site Request Forgery)](https://datatracker.ietf.org/doc/html/rfc6749#section-10.12)<br>• NIST SP 800-63C Guidelines | `MockScenario = "invalid_state"` หรือส่ง `expected_state` ไม่ตรงกับ `Request.State` | กดปุ่ม `⚠️ จำลองค่า State ไม่ตรง` หรือส่ง `?scenario=invalid_state` | `IsSuccess = false`, `ErrorCode = "INVALID_STATE"` (บล็อกทันทีโดยไม่แลก Token) |
| **`UC-THAID-03`** | **Expired / Invalid Authorization Code (`invalid_grant`)** | • [RFC 6749 Sec 5.2 (invalid_grant Error)](https://datatracker.ietf.org/doc/html/rfc6749#section-5.2)<br>• OpenID Connect Core 1.0 Sec 3.1.2.6 | `MockScenario = "expired_code"` หรือส่ง `code = "mock-expired-code"` | กดปุ่ม `⏰ จำลอง Code หมดอายุ` หรือส่ง `?scenario=expired_code` | `IsSuccess = false`, `ErrorCode = "TOKEN_EXCHANGE_FAILED"`, `HttpStatusCode = 400` |
| **`UC-THAID-04`** | **ThaID Server / Network Timeout** | • ISO/IEC 27001 Availability Control<br>• Microservices Resilience Pattern (Timeout) | `MockScenario = "timeout"` หรือส่ง `code = "mock-timeout"` | กดปุ่ม `🔌 จำลองโครงข่ายล่ม` หรือส่ง `?scenario=timeout` | `IsSuccess = false`, `ErrorCode = "NETWORK_ERROR"` |
| **`UC-THAID-05`** | **Token Introspection & Revocation** | • [RFC 7662 (Token Introspection)](https://datatracker.ietf.org/doc/html/rfc7662)<br>• [RFC 7009 (Token Revocation)](https://datatracker.ietf.org/doc/html/rfc7009)<br>• BORA `/api/v2/oauth2/introspect/` & `/revoke/` | เรียก `InspectTokenAsync(token)` หรือ `RevokeTokenAsync(token)` (ถ้าส่ง `"revoked-token"` จะได้ `active = false`) | เรียก `/api/v2/oauth2/introspect/` หรือ `/api/v2/oauth2/revoke/` | `IsSuccess = true`, `Data["active"] = true/false` และยกเลิก Token สำเร็จ |
| **`UC-THAID-06`** | **User Denied Consent (ประชาชนกดยกเลิกในแอป ThaID)** | • [RFC 6749 Sec 4.1.2.1 (access_denied)](https://datatracker.ietf.org/doc/html/rfc6749#section-4.1.2.1)<br>• ThaID Citizen Consent Cancellation Flow | `MockScenario = "deny"` (`GetAuthorizationUrl` คืน `?error=access_denied`) | กดปุ่ม `❌ ประชาชนกดยกเลิก` หรือส่ง `?scenario=deny` | Redirect กลับมาพร้อม `error=access_denied` (ไม่มี `code`) หากส่งเข้า `VerifyAsync` จะได้ `ErrorCode = "MISSING_AUTH_CODE"` |

### ตัวอย่างข้อมูล JSON ครบ 20 Scopes ที่ได้รับจาก `UC-THAID-01` (`VerificationResult.Data`)
```json
{
  "sub": "thaid-mock-sub-001",
  "pid": "1100501234567",
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

| รหัส Use Case | ชื่อสถานการณ์ (Scenario) | แหล่งที่มาและมาตรฐานอ้างอิง (Source & Standard) | เงื่อนไข Input ที่ใช้ทริกเกอร์ (ใช้ได้ทั้ง `UseMock = true` และ `MockServer`) | ผลลัพธ์ที่ได้จาก SDK (`VerificationResult`) |
| :--- | :--- | :--- | :--- | :--- |
| **`UC-DOPA-01`** | **Happy Path (สถานะบัตรปกติ)** | • [DGA GDX Laser API kb.dga.or.th](https://kb.dga.or.th/gdx/5api-lasercode/)<br>• [BORA Laser Code Stat](https://stat.bora.dopa.go.th/card/smartcard4_5.htm)<br>• BORA CheckCard By CID Spec | `pid` 13 หลักถูกต้องตามสูตร Modulo 11 (เช่น `"1234567890121"`) + `laserId` ปกติ (เช่น `"ME0123456789"`) หรือ `chipNo` ปกติ | `IsSuccess = true`, `Data["Code"] = 0`, `Data["Desc"] = "สถานะปกติ"`, `Data["IsError"] = false` |
| **`UC-DOPA-02`** | **Invalid PID Format / Checksum (เลขบัตรผิดหลัก Modulo 11)** | • พ.ร.บ. การทะเบียนราษฎร<br>• มาตรฐานสากล ISO/IEC 7064 (MOD 11, 10)<br>• Thai National ID Verification Formula | `pid` ไม่ครบ 13 หลัก หรือหลักที่ 13 คำนวณ Modulo 11 ไม่ตรง (เช่น `"1234567890122"`) | `IsSuccess = false`, `ErrorCode = "INVALID_PID_FORMAT"` (SDK บล็อกทันทีโดยไม่ยิง API) |
| **`UC-DOPA-03`** | **Missing Required Parameters (ส่งข้อมูลไม่ครบ)** | • ข้อกำหนดพารามิเตอร์ DGA API Specification<br>• BORA Gateway Input Schema Validation | ส่ง `firstName`, `lastName`, `birthDate`, `laserId`, หรือ `chipNo` เป็นค่าว่าง (`""` หรือ `null`) | `IsSuccess = false`, `ErrorCode = "MISSING_PARAMETERS"` (SDK บล็อกทันทีโดยไม่ยิง API) |
| **`UC-DOPA-04`** | **Card Verification Rejected (เลขหลังบัตรไม่ตรง / บัตรถูกยกเลิก)** | • DOPA Gateway Error Code Standard (`Code: 1`)<br>• DGA GDX Result Schema (`IsError: true`) | ส่ง `laserId = "ME9999999999"` หรือ `chipNo = "0000000000000000"` | `IsSuccess = false`, `ErrorCode = "CARD_VERIFICATION_REJECTED"`, พร้อม `Data["IsError"] = true`, `Data["Code"] = 1` |
| **`UC-DOPA-05`** | **DOPA Gateway Unavailable (HTTP 503 Service Unavailable)** | • [RFC 7231 Sec 6.6.4 (HTTP 503)](https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4)<br>• DOPA Government Gateway High-Availability SLA | ส่ง `laserId = "ME5000000000"` หรือ `chipNo = "5000000000000000"` | `IsSuccess = false`, `ErrorCode = "DOPA_API_FAILED"`, `HttpStatusCode = 503` |
| **`UC-DOPA-06`** | **Network / VPN Connection Timeout** | • ISO/IEC 27001 Secure Gateway SLA<br>• Government Intranet VPN Connectivity Timeout | ส่ง `laserId = "METIMEOUT000"` หรือ `chipNo = "TIMEOUT000000000"` | `IsSuccess = false`, `ErrorCode = "NETWORK_ERROR"` |

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
