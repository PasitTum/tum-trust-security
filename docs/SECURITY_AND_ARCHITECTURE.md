# สถาปัตยกรรม ความปลอดภัย PDPA และตารางรหัสข้อผิดพลาด (Architecture, Security & Error Reference)
**โปรเจกต์:** `TUM.Identity.SDK`  

---

## 1. สถาปัตยกรรมระบบ (Clean Architecture & Pass-Through Design)

`TUM.Identity.SDK` แบ่งโครงสร้างออกเป็น 3 เลเยอร์หลักภายใน Assembly เดียว (`TUM.Identity.SDK.dll`) เพื่อให้สะดวกต่อการแจกจ่ายแบบ Single-DLL:

```text
┌───────────────────────────────────────────────────────────────────────────┐
│                     Host Project (Kiosk / Web API)                        │
│        Controllers / Minimal APIs / Services / EF Core DbContext          │
└─────────────────────────────────────┬─────────────────────────────────────┘
                                      │ References libs/TUM.Identity.SDK.dll
                                      ▼
┌───────────────────────────────────────────────────────────────────────────┐
│ 1. Application Layer (TUM.Identity.Application)                         │
│    - DependencyInjection.AddIdentityVerification() (4 Overloads)          │
│    - IdentityResilienceHandler (Built-in Retry + Circuit Breaker)         │
├───────────────────────────────────────────────────────────────────────────┤
│ 2. Core Layer (TUM.Identity.Core)                                       │
│    - Interfaces: IVerificationProvider, IThaIdProvider, IDopaProvider,    │
│                  IEncryptionService, IIdentityOptionsProvider             │
│    - Models: VerificationRequest, VerificationResult, VerificationError,  │
│              IdentityVerificationOptions, ThaIdOptions, DopaOptions       │
├───────────────────────────────────────────────────────────────────────────┤
│ 3. Infrastructure Layer (TUM.Identity.Infrastructure)                   │
│    - Providers: ThaIdProvider, DopaProvider (Live HTTP + Built-in Mock)   │
│    - Security: AesEncryptionService (AES-256-CBC + HMAC-SHA256), PkceHelper│
│    - Helpers: ProviderHelper (Modulo 11 PID Validator, PII Vault Logger)  │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## 2. มาตรฐานความปลอดภัยและการคุ้มครองข้อมูลส่วนบุคคล (PDPA & Security)

### 2.1 การเข้ารหัสข้อมูลส่วนบุคคล (AES-256-CBC + HMAC-SHA256 Encrypt-then-MAC)
- ข้อมูลเลขประจำตัวประชาชน 13 หลัก (`pid`, `PID`, `citizen_id`) จะ**ไม่ถูกบันทึกลง Log เป็น Plaintext เด็ดขาด**
- ทุกครั้งที่มีการบันทึก Log ผ่าน `ProviderHelper.LogEncryptedPiiIfPresent(...)`:
  1. เลขบัตรประชาชนจะถูกแปลงเป็นรูปแบบ Masked (`11005*****561`) สำหรับการตรวจสอบเบื้องต้น
  2. ข้อมูลจริงจะถูกเข้ารหัสด้วย `AesEncryptionService` ซึ่งใช้คีย์ขนาด 256-bit (32 bytes) สุ่ม `IV` ใหม่ทุกครั้ง และคำนวณ `HMAC-SHA256` เพื่อป้องกันการดัดแปลงข้อมูล (Tamper Protection) ก่อนบันทึกลง `[PII-VAULT]`

### 2.2 มาตรฐาน PKCE (RFC 7636) และ CSRF State Validation
- `PkceHelper.GenerateCodeVerifier()` สร้างรหัสสุ่มขนาด 32 bytes ด้วย `RandomNumberGenerator` (Cryptographically Secure)
- `PkceHelper.GenerateCodeChallenge(verifier)` คำนวณ SHA-256 และแปลงเป็น Base64Url ตามมาตรฐาน RFC 7636 (`S256`)
- `ThaIdProvider.VerifyAsync` ตรวจสอบค่า `expected_state` เทียบกับ `request.State` แบบ Ordinal ก่อนอนุญาตให้แลก Token เสมอ

### 2.3 ระบบความทนทานต่อโครงข่ายล่มในตัว (`IdentityResilienceHandler`)
เพื่อให้ `TUM.Identity.SDK.dll` ไม่มี Dependency ภายนอก (`Zero Third-Party DLLs`) ตัว SDK จึงมี `IdentityResilienceHandler : DelegatingHandler` ฝังอยู่ในตัว:
- **Exponential Backoff Retry (3 ครั้ง):** เมื่อเจอ HTTP `5xx`, `408 Request Timeout`, `429 Too Many Requests` หรือ `HttpRequestException` ระบบจะลองใหม่อัตโนมัติที่ระยะเวลา `400ms`, `800ms`, และ `1600ms`
- **Circuit Breaker (ตัดวงจรอัตโนมัติ):** หากเกิดความล้มเหลวระดับโครงข่ายติดต่อกันครบ **5 ครั้ง** ระบบจะเปิดวงจร (Open Circuit) เป็นเวลา **30 วินาที** เพื่อไม่ให้ตู้ Kiosk ค้างรอ Timeout ซ้ำซ้อน

---

## 3. ตารางรหัสข้อผิดพลาดทั้งหมดของ SDK (`ErrorCode` Reference Catalog)

ทุกเมธอดของ `IThaIdProvider` และ `IDopaProvider` จะคืนค่า `VerificationResult` โดยไม่โยน Unhandled Exception ออกมาทำเว็บล่ม หากเกิดข้อผิดพลาดจะมี `IsSuccess = false` และระบุรหัส `ErrorCode` มาตรฐานดังนี้:

### 3.1 รหัสข้อผิดพลาดทั่วไปและการตั้งค่า (Common & Configuration Errors)
| `ErrorCode` | ความหมายและสาเหตุ | แนวทางแก้ไข |
| :--- | :--- | :--- |
| **`MISSING_CONFIGURATION`** | ยังไม่ได้ตั้งค่า `ClientId`, `ClientSecret`, หรือ `LaserUrl`/`ChipUrl` ใน Database หรือ `appsettings.json` (และไม่ได้เปิด `UseMock = true`) | ตั้งค่า Config ใน Database / `appsettings.json` หรือเปิด `UseMock = true` สำหรับโหมดทดสอบ |
| **`NETWORK_ERROR`** | ไม่สามารถเชื่อมต่อเซิร์ฟเวอร์ปลายทางได้ (DNS ไม่เจอ, เน็ตหลุด, VPN กรมการปกครองตัด, หรือ Timeout) | ตรวจสอบการเชื่อมต่อเครือข่าย / VPN หรือสถานะเซิร์ฟเวอร์ต้นทาง |
| **`INTERNAL_ERROR`** | เกิดข้อผิดพลาดไม่คาดคิดภายในระบบ | ตรวจสอบ Stack Trace ใน Application Log |

### 3.2 รหัสข้อผิดพลาดของ `IThaIdProvider` (DOPA ThaID)
| `ErrorCode` | ความหมายและสาเหตุ | `HttpStatusCode` |
| :--- | :--- | :---: |
| **`INVALID_STATE`** | ค่า `state` ที่ได้รับจาก Callback ไม่ตรงกับ `expected_state` ที่สร้างไว้ตอนแรก (อาจเกิดจาก CSRF หรือ Session หมดอายุ) | `null` |
| **`MISSING_AUTH_CODE`** | ไม่พบค่า `code` (เช่น ประชาชนกดยกเลิก `error=access_denied` ในแอป ThaID) | `null` |
| **`MISSING_REDIRECT_URI`** | ไม่ได้ระบุ `redirectUri` ทั้งในพารามิเตอร์และใน Configuration | `null` |
| **`MISSING_REFRESH_TOKEN`** | เรียก `RefreshTokenAsync` โดยส่งค่าว่าง | `null` |
| **`MISSING_TOKEN`** | เรียก `InspectTokenAsync` หรือ `RevokeTokenAsync` โดยส่งค่าว่าง | `null` |
| **`MISSING_ACCESS_TOKEN`** | เรียก `GetUserInfoAsync` โดยส่ง `accessToken` เป็นค่าว่าง | `null` |
| **`TOKEN_EXCHANGE_FAILED`** | การแลก `authorization_code` ล้มเหลว (เช่น Code หมดอายุ, ใช้ซ้ำ, หรือ `code_verifier` ไม่ตรง) | `400` / `401` |
| **`TOKEN_REFRESH_FAILED`** | การต่ออายุด้วย `refresh_token` ล้มเหลว | `400` / `401` |
| **`TOKEN_INSPECT_FAILED`** | การเรียก `/introspect/` ล้มเหลว | `400` / `401` |
| **`TOKEN_REVOKE_FAILED`** | การเรียก `/revoke/` ล้มเหลว | `400` / `401` |
| **`USERINFO_FETCH_FAILED`** | การดึงข้อมูล `/userinfo/` ด้วย Access Token ล้มเหลว | `401` / `403` |
| **`INVALID_THAID_RESPONSE`** | เซิร์ฟเวอร์ต้นทางตอบกลับไม่ใช่รูปแบบ JSON ที่ถูกต้อง | `null` |

### 3.3 รหัสข้อผิดพลาดของ `IDopaProvider` (DOPA Check Card Service)
| `ErrorCode` | ความหมายและสาเหตุ | `HttpStatusCode` |
| :--- | :--- | :---: |
| **`INVALID_PID_FORMAT`** | เลขประจำตัวประชาชน (`pid`) ไม่ใช่ตัวเลข 13 หลัก หรือคำนวณหลักที่ 13 ตามสูตร Modulo 11 ไม่ถูกต้อง | `null` |
| **`MISSING_PARAMETERS`** | ส่งพารามิเตอร์ที่จำเป็น (`firstName`, `lastName`, `birthDate`, `laserId`, หรือ `chipNo`) เป็นค่าว่าง | `null` |
| **`CARD_VERIFICATION_REJECTED`** | กรมการปกครองตอบกลับว่า `IsError: true` (เช่น เลขหลังบัตรไม่ตรงกับหน้าบัตร, บัตรหมดอายุ/ถูกยกเลิก, หรือสถานะบัตรไม่ปกติ) โดยดูรายละเอียดเพิ่มได้ใน `result.Data` | `200` |
| **`DOPA_API_FAILED`** | เซิร์ฟเวอร์ DOPA Gateway ตอบกลับ HTTP Error (เช่น `500`, `502`, `503 Service Unavailable`) | `500` / `503` |
| **`INVALID_DOPA_RESPONSE`** | เซิร์ฟเวอร์ DOPA ตอบกลับไม่ใช่รูปแบบ JSON ที่ถูกต้อง | `null` |

---

## 4. แผนงานปรับปรุงสถาปัตยกรรมในอนาคต (Future Architectural Roadmap: Secret Management)

### 4.1 ข้อสังเกตและช่องโหว่ความเสี่ยงในปัจจุบัน (Current Pain Points & Security Risks)
ปัจจุบันตู้ Kiosk มีการประกาศตัวแปรการตั้งค่าในตาราง `variables` แบบ Flat Keys ถึง 36 ตัวแปร ซึ่งก่อให้เกิดความเสี่ยงและภาระการดูแลระบบดังนี้:
1. **ความเสี่ยงข้อมูลความลับรั่วไหล (Plaintext Secrets in Database):**
   - ค่าความลับระดับสูงสุด เช่น `kiosk_thaid_client_secret`, `kiosk_thaid_api_key`, `kiosk_dopa_consumer_secret` ถูกบันทึกเป็น Plaintext อยู่ในฐานข้อมูล
   - หากฐานข้อมูลถูกโจมตี (SQL Injection, Database Backup หลุด หรือ Harddisk ประจำตู้ถูกถอด) ผู้โจมตีจะได้กุญแจลับไปสวมรอยเรียกข้อมูลประชาชนได้ทันที
2. **ความซับซ้อนในการจัดการ (Configuration Sprawl):**
   - ตัวแปร 36 ตัวแปรมีความกระจัดกระจาย ตรวจสอบความถูกต้องได้ยาก และเสี่ยงต่อการสะกดชื่อผิด (Typo)

### 4.2 สถาปัตยกรรมเป้าหมายที่แนะนำ (Recommended Target Architecture)
1. **การแยก Configuration ทั่วไป ออกจาก Secrets (Principle of Least Privilege):**
   - **Non-sensitive Config (เก็บบน DB ได้):** เช่น `redirect_uri`, `scope`, `face_threshold`, `use_mock`
     * รวมเป็น **JSON Config Block** เหลือเพียง 2 แถวใน DB: `kiosk_thaid_config` และ `kiosk_dopa_config`
     * สามารถ Deserialize เข้า Object ของ SDK ได้ในบรรทัดเดียว
   - **Sensitive Secrets (ห้ามเก็บบน DB เป็น Plaintext เด็ดขาด):**
     * **แนวทางที่ 1 (Best Practice):** ย้ายไปเก็บใน **OS Environment Variables** หรือ **Windows DPAPI / Machine KeyStore** ของตัวตู้ Kiosk ซึ่งเข้าถึงได้เฉพาะบัญชี Service ของระบบเท่านั้น
     * **แนวทางที่ 2 (Encrypted at Rest):** หากจำเป็นต้องเก็บบน DB เพื่อให้หน้า Web Admin แก้ไขได้ ต้องบังคับเข้ารหัสด้วย `AES-256-GCM` โดยใช้กุญแจลับ (`identity_encryption_key`) ที่ซ่อนอยู่นอกฐานข้อมูลเสมอ
2. **Convention Over Configuration (การลดตัวแปรที่ไม่จำเป็น):**
   - Endpoint มาตรฐานของ ThaID (`authorize_url`, `token_url`, `userinfo_url`, `jwks_url`) สามารถอ่านอัตโนมัติผ่าน Discovery URL (`.well-known/openid-configuration`) หรือกำหนดเป็น Built-in Default ใน SDK
   - ช่วยลดตัวแปรที่ต้องตั้งค่าในตู้ Kiosk จาก 36 ตัว เหลือเพียงไม่เกิน 4-5 ตัวที่จำเป็นจริงๆ เท่านั้น
