# เอกสารอ้างอิงการเชื่อมต่อ ThaID (`ThaID`)

- **ProviderName:** `"ThaID"`
- **Config Section:** `"IdentityVerification:ThaId"`
- **Interface:** `Netka.Identity.Core.Interfaces.IThaIdProvider` (สืบทอดจาก `IVerificationProvider`)
- **Implementation:** `Netka.Identity.Infrastructure.Providers.ThaIdProvider`
- **มาตรฐานการเชื่อมต่อ:** OAuth 2.0 Authorization Code Flow + PKCE (`S256`) + HTTP Basic Authentication (`ClientId:ClientSecret`) + Optional API Gateway Header (`x-api-key`)
- **OpenID Connect Discovery (Live URL):** [https://imauth.bora.dopa.go.th/.well-known/openid-configuration](https://imauth.bora.dopa.go.th/.well-known/openid-configuration)
- **JWKS Endpoint (Live URL):** [https://imauth.bora.dopa.go.th/jwks/](https://imauth.bora.dopa.go.th/jwks/)

> **หมายเหตุ:** เอกสารคู่มือฉบับเต็มของกรมการปกครอง (BORA) จะแจกจ่ายให้เฉพาะหน่วยงานที่ลงทะเบียน Relying Party (RP) เท่านั้น ส่วนลิงก์ `/dev/doc/` เดิมถูกปิดไปแล้ว (`404 Not Found`) ดังนั้นจึงบันทึกข้อมูลโครงสร้างจริงจาก `.well-known/openid-configuration` ของกรมการปกครองไว้ในไฟล์นี้โดยตรง

> ⚠️ **กฎความปลอดภัยเคร่งครัด (Strict Security Rule — ห้ามใส่ Key หรือ Secret จริงลงในเอกสารเด็ดขาด):**
> - **ห้ามใส่ API Key, Client Secret, Encryption Key หรือ Secret จริงใดๆ ลงในไฟล์เอกสาร `docs/providers/*.md` หรือ `docs/providers/*.http` โดยเด็ดขาด!**
> - ในเอกสารและไฟล์ตัวอย่างทั้งหมด **ต้องใช้ Placeholder เสมอ** เช่น `<YOUR_THAID_CLIENT_ID>`, `<YOUR_THAID_CLIENT_SECRET>`, หรือ `<BASE64_32_BYTE_AES_KEY>` เท่านั้น
> - ข้อมูล Secret จริงต้องถูกจัดเก็บใน Secure Environment Variables, Secret Manager, หรือ Key Vault ประจำแต่ละสภาพแวดล้อมระบบเท่านั้น

---

## 1. ข้อมูลจริงจาก OpenID Configuration ของ ThaID

- **Issuer:** `https://imauth.bora.dopa.go.th`
- **Discovery Endpoint:** `https://imauth.bora.dopa.go.th/.well-known/openid-configuration`
- **JWKS Endpoint:** `https://imauth.bora.dopa.go.th/jwks/`
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

## 2. การตั้งค่า (`appsettings.json` หรือ Database)

### 2.1 การตั้งค่าผ่าน `appsettings.json`
```json
{
  "IdentityVerification": {
    "EncryptionKey": "<BASE64_32_BYTE_AES_KEY>",
    "ThaId": {
      "UseMock": false,
      "MockScenario": "success",
      "ClientId": "<YOUR_THAID_CLIENT_ID>",
      "ClientSecret": "<YOUR_THAID_CLIENT_SECRET>",
      "AuthorizationUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/",
      "TokenUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/token/",
      "InspectUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/introspect/",
      "RevokeUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/revoke/",
      "UserInfoUrl": "https://imauth.bora.dopa.go.th/api/v2/oauth2/userinfo/",
      "DiscoveryUrl": "https://imauth.bora.dopa.go.th/.well-known/openid-configuration",
      "JwksUrl": "https://imauth.bora.dopa.go.th/jwks/",
      "Issuer": "https://imauth.bora.dopa.go.th",
      "ApiKey": "",
      "ApiKeyHeader": "x-api-key",
      "RedirectUri": "https://your-domain.com/api/identity/thaid/callback",
      "Scope": "openid pid given_name family_name birthdate",
      "AllowUntrustedCertificates": false
    }
  }
}
```
> **หมายเหตุ:** `ClientId` และ `ClientSecret` เป็นค่าบังคับ (Required เมื่อ `UseMock = false`) ส่วน URL Endpoints และ `Scope` มีค่า Default เตรียมไว้ให้ใน `ThaIdProvider` แล้ว สามารถละไว้ได้หากใช้ Endpoint มาตรฐาน (รองรับชื่อ Key ทั้ง `AuthorizeUrl` และ `AuthorizationUrl`)

### 2.2 การตั้งค่าจาก Database (`ThaIdOptions` ใน `IdentityVerificationOptions`)
หากโปรเจคของคุณเก็บค่าตั้งต้นไว้ในตารางฐานข้อมูล สามารถส่งค่าผ่าน `AddIdentityVerification(sp => ...)` ได้โดยตรง:
```csharp
builder.Services.AddIdentityVerification(sp =>
{
    using var scope = sp.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MyAppDbContext>();
    var cfg = db.SystemConfigs.AsNoTracking().First();

    return new IdentityVerificationOptions
    {
        EncryptionKey = cfg.EncryptionKey,
        ThaId = new ThaIdOptions
        {
            UseMock = cfg.ThaIdUseMock,
            MockScenario = cfg.ThaIdMockScenario, // "success", "deny", "invalid_state", "expired_code", "timeout"
            ClientId = cfg.ThaIdClientId,
            ClientSecret = cfg.ThaIdClientSecret,
            AuthorizeUrl = cfg.ThaIdAuthorizeUrl,
            TokenUrl = cfg.ThaIdTokenUrl,
            InspectUrl = cfg.ThaIdInspectUrl,
            RevokeUrl = cfg.ThaIdRevokeUrl,
            UserInfoUrl = cfg.ThaIdUserInfoUrl,
            DiscoveryUrl = cfg.ThaIdDiscoveryUrl,
            JwksUrl = cfg.ThaIdJwksUrl,
            Issuer = cfg.ThaIdIssuer,
            ApiKey = cfg.ThaIdApiKey,
            ApiKeyHeader = cfg.ThaIdApiKeyHeader ?? "x-api-key",
            RedirectUri = cfg.ThaIdRedirectUri,
            Scope = cfg.ThaIdScope
        }
    };
});
```

### 2.3 โหมดจำลองในตัว (`UseMock = true` — ไม่ต้องรัน MockServer แยก)
เมื่อตั้งค่า `ThaId.UseMock = true` (หรือ `IdentityVerification.UseMock = true`):
- `GetAuthorizationUrl(...)` จะคืนค่า **Loopback Redirect URL** วิ่งตรงกลับเข้า `RedirectUri` ของเว็บคุณทันทีพร้อม `?code=mock-auth-code-001&state={state}`
- `ExchangeCodeAsync(...)`, `GetUserInfoAsync(...)`, `RefreshTokenAsync(...)`, `InspectTokenAsync(...)`, `RevokeTokenAsync(...)`, `GetOpenIdConfigurationAsync(...)`, `GetJwksAsync(...)`, และ `ValidateIdTokenAsync(...)` จะจำลองผลลัพธ์ครบทั้ง 20 Scopes ภายในหน่วยความจำของ SDK ทันทีโดยไม่ยิง HTTP ออกภายนอก

---

## 3. เมธอดและการรับส่งข้อมูล (`IThaIdProvider` — รองรับครบทุก Endpoint ของ ThaID)

### 3.1 สร้าง Authorization URL (`GetAuthorizationUrl`)
```csharp
string codeVerifier = PkceHelper.GenerateCodeVerifier();
string codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);
string state = PkceHelper.GenerateState(); // หรือ PkceHelper.RandomUrlSafe(32)
string nonce = PkceHelper.GenerateNonce();

string authUrl = _thaIdProvider.GetAuthorizationUrl(
    redirectUri: "https://your-domain.com/api/identity/thaid/callback",
    state: state,
    codeChallenge: codeChallenge,
    codeChallengeMethod: "S256",
    nonce: nonce);
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
- **`RefreshTokenAsync(string refreshToken, CancellationToken ct)`:** ขอ Access Token ใหม่ด้วย `grant_type=refresh_token` ที่ `/api/v2/oauth2/token/`
- **`InspectTokenAsync(string accessToken, CancellationToken ct)`:** ตรวจสอบสถานะ Token (`active`, `scope`, `sub`, `exp`) ที่ `/api/v2/oauth2/introspect/`
- **`RevokeTokenAsync(string token, CancellationToken ct)`:** ยกเลิก Token ที่ `/api/v2/oauth2/revoke/`

### 3.4 เมธอดสำหรับ OIDC Discovery, JWKS และการตรวจสอบ ID Token (`ValidateIdTokenAsync`)
- **`GetOpenIdConfigurationAsync(CancellationToken ct)`:** ดึงข้อมูล OpenID Configuration จาก `/.well-known/openid-configuration`
- **`GetJwksAsync(CancellationToken ct)`:** ดึง Public Keys (JWKS `keys`) จาก `/jwks/`
- **`ValidateIdTokenAsync(string idToken, string? expectedNonce = null, CancellationToken ct = default)`:** ถอดรหัสและตรวจสอบความถูกต้องของ `id_token` (JWT) ที่ได้รับจาก ThaID:
  - ตรวจสอบวันหมดอายุ (`exp`), ผู้ออก (`iss`), ผู้รับ (`aud` เทียบกับ `ClientId`), และค่า `nonce` (เมื่อส่ง `expectedNonce` มา)
  - เมื่อกำหนดค่า `JwksUrl` ไว้ ระบบจะดึง Public Key (`ES256` P-256 / `RS256`) จาก JWKS มาตรวจสอบลายเซ็นดิจิทัลของ JWT อัตโนมัติโดยไม่ต้องพึ่งพา NuGet ภายนอก
  - คืนค่า Claims ทั้งหมดใน `result.Data`

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
| `"pid"` | `ExchangeCodeAsync`, `GetUserInfoAsync`, `ValidateIdTokenAsync` | เลขประจำตัวประชาชน 13 หลัก |
| `"given_name"`, `"middle_name"`, `"family_name"`, `"name"` | `GetUserInfoAsync`, `ExchangeCodeAsync`, `ValidateIdTokenAsync` | ชื่อ-นามสกุลภาษาไทย |
| `"given_name_en"`, `"middle_name_en"`, `"family_name_en"`, `"name_en"` | `GetUserInfoAsync`, `ExchangeCodeAsync`, `ValidateIdTokenAsync` | ชื่อ-นามสกุลภาษาอังกฤษ |
| `"title"`, `"title_en"`, `"gender"`, `"birthdate"` | `GetUserInfoAsync`, `ExchangeCodeAsync`, `ValidateIdTokenAsync` | คำนำหน้าชื่อ, เพศ, วันเกิด |
| `"address"`, `"house_address"`, `"smartcard_code"`, `"date_of_issuance"`, `"date_of_expiry"`, `"ial"` | `GetUserInfoAsync`, `ExchangeCodeAsync`, `ValidateIdTokenAsync` | ที่อยู่ตามทะเบียนบ้าน, รหัสบัตร, วันออกบัตร/หมดอายุ, ระดับ IAL |
| `"active"` | `InspectTokenAsync` | สถานะของ Token (`true` / `false`) |
| `"issuer"`, `"authorization_endpoint"`, `"token_endpoint"`, `"jwks_uri"` | `GetOpenIdConfigurationAsync` | ข้อมูล OIDC Discovery Metadata |
| `"keys"` | `GetJwksAsync` | รายการ JSON Web Keys (`EC` P-256 `ES256`) |

---

## 5. รหัสข้อผิดพลาด (`result.ErrorCode`)
- `MISSING_AUTH_CODE`: ไม่ได้ส่ง `authorizationCode`
- `MISSING_REDIRECT_URI`: ไม่ได้ระบุ `redirectUri` ทั้งในพารามิเตอร์และใน Config
- `MISSING_CONFIGURATION`: ไม่ได้ระบุ `ClientId` หรือ `ClientSecret`
- `INVALID_STATE`: ค่า `request.State` ไม่ตรงกับ `expected_state`
- `MISSING_ID_TOKEN` / `INVALID_ID_TOKEN`: ไม่ได้ส่ง `idToken` หรือรูปแบบ JWT ไม่ถูกต้อง
- `ID_TOKEN_EXPIRED`: `id_token` หมดอายุ (`exp`)
- `INVALID_ISSUER` / `INVALID_AUDIENCE` / `INVALID_NONCE` / `INVALID_ID_TOKEN_SIGNATURE`: ข้อมูล `iss`, `aud`, `nonce` หรือลายเซ็นดิจิทัลของ `id_token` ไม่ถูกต้อง
- `TOKEN_EXCHANGE_FAILED` / `TOKEN_REFRESH_FAILED` / `TOKEN_INSPECT_FAILED` / `TOKEN_REVOKE_FAILED` / `USERINFO_FETCH_FAILED` / `DISCOVERY_FETCH_FAILED` / `JWKS_FETCH_FAILED`: ThaID ตอบกลับด้วย HTTP Non-2xx (ดู HTTP Status Code ได้จาก `result.Error?.HttpStatusCode`)
- `INVALID_THAID_RESPONSE` / `INVALID_USERINFO_RESPONSE` / `INVALID_DISCOVERY_RESPONSE` / `INVALID_JWKS_RESPONSE`: JSON ที่ได้รับไม่ถูกต้อง
- `NETWORK_ERROR`: เกิดข้อผิดพลาดในการเชื่อมต่อโครงข่าย (`HttpRequestException`)

---

## 6. รายละเอียดแยกตามราย Endpoint (Request / Response Specification & VS Code REST Client)

> 💡 **รองรับการยิงทดสอบผ่าน VS Code Extension ทันที:**
>
> 🚀 **วิธีรัน MockServer แบบ 1-Click (ไม่สนตำแหน่ง Path / ย้ายไปเครื่องไหนก็กดรันได้ทันที):**
> 1. **กดรันผ่าน VS Code Task (แนะนำที่สุด):**
>    - ใน VS Code Markdown Preview: คลิก **[▶️ กดรัน MockServer ทันที](command:workbench.action.tasks.runTask?%5B%22Run%20MockServer%22%5D)** *(รันผ่าน Task `${workspaceFolder}` อัตโนมัติ)*
>    - หรือกดคีย์ลัด: `Ctrl+Shift+P` (Mac: `Cmd+Shift+P`) ➔ พิมพ์ `Tasks: Run Task` ➔ เลือก **`Run MockServer`**
> 2. **กดรันผ่านสคริปต์ 1-Click ในโปรเจกต์ (ตรวจหา Root อัตโนมัติ):**
>    - **Windows:** ดับเบิลคลิกไฟล์ [`run-mockserver.cmd`](../../run-mockserver.cmd) หรือ [`run-mockserver.ps1`](../../run-mockserver.ps1)
>    - **Linux / macOS:** รันสคริปต์ [`./run-mockserver.sh`](../../run-mockserver.sh)
> 3. **หรือรันผ่าน Terminal จากโฟลเดอร์ใดก็ได้ (ตรวจหา Path อัตโนมัติด้วย Git):**
>    ```bash
>    dotnet run --project "$((git rev-parse --show-toplevel)/MockServer/Netka.Identity.MockServer.csproj)"
>    ```
>
> 📡 **ขั้นตอนการยิงทดสอบ API:**
> - ติดตั้ง Extension **[REST Client (`humao.rest-client`)](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)** ใน VS Code
> - คุณสามารถคลิกปุ่ม **`Send Request`** ที่ปรากฏเหนือบล็อก `http` ด้านล่างนี้ในไฟล์ `.md` ได้โดยตรง หรือเปิดไฟล์ **[`docs/providers/thaid.http`](./thaid.http)** เพื่อยิงทดสอบครบทั้ง 8 Endpoints (สลับ `@baseUrl` ระหว่าง `http://localhost:5199` ของ `MockServer` กับ `https://imauth.bora.dopa.go.th` ของกรมการปกครองได้ทันที)

---

### Endpoint 6.1: OpenID Connect Discovery (`GET /.well-known/openid-configuration`)
- **เมธอดใน SDK:** `await _thaIdProvider.GetOpenIdConfigurationAsync(ct)`
- **หน้าที่:** ดึงข้อมูล Metadata และ URL ของทุก Endpoint จากระบบ ThaID

#### Request (VS Code REST Client)
```http
### 1. OpenID Connect Discovery
GET https://imauth.bora.dopa.go.th/.well-known/openid-configuration HTTP/1.1
Accept: application/json
```

#### Response (`200 OK`)
```json
{
  "issuer": "https://imauth.bora.dopa.go.th",
  "authorization_endpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/auth/",
  "token_endpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/token/",
  "userinfo_endpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/userinfo/",
  "introspection_endpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/introspect/",
  "revocation_endpoint": "https://imauth.bora.dopa.go.th/api/v2/oauth2/revoke/",
  "jwks_uri": "https://imauth.bora.dopa.go.th/jwks/",
  "response_types_supported": ["code"],
  "grant_types_supported": ["authorization_code", "refresh_token"],
  "token_endpoint_auth_methods_supported": ["client_secret_basic", "client_secret_post"],
  "id_token_signing_alg_values_supported": ["ES256"],
  "code_challenge_methods_supported": ["S256", "plain"],
  "scopes_supported": [
    "openid", "pid", "address", "gender", "birthdate",
    "given_name", "middle_name", "family_name", "name",
    "given_name_en", "middle_name_en", "family_name_en", "name_en",
    "title", "title_en", "ial", "smartcard_code",
    "date_of_expiry", "date_of_issuance", "house_address"
  ]
}
```

---

### Endpoint 6.2: JSON Web Key Set — JWKS (`GET /jwks/`)
- **เมธอดใน SDK:** `await _thaIdProvider.GetJwksAsync(ct)` (และถูกใช้โดย `ValidateIdTokenAsync`)
- **หน้าที่:** ดึง Public Key (`EC` P-256 `ES256`) ของกรมการปกครองสำหรับตรวจสอบลายเซ็นดิจิทัลของ `id_token`

#### Request (VS Code REST Client)
```http
### 2. JSON Web Key Set (JWKS)
GET https://imauth.bora.dopa.go.th/jwks/ HTTP/1.1
Accept: application/json
```

#### Response (`200 OK`)
```json
{
  "keys": [
    {
      "kty": "EC",
      "crv": "P-256",
      "use": "sig",
      "alg": "ES256",
      "kid": "thaid-bora-key-id",
      "x": "<BASE64URL_EC_X_COORDINATE>",
      "y": "<BASE64URL_EC_Y_COORDINATE>"
    }
  ]
}
```

---

### Endpoint 6.3: OAuth 2.0 Authorization Endpoint (`GET /api/v2/oauth2/auth/`)
- **เมธอดใน SDK:** `_thaIdProvider.GetAuthorizationUrl(redirectUri, state, codeChallenge, codeChallengeMethod, nonce)`
- **หน้าที่:** สร้าง URL สำหรับแสดง QR Code บนหน้าจอตู้ Kiosk หรือสั่ง Browser Redirect ไปยังหน้ายินยอมของแอป ThaID

#### ตาราง Query Parameters ขาเข้า
| ชื่อพารามิเตอร์ | บังคับ | คำอธิบาย |
| :--- | :---: | :--- |
| `response_type` | ✅ | ต้องระบุเป็น `"code"` เสมอ |
| `client_id` | ✅ | รหัส `ClientId` ที่ได้รับจากกรมการปกครอง |
| `redirect_uri` | ✅ | Callback URL ที่ลงทะเบียนไว้กับกรมการปกครอง |
| `scope` | ✅ | รายการ Scopes คั่นด้วยช่องว่าง เช่น `openid pid given_name family_name birthdate` |
| `state` | ✅ | ค่าสุ่มป้องกัน CSRF (สร้างด้วย `PkceHelper.GenerateState()`) |
| `code_challenge` | แนะนำ | ค่า PKCE Challenge ที่คำนวณจาก `PkceHelper.GenerateCodeChallenge(codeVerifier)` |
| `code_challenge_method` | แนะนำ | `"S256"` |
| `nonce` | Optional | ค่าสุ่มป้องกัน Replay Attack ใน `id_token` (`PkceHelper.GenerateNonce()`) |

#### Request (VS Code REST Client — ทดสอบผ่าน MockServer)
```http
### 3. OAuth 2.0 Authorization Endpoint
GET http://localhost:5199/api/v2/oauth2/auth/?response_type=code&client_id=mock-client-id&redirect_uri=https%3A%2F%2Flocalhost%3A5001%2Fapi%2Fidentity%2Fthaid%2Fcallback&scope=openid%20pid%20profile&state=state-12345&code_challenge=E9Melhoa2OwvFrGMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256&scenario=success HTTP/1.1
```

#### Response (`302 Found` Redirect กลับไปยัง `redirect_uri`)
- **กรณีประชาชนกดยินยอมสำเร็จ:**
  ```http
  HTTP/1.1 302 Found
  Location: https://localhost:5001/api/identity/thaid/callback?code=mock-auth-code-001&state=state-12345
  ```
- **กรณีประชาชนกดยกเลิกในแอป ThaID (`error=access_denied`):**
  ```http
  HTTP/1.1 302 Found
  Location: https://localhost:5001/api/identity/thaid/callback?error=access_denied&error_description=User+denied+consent&state=state-12345
  ```

---

### Endpoint 6.4: Token Endpoint — แลก Authorization Code (`POST /api/v2/oauth2/token/`)
- **เมธอดใน SDK:** `await _thaIdProvider.ExchangeCodeAsync(authorizationCode, redirectUri, codeVerifier, ct)` หรือ `await _thaIdProvider.VerifyAsync(request, ct)`
- **การยืนยันตัวตน:** `Authorization: Basic Base64(ClientId:ClientSecret)` (และแนบ `x-api-key` อัตโนมัติหากตั้งค่า `ApiKey` ไว้)
- **Content-Type:** `application/x-www-form-urlencoded`

#### ตาราง Form Parameters ขาเข้า
| ชื่อฟิลด์ | บังคับ | คำอธิบาย |
| :--- | :---: | :--- |
| `grant_type` | ✅ | `"authorization_code"` |
| `code` | ✅ | ค่า `code` ที่ได้รับจาก Callback |
| `redirect_uri` | ✅ | ต้องตรงกับ `redirect_uri` ตอนขอ Authorization Code |
| `client_id` | ✅ | รหัส `ClientId` |
| `client_secret` | ✅ | รหัส `ClientSecret` |
| `code_verifier` | แนะนำ | ค่า PKCE `code_verifier` คู่กับ `code_challenge` ตอนเริ่ม |

#### Request (VS Code REST Client)
```http
### 4. Token Exchange (authorization_code)
POST http://localhost:5199/api/v2/oauth2/token/ HTTP/1.1
Authorization: Basic bW9jay1jbGllbnQtaWQ6bW9jay1jbGllbnQtc2VjcmV0
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&code=mock-auth-code-001&client_id=mock-client-id&client_secret=mock-client-secret&redirect_uri=https%3A%2F%2Flocalhost%3A5001%2Fapi%2Fidentity%2Fthaid%2Fcallback
```

#### Response กรณีสำเร็จ (`200 OK` — ข้อมูลครบ 20 Scopes + Tokens)
```json
{
  "access_token": "mock-thaid-access-token",
  "refresh_token": "mock-thaid-refresh-token",
  "id_token": "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "openid pid address gender birthdate given_name middle_name family_name name given_name_en middle_name_en family_name_en name_en title title_en ial smartcard_code date_of_expiry date_of_issuance house_address",
  "sub": "1234567890121",
  "pid": "1234567890121",
  "title": "นาย",
  "title_en": "Mr.",
  "given_name": "สมชาย",
  "middle_name": "",
  "family_name": "ใจดี",
  "name": "นาย สมชาย ใจดี",
  "given_name_en": "Somchai",
  "middle_name_en": "",
  "family_name_en": "Jaidee",
  "name_en": "Mr. Somchai Jaidee",
  "gender": "male",
  "birthdate": "1990-01-31",
  "address": "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี",
  "house_address": {
    "raw": "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี",
    "formatted": "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี"
  },
  "smartcard_code": "ME0123456789",
  "date_of_issuance": "2022-01-31",
  "date_of_expiry": "2030-01-30",
  "ial": 2.3
}
```

#### Response กรณี Error (`400 Bad Request` -> SDK คืน `ErrorCode = "TOKEN_EXCHANGE_FAILED"`)
```json
{
  "error": "invalid_grant",
  "error_description": "Authorization code has expired"
}
```

---

### Endpoint 6.5: Token Endpoint — ต่ออายุด้วย Refresh Token (`POST /api/v2/oauth2/token/`)
- **เมธอดใน SDK:** `await _thaIdProvider.RefreshTokenAsync(refreshToken, ct)`
- **การยืนยันตัวตน:** `Authorization: Basic Base64(ClientId:ClientSecret)`
- **Content-Type:** `application/x-www-form-urlencoded`

#### Request (VS Code REST Client)
```http
### 5. Refresh Token (refresh_token)
POST http://localhost:5199/api/v2/oauth2/token/ HTTP/1.1
Authorization: Basic bW9jay1jbGllbnQtaWQ6bW9jay1jbGllbnQtc2VjcmV0
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&refresh_token=mock-thaid-refresh-token&code=mock-auth-code-001
```

#### Response กรณีสำเร็จ (`200 OK`)
```json
{
  "access_token": "new-thaid-access-token",
  "refresh_token": "new-thaid-refresh-token",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

---

### Endpoint 6.6: Token Introspection Endpoint (`POST /api/v2/oauth2/introspect/`)
- **เมธอดใน SDK:** `await _thaIdProvider.InspectTokenAsync(accessToken, ct)`
- **การยืนยันตัวตน:** `Authorization: Basic Base64(ClientId:ClientSecret)`
- **Content-Type:** `application/x-www-form-urlencoded`

#### Request (VS Code REST Client)
```http
### 6. Token Introspection
POST http://localhost:5199/api/v2/oauth2/introspect/ HTTP/1.1
Authorization: Basic bW9jay1jbGllbnQtaWQ6bW9jay1jbGllbnQtc2VjcmV0
Content-Type: application/x-www-form-urlencoded

token=mock-thaid-access-token
```

#### Response กรณี Token ยังใช้งานได้ (`200 OK`)
```json
{
  "active": true,
  "sub": "1234567890121",
  "scope": "openid pid address gender birthdate given_name middle_name family_name name given_name_en middle_name_en family_name_en name_en title title_en ial smartcard_code date_of_expiry date_of_issuance house_address"
}
```

#### Response กรณี Token หมดอายุหรือถูกยกเลิก (`200 OK`)
```json
{
  "active": false
}
```

---

### Endpoint 6.7: Token Revocation Endpoint (`POST /api/v2/oauth2/revoke/`)
- **เมธอดใน SDK:** `await _thaIdProvider.RevokeTokenAsync(token, ct)`
- **การยืนยันตัวตน:** `Authorization: Basic Base64(ClientId:ClientSecret)`
- **Content-Type:** `application/x-www-form-urlencoded`

#### Request (VS Code REST Client)
```http
### 7. Token Revocation
POST http://localhost:5199/api/v2/oauth2/revoke/ HTTP/1.1
Authorization: Basic bW9jay1jbGllbnQtaWQ6bW9jay1jbGllbnQtc2VjcmV0
Content-Type: application/x-www-form-urlencoded

token=mock-thaid-access-token
```

#### Response กรณีสำเร็จ (`200 OK`)
```json
{
  "message": "revoked"
}
```

---

### Endpoint 6.8: OIDC UserInfo Endpoint (`GET /api/v2/oauth2/userinfo/`)
- **เมธอดใน SDK:** `await _thaIdProvider.GetUserInfoAsync(accessToken, ct)`
- **การยืนยันตัวตน:** `Authorization: Bearer <access_token>`

#### Request (VS Code REST Client)
```http
### 8. UserInfo Endpoint
GET http://localhost:5199/api/v2/oauth2/userinfo/ HTTP/1.1
Authorization: Bearer mock-thaid-access-token
Accept: application/json
```

#### Response กรณีสำเร็จ (`200 OK` — ข้อมูลโปรไฟล์ประชาชนตาม Scopes ที่ได้รับอนุญาต)
```json
{
  "sub": "1234567890121",
  "pid": "1234567890121",
  "title": "นาย",
  "title_en": "Mr.",
  "given_name": "สมชาย",
  "middle_name": "",
  "family_name": "ใจดี",
  "name": "นาย สมชาย ใจดี",
  "given_name_en": "Somchai",
  "middle_name_en": "",
  "family_name_en": "Jaidee",
  "name_en": "Mr. Somchai Jaidee",
  "gender": "male",
  "birthdate": "1990-01-31",
  "address": "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี",
  "house_address": {
    "raw": "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี",
    "formatted": "99/9 หมู่ 1 ต.คลองหนึ่ง อ.คลองหลวง จ.ปทุมธานี"
  },
  "smartcard_code": "ME0123456789",
  "date_of_issuance": "2022-01-31",
  "date_of_expiry": "2030-01-30",
  "ial": 2.3
}
```

#### Response กรณี Token ไม่ถูกต้องหรือหมดอายุ (`401 Unauthorized` -> SDK คืน `ErrorCode = "USERINFO_FETCH_FAILED"`)
```http
HTTP/1.1 401 Unauthorized
```

