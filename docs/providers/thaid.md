# เอกสารอ้างอิงการเชื่อมต่อ ThaID (`ThaID`)

- **Provider Key:** `"ThaID"`
- **Interface:** `Netka.Identity.Core.Interfaces.IThaIdProvider` (สืบทอดจาก `IIdentityVerificationProvider`)
- **Implementation:** `Netka.Identity.Infrastructure.Providers.ThaIdProvider`
- **มาตรฐานการเชื่อมต่อ:** OAuth 2.0 Authorization Code Flow + PKCE (`S256`)
- **เอกสารต้นฉบับ:** [ThaID Developer Docs](https://imauth.bora.dopa.go.th/dev/doc/)

---

## 1. การตั้งค่า (`appsettings.json`)

```json
{
  "IdentityVerification": {
    "Providers": {
      "ThaID": {
        "ClientId": "<YOUR_CLIENT_ID>",
        "ClientSecret": "<YOUR_CLIENT_SECRET>",
        "ApiKey": "<YOUR_API_KEY_IF_APPLICABLE>",
        "CallbackUrl": "https://your-domain.com/api/identity/thaid/callback",
        "AuthorizeEndpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/",
        "TokenEndpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/token/",
        "RevokeEndpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/revoke/",
        "IntrospectEndpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/introspect/",
        "Scope": "pid given_name family_name birthdate",
        "TimeoutSeconds": 30
      }
    }
  }
}
```

---

## 2. การรับส่งข้อมูล (`IThaIdProvider`)

### 2.1 สร้าง Authorization URL (`GenerateAuthorizationUrlAsync`)
- **พารามิเตอร์ขาเข้า:**
  - `state` (Optional): ค่าป้องกัน CSRF (หากไม่ส่งมา SDK จะสุ่มค่า Cryptographic 32-byte Base64URL ให้อัตโนมัติ)
  - `codeChallenge` (Optional): ค่า PKCE Challenge (แนะนำให้ใช้ `PkceHelper.GeneratePkcePair()` สร้างคู่ `CodeVerifier` และ `CodeChallenge` แล้วเก็บ `CodeVerifier` ไว้ใน Session/Cache ผูกกับ `State`)
- **ผลลัพธ์ (`AuthorizationUrlResult`):**
  - `Url`: URL เต็มสำหรับ Redirect ผู้ใช้ไปหน้า Login ของ ThaID
  - `State`: ค่า State ที่ใช้ผูกกับ Session
  - `CodeVerifier`: ค่า Code Verifier (จะมีค่าเมื่อให้ SDK สร้างให้อัตโนมัติ)

### 2.2 แลก Authorization Code เป็นข้อมูลยืนยันตัวตน (`VerifyAsync`)
- **พารามิเตอร์ขาเข้า (`VerificationRequest`):**
  - `AuthorizationCode` (**Required**): ค่า `code` ที่ได้รับจาก ThaID Callback
  - `CodeVerifier` (**Required**): ค่า PKCE `code_verifier` ที่ตรงกับ `code_challenge` ตอนสร้าง URL
  - `AdditionalParameters["redirect_uri"]` (Optional): หากต้องการ Override ค่า `CallbackUrl` ใน Config
- **ผลลัพธ์ (`VerificationResult` — Pass-Through Dictionary ใน `result.Data`):**
  - `result.IsSuccess`: `true` เมื่อ HTTP Status 2xx และไม่มีฟิลด์ `"error"` ใน JSON
  - `result.Data` (ค้นหา Key แบบไม่สนตัวพิมพ์เล็ก-ใหญ่ `OrdinalIgnoreCase`):
    - `"access_token"`: OAuth 2.0 Access Token
    - `"token_type"`: เช่น `"Bearer"`
    - `"expires_in"`: อายุ Token (วินาที)
    - `"scope"`: รายการ Scope ที่ได้รับอนุมัติ
    - `"id_token"`: JWT ID Token
    - `"pid"`: เลขประจำตัวประชาชน 13 หลัก (ขึ้นอยู่กับ Scope ที่ขอ)
    - `"given_name"`, `"family_name"`, `"birthdate"`, `"titleTh"`, `"titleEn"` (ขึ้นอยู่กับ Scope ที่ขอในโครงการ ThaID)

### 2.3 การตรวจสอบสถานะและยกเลิก Token
- **`IntrospectTokenAsync(string token)`:** ส่งค่า JSON ดิบจาก Endpoint `/oauth2/introspect/` กลับมาใน `result.Data` (เช่น `result.Data["active"]`)
- **`RevokeTokenAsync(string token)`:** ยกเลิก Token ที่ Endpoint `/oauth2/revoke/`
