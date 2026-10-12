# เอกสารอ้างอิงการเชื่อมต่อ ThaID (`ThaID`)

- **ProviderName:** `"ThaID"`
- **Config Section:** `"IdentityVerification:ThaId"`
- **Interface:** `Netka.Identity.Core.Interfaces.IThaIdProvider` (สืบทอดจาก `IVerificationProvider`)
- **Implementation:** `Netka.Identity.Infrastructure.Providers.ThaIdProvider`
- **มาตรฐานการเชื่อมต่อ:** OAuth 2.0 Authorization Code Flow + PKCE (`S256`) + HTTP Basic Authentication (`ClientId:ClientSecret`)
- **OpenID Connect Discovery (Live URL):** [https://imauth.bora.dopa.go.th/.well-known/openid-configuration](https://imauth.bora.dopa.go.th/.well-known/openid-configuration)
- **JWKS Endpoint (Live URL):** [https://imauth.bora.dopa.go.th/jwks/](https://imauth.bora.dopa.go.th/jwks/)

> **หมายเหตุ:** เอกสารคู่มือฉบับเต็มของกรมการปกครอง (BORA) จะแจกจ่ายให้เฉพาะหน่วยงานที่ลงทะเบียน Relying Party (RP) เท่านั้น ส่วนลิงก์ `/dev/doc/` เดิมถูกปิดไปแล้ว (`404 Not Found`) ดังนั้นจึงบันทึกข้อมูลโครงสร้างจริงจาก `.well-known/openid-configuration` ของกรมการปกครองไว้ในไฟล์นี้โดยตรง

---

## 1. ข้อมูลจริงจาก OpenID Configuration ของ ThaID

- **Issuer:** `https://imauth.bora.dopa.go.th`
- **Authorization Endpoint:** `https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/`
- **Token Endpoint:** `https://imauth.bora.dopa.go.th/api/v2/oauth2/token/`
- **UserInfo Endpoint:** `https://imauth.bora.dopa.go.th/api/v2/oauth2/userinfo/`
- **Revocation Endpoint:** `https://imauth.bora.dopa.go.th/api/v2/oauth2/revoke/`
- **Introspect / Inspect Endpoint:** `https://imauth.bora.dopa.go.th/api/v2/oauth2/introspect/`
- **Grant Types Supported:** `authorization_code`, `refresh_token`
- **Token Endpoint Auth Methods:** `client_secret_basic`, `client_secret_post`
- **ID Token Signing Algorithm:** `ES256`
- **Scopes ที่รองรับทั้งหมด (20 Scopes จริงจากระบบ ThaID):**
  - `openid`, `pid`, `address`, `gender`, `birthdate`
  - `given_name`, `middle_name`, `family_name`, `name`
  - `given_name_en`, `middle_name_en`, `family_name_en`, `name_en`
  - `title`, `title_en`, `ial`, `smartcard_code`
  - `date_of_expiry`, `date_of_issuance`, `house_address`

---

## 2. การตั้งค่า (`appsettings.json`)

```json
{
  "IdentityVerification": {
    "EncryptionKey": "<BASE64_32_BYTE_AES_KEY>",
    "ThaId": {
      "ClientId": "<YOUR_THAID_CLIENT_ID>",
      "ClientSecret": "<YOUR_THAID_CLIENT_SECRET>",
      "AuthorizationUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/",
      "TokenUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/token/",
      "InspectUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/introspect/",
      "RevokeUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/revoke/",
      "UserInfoUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/userinfo/",
      "RedirectUri": "https://your-domain.com/api/identity/thaid/callback",
      "Scope": "openid pid given_name family_name birthdate",
      "AllowUntrustedCertificates": false
    }
  }
}
```
> **หมายเหตุ:** `ClientId` และ `ClientSecret` เป็นค่าบังคับ (Required) ส่วน URL Endpoints และ `Scope` มีค่า Default เตรียมไว้ให้ใน `ThaIdProvider` แล้ว สามารถละไว้ได้หากใช้ Endpoint มาตรฐาน

---

## 3. เมธอดและการรับส่งข้อมูล (`IThaIdProvider`)

### 3.1 สร้าง Authorization URL (`GetAuthorizationUrl`)
```csharp
string codeVerifier = PkceHelper.GenerateCodeVerifier();
string codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);
string state = Guid.NewGuid().ToString("N");

string authUrl = _thaIdProvider.GetAuthorizationUrl(
    redirectUri: "https://your-domain.com/api/identity/thaid/callback",
    state: state,
    codeChallenge: codeChallenge,
    codeChallengeMethod: "S256",
    nonce: null);
```

### 3.2 แลก Authorization Code เป็น Token (`ExchangeCodeAsync` หรือ `VerifyAsync`)
- **เรียกตรงผ่าน `ExchangeCodeAsync`:**
  ```csharp
  VerificationResult result = await _thaIdProvider.ExchangeCodeAsync(
      authorizationCode: code,
      redirectUri: "https://your-domain.com/api/identity/thaid/callback",
      codeVerifier: codeVerifier,
      cancellationToken: ct);
  ```
- **หรือเรียกผ่าน `VerifyAsync(VerificationRequest)`:**
  - `request.AuthorizationCode`: ค่า `code` จาก Callback (**Required**)
  - `request.RedirectUri`: ค่า Callback URI (หรือใช้ค่าจาก Config `RedirectUri`)
  - `request.State`: ค่า `state` ที่รับกลับมา
  - `request.AdditionalParameters["expected_state"]`: ค่า `state` ที่คาดหวัง (หากส่งมา SDK จะตรวจเทียบป้องกัน CSRF ให้ทันที)
  - `request.AdditionalParameters["code_verifier"]`: ค่า PKCE `code_verifier`

### 3.3 เมธอดจัดการ Token และดึงข้อมูลผู้ใช้เพิ่มเติม
- **`GetUserInfoAsync(string accessToken, CancellationToken ct)`:** ดึงข้อมูลโปรไฟล์จาก `/api/v2/oauth2/userinfo/` ด้วย `Bearer {accessToken}`
- **`RefreshTokenAsync(string refreshToken, CancellationToken ct)`:** ขอ Access Token ใหม่ด้วย `grant_type=refresh_token`
- **`InspectTokenAsync(string accessToken, CancellationToken ct)`:** ตรวจสอบสถานะ Token
- **`RevokeTokenAsync(string token, CancellationToken ct)`:** ยกเลิก Token

---

## 4. ข้อมูลขาออก (`VerificationResult.Data` — Pass-Through Keys)

SDK ส่งค่า JSON ดิบจาก ThaID กลับมาใน `result.Data` (ค้นหา Key แบบไม่สนตัวพิมพ์เล็ก-ใหญ่ `OrdinalIgnoreCase` หรือใช้ `result.Data.GetStringValue("key")`):

| ชื่อ Key ใน `result.Data` | Endpoint ที่คืนค่า | คำอธิบาย |
| :--- | :--- | :--- |
| `"access_token"` | `ExchangeCodeAsync`, `RefreshTokenAsync` | OAuth 2.0 Access Token |
| `"refresh_token"` | `ExchangeCodeAsync`, `RefreshTokenAsync` | OAuth 2.0 Refresh Token |
| `"id_token"` | `ExchangeCodeAsync` | OIDC JWT ID Token (ลงนามด้วย `ES256`) |
| `"token_type"` | `ExchangeCodeAsync`, `RefreshTokenAsync` | เช่น `"Bearer"` |
| `"expires_in"` | `ExchangeCodeAsync`, `RefreshTokenAsync` | อายุของ Token (วินาที) |
| `"scope"` | `ExchangeCodeAsync`, `InspectTokenAsync` | รายการ Scope ที่ได้รับอนุมัติ |
| `"pid"` | `ExchangeCodeAsync`, `GetUserInfoAsync` | เลขประจำตัวประชาชน 13 หลัก |
| `"given_name"`, `"middle_name"`, `"family_name"`, `"name"` | `GetUserInfoAsync` / `ExchangeCodeAsync` | ชื่อ-นามสกุลภาษาไทย |
| `"given_name_en"`, `"middle_name_en"`, `"family_name_en"`, `"name_en"` | `GetUserInfoAsync` / `ExchangeCodeAsync` | ชื่อ-นามสกุลภาษาอังกฤษ |
| `"title"`, `"title_en"`, `"gender"`, `"birthdate"` | `GetUserInfoAsync` / `ExchangeCodeAsync` | คำนำหน้าชื่อ, เพศ, วันเกิด |
| `"address"`, `"house_address"`, `"smartcard_code"`, `"date_of_issuance"`, `"date_of_expiry"`, `"ial"` | `GetUserInfoAsync` / `ExchangeCodeAsync` | ที่อยู่ตามทะเบียนบ้าน, รหัสบัตร, วันออกบัตร/หมดอายุ, ระดับ IAL |
| `"active"` | `InspectTokenAsync` | สถานะของ Token (`true` / `false`) |

---

## 5. รหัสข้อผิดพลาด (`result.ErrorCode`)
- `MISSING_AUTH_CODE`: ไม่ได้ส่ง `authorizationCode`
- `MISSING_REDIRECT_URI`: ไม่ได้ระบุ `redirectUri` ทั้งในพารามิเตอร์และใน Config
- `INVALID_STATE`: ค่า `request.State` ไม่ตรงกับ `expected_state`
- `TOKEN_EXCHANGE_FAILED` / `TOKEN_REFRESH_FAILED` / `TOKEN_INSPECT_FAILED` / `TOKEN_REVOKE_FAILED` / `USERINFO_FETCH_FAILED`: ThaID ตอบกลับด้วย HTTP Non-2xx (ดู HTTP Status Code ได้จาก `result.Error?.HttpStatusCode`)
- `INVALID_THAID_RESPONSE` / `INVALID_USERINFO_RESPONSE`: JSON ที่ได้รับไม่ถูกต้อง
- `NETWORK_ERROR`: เกิดข้อผิดพลาดในการเชื่อมต่อโครงข่าย (`HttpRequestException`)
