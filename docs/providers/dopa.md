# เอกสารอ้างอิงการเชื่อมต่อ DOPA (`DOPA`)

- **ProviderName:** `"DOPA"`
- **Config Section:** `"IdentityVerification:Dopa"`
- **Interface:** `Netka.Identity.Core.Interfaces.IDopaProvider` (สืบทอดจาก `IVerificationProvider`)
- **Implementation:** `Netka.Identity.Infrastructure.Providers.DopaProvider`
- **เอกสารอ้างอิงทางการ (Official Documentation & Spec URLs):**
  - **แนวทางการพัฒนาระบบโดยใช้ API Laser Code (สพร. / DGA):** [https://kb.dga.or.th/gdx/5api-lasercode/](https://kb.dga.or.th/gdx/5api-lasercode/)
  - **แนวทางการพัฒนาระบบโดยใช้ API สำหรับผู้พัฒนาหน่วยงาน (สพร. / DGA):** [https://kb.dga.or.th/gdx/4api/](https://kb.dga.or.th/gdx/4api/)
  - **มาตรฐานรหัสหลังบัตรประจำตัวประชาชน (Laser Code Spec):** [https://stat.bora.dopa.go.th/card/smartcard4_5.htm](https://stat.bora.dopa.go.th/card/smartcard4_5.htm)
  - **พจนานุกรมข้อมูลภาครัฐ (Data Dictionary Detail):** [https://gdx.dga.or.th/DataCatalog/Dictionary/Detail?id=4996ceda-33a8-49ad-9841-e169fc9ecc4a](https://gdx.dga.or.th/DataCatalog/Dictionary/Detail?id=4996ceda-33a8-49ad-9841-e169fc9ecc4a)
  - **SwaggerHub DGA APIs (Validate & Services — ลิงก์เดิมในคู่มือ DGA):** [https://app.swaggerhub.com/apis/dga/Validate/1.0](https://app.swaggerhub.com/apis/dga/Validate/1.0) *(หมายเหตุ: ปัจจุบันฝั่ง SwaggerHub ปิดการเข้าถึง Public ของหน้านี้เป็น `404 Not Found` แล้ว โดยให้ยึดสเปกจริงจากคู่มือ `kb.dga.or.th/gdx/4api/` และที่บันทึกไว้ในเอกสารนี้แทน)*
- **มาตรฐานการเชื่อมต่อ (รองรับครบทั้ง 2 รูปแบบ):**
  1. **รูปแบบ Enterprise Internal Gateway / VPN Direct (`Mode = EnterpriseGateway`, ค่าเริ่มต้น):**
     - Direct Server-to-Server REST API (`POST application/json` พร้อม `Authorization: Bearer <ApiKey>` หรือขอ OAuth 2.0 `client_credentials` Token อัตโนมัติผ่าน `TokenUrl`)
     - รองรับครบทั้ง Laser Code, Smart Card Chip (`CheckCardByCID`), และ Face Match 1:1 กับทะเบียนราษฎร
  2. **รูปแบบ DGA GDX (`api.egov.go.th` — `Mode = DgaGdx`):**
     - ตามมาตรฐานทางการของศูนย์กลางแลกเปลี่ยนข้อมูลภาครัฐ (สพร. / DGA)
     - ขั้นที่ 1: ตรวจสอบยืนยันสิทธิ์อัตโนมัติ (`GET /ws/auth/validate`) ด้วย `Consumer-Key` + `ConsumerSecret` และ `AgentID` -> ได้ Token อายุ 60 นาที (แคชในหน่วยความจำ 55 นาที)
     - ขั้นที่ 2: ตรวจสอบข้อมูลหน้าบัตรและเลขหลังบัตร (`GET /ws/dopa/verification/personal?CitizenID=...&FirstName=...&LastName=...&BEBirthDate=...&LaserCode=...`) พร้อม Header `Consumer-Key` และ `Token`

> ⚠️ **กฎความปลอดภัยเคร่งครัด (Strict Security Rule — ห้ามใส่ Key หรือ Secret จริงลงในเอกสารเด็ดขาด):**
> - **ห้ามใส่ API Key, Consumer-Key, ConsumerSecret, Client Secret, Encryption Key หรือ Secret จริงใดๆ ลงในไฟล์เอกสาร `docs/providers/*.md` หรือ `docs/providers/*.http` โดยเด็ดขาด!**
> - ในเอกสารและไฟล์ตัวอย่างทั้งหมด **ต้องใช้ Placeholder เสมอ** เช่น `<YOUR_DGA_CONSUMER_KEY>`, `<YOUR_DGA_CONSUMER_SECRET>`, `<YOUR_DOPA_GATEWAY_API_KEY>`, หรือ `{{dgaConsumerKey}}` เท่านั้น
> - ข้อมูล Secret จริงต้องถูกจัดเก็บใน Secure Environment Variables, Secret Manager, หรือ Key Vault ประจำแต่ละสภาพแวดล้อมระบบเท่านั้น

---

## 1. การตั้งค่า (`appsettings.json` หรือ Database)

### 1.1 การตั้งค่ารูปแบบที่ 1: Enterprise Internal Gateway (ค่าเริ่มต้น)
```json
{
  "IdentityVerification": {
    "EncryptionKey": "<BASE64_32_BYTE_AES_KEY>",
    "Dopa": {
      "UseMock": false,
      "Mode": "EnterpriseGateway",
      "VerificationUrl": "https://dopa-gateway.your-agency.local/api/v1/card/verify",
      "LaserVerificationUrl": "https://dopa-gateway.your-agency.local/api/v1/card/verify-laser",
      "ChipVerificationUrl": "https://dopa-gateway.your-agency.local/api/v1/card/verify-chip",
      "FaceVerificationUrl": "https://dopa-gateway.your-agency.local/api/v1/face/verify",
      "FaceThreshold": 0.80,
      "TokenUrl": "https://dopa-gateway.your-agency.local/oauth/token",
      "ClientId": "<YOUR_DOPA_CLIENT_ID>",
      "ClientSecret": "<YOUR_DOPA_CLIENT_SECRET>",
      "ApiKey": "<YOUR_DOPA_GATEWAY_API_KEY>",
      "AllowUntrustedCertificates": false
    }
  }
}
```

### 1.2 การตั้งค่ารูปแบบที่ 2: DGA GDX (`api.egov.go.th`)
```json
{
  "IdentityVerification": {
    "EncryptionKey": "<BASE64_32_BYTE_AES_KEY>",
    "Dopa": {
      "UseMock": false,
      "Mode": "DgaGdx",
      "ConsumerKey": "<YOUR_DGA_CONSUMER_KEY>",
      "ConsumerSecret": "<YOUR_DGA_CONSUMER_SECRET>",
      "AgentId": "<OFFICER_PID_OR_SYSTEM_NAME>",
      "DgaGdxBaseUrl": "https://api.egov.go.th"
    }
  }
}
```
> **หมายเหตุ:**
> - หากกำหนด `ConsumerKey` หรือ `ConsumerSecret` ระบบจะตั้งค่า `Mode = DgaGdx` ให้อัตโนมัติแม้ไม่ได้ระบุ `Mode`
> - ในโหมด `DgaGdx` เมธอด `VerifyByLaserAsync` จะเรียก Token และแปลง Query parameters ตามมาตรฐาน DGA ให้อัตโนมัติทั้งหมด โดยที่ Dev ไม่ต้องจัดการ Session หรือ URL Encoding เอง

### 1.3 การตั้งค่าจาก Database (`DopaOptions` ใน `IdentityVerificationOptions`)
```csharp
builder.Services.AddIdentityVerification(sp =>
{
    using var scope = sp.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MyAppDbContext>();
    var cfg = db.SystemConfigs.AsNoTracking().First();

    return new IdentityVerificationOptions
    {
        EncryptionKey = cfg.EncryptionKey,
        Dopa = new DopaOptions
        {
            UseMock = cfg.DopaUseMock,
            VerificationUrl = cfg.DopaVerificationUrl,
            LaserUrl = cfg.DopaLaserUrl,
            ChipUrl = cfg.DopaChipUrl,
            FaceUrl = cfg.DopaFaceUrl,
            FaceThreshold = cfg.DopaFaceThreshold,
            TokenUrl = cfg.DopaTokenUrl,
            ClientId = cfg.DopaClientId,
            ClientSecret = cfg.DopaClientSecret,
            ApiKey = cfg.DopaApiKey,
            AllowUntrustedCertificates = cfg.DopaAllowUntrustedCerts
        }
    };
});
```

### 1.3 โหมดจำลองในตัว (`UseMock = true` — ไม่ต้องรัน MockServer แยก)
เมื่อตั้งค่า `Dopa.UseMock = true` (หรือ `IdentityVerification.UseMock = true`) SDK จะจำลองผลลัพธ์ของ DOPA ทั้งหมดในหน่วยความจำทันที:
- **บัตรสถานะปกติ (Success):** เลขบัตร 13 หลักถูกต้อง + รหัส Laser / Chip ปกติ -> คืนค่า `IsSuccess = true`, `Code = 0`, `Desc = "สถานะปกติ"`
- **บัตรไม่ปกติ / ปฏิเสธ (Rejected):** ส่ง `laserId: "ME9999999999"` หรือ `chipNo: "0000000000000000"` -> คืนค่า `IsSuccess = false`, `ErrorCode = "CARD_VERIFICATION_REJECTED"`, `IsError = true`
- **ใบหน้าตรงกัน (Face Match Success):** เรียก `VerifyByFaceAsync` ด้วย Base64 ปกติ -> คืนค่า `IsSuccess = true`, `similarity = 0.965`, `confidence = 96.5`, `match = true`, `result_code = "0"`
- **ใบหน้าไม่ตรงกัน (Face Mismatch):** เรียก `VerifyByFaceAsync` โดย `faceImageBase64` มีคำว่า `"MISMATCH"` -> คืนค่า `IsSuccess = false`, `ErrorCode = "FACE_VERIFICATION_REJECTED"`, `similarity = 0.42`, `match = false`
- **จำลอง HTTP 503:** ส่ง `laserId: "ME5000000000"` หรือ `faceImageBase64: "ERROR503"` -> คืนค่า `ErrorCode = "DOPA_API_FAILED"` (`HttpStatusCode = 503`)
- **จำลอง Timeout:** ส่ง `laserId: "METIMEOUT000"` หรือ `faceImageBase64: "TIMEOUT"` -> คืนค่า `ErrorCode = "NETWORK_ERROR"`

---

## 2. เมธอดและการรับส่งข้อมูล (`IDopaProvider` — รองรับครบทุก Endpoint ของ DOPA)

### 2.1 ตรวจสอบด้วยข้อมูลหน้าบัตรและเลขหลังบัตร (`VerifyByLaserAsync` — `CheckCardByLaser`)
```csharp
VerificationResult result = await _dopaProvider.VerifyByLaserAsync(
    pid: "1234567890121",       // เลขบัตรประชาชน 13 หลัก (ตรวจ Checksum Modulo 11 อัตโนมัติ)
    firstName: "สมชาย",          // ชื่อภาษาไทย
    lastName: "ใจดี",            // นามสกุลภาษาไทย
    birthDate: "25350131",      // วันเกิด พ.ศ. รูปแบบ YYYYMMDD
    laserId: "ME0123456789",    // เลขหลังบัตร (Laser ID)
    cancellationToken: ct);
```

### 2.2 ตรวจสอบด้วยเลขบัตรและเลขชิปสมาร์ทการ์ด (`VerifyByChipAsync` — `CheckCardByCID`)
```csharp
VerificationResult result = await _dopaProvider.VerifyByChipAsync(
    pid: "1234567890121",       // เลขบัตรประชาชน 13 หลัก (ตรวจ Checksum Modulo 11 อัตโนมัติ)
    chipNo: "0011223344556677", // เลข Chip Number ที่อ่านได้จากเครื่องอ่านบัตรสมาร์ทการ์ด
    bp1No: "1234567890",        // (Optional) เลขคำขอมีบัตร (BP1)
    cancellationToken: ct);
```

### 2.3 ตรวจสอบเปรียบเทียบใบหน้า 1:1 (`VerifyByFaceAsync` — DOPA Face Verification API)
```csharp
VerificationResult result = await _dopaProvider.VerifyByFaceAsync(
    pid: "1234567890121",                 // เลขบัตรประชาชน 13 หลัก (ตรวจ Checksum Modulo 11 อัตโนมัติ)
    faceImageBase64: base64ImageString,   // รูปภาพใบหน้า Base64 (ระบบตัด prefix "data:image/jpeg;base64," ให้อัตโนมัติ)
    threshold: 0.80f,                     // (Optional) เกณฑ์ความคล้ายคลึงขั้นต่ำ (0.0 - 1.0, ค่าเริ่มต้น 0.80)
    cancellationToken: ct);
```

### 2.4 ขอ OAuth 2.0 Access Token จาก DOPA Gateway (`RequestAccessTokenAsync`)
> **หมายเหตุ:** โดยปกติ `DopaProvider` จะเรียกและ Cache Token ให้อัตโนมัติเมื่อตั้งค่า `TokenUrl`, `ClientId`, และ `ClientSecret` แต่หากต้องการเรียกขอ Token โดยตรงสามารถใช้เมธอดนี้ได้:
```csharp
VerificationResult tokenResult = await _dopaProvider.RequestAccessTokenAsync(ct);
string? accessToken = tokenResult.Data.GetStringValue("access_token");
```

### 2.5 ตรวจสอบแบบยืดหยุ่นผ่าน Dictionary (`VerifyAsync(VerificationRequest)`)
ส่งพารามิเตอร์ใดๆ ที่ต้องการไปให้ DOPA Gateway ผ่าน `request.AdditionalParameters` (รองรับ Case-Insensitive `OrdinalIgnoreCase` และหากส่ง `"image"` หรือ `"faceImageBase64"` คู่กับ `"PID"` จะวิ่งเข้ากระบวนการตรวจสอบใบหน้าให้อัตโนมัติ):
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

- `result.IsSuccess`: เป็น `true` เมื่อ HTTP Status เป็น `2xx`, ฟิลด์ `"IsError"` ใน JSON ไม่ได้เป็น `true`, และ (กรณีตรวจใบหน้า) ค่า `match` เป็น `true` หรือ `similarity >= threshold`
- `result.Data` (เก็บ JSON ดิบทั้งหมดจาก DOPA ค้นหา Key แบบ `OrdinalIgnoreCase` หรือใช้ `result.Data.GetStringValue(...)`):
  - **สำหรับตรวจสอบบัตร (`VerifyByLaserAsync`, `VerifyByChipAsync`):**
    - `"IsError"`: `false` (เมื่อบัตรปกติ) หรือ `true` (เมื่อสถานะบัตรไม่ปกติ/ข้อมูลไม่ตรง)
    - `"ErrorMessage"` / `"Desc"` / `"message"`: คำอธิบายผลการตรวจสอบจากกรมการปกครอง (เช่น `"สถานะปกติ"`)
    - `"Code"` / `"StCode"`: รหัสสถานะบัตรประชาชนตามมาตรฐาน DOPA
  - **สำหรับตรวจสอบใบหน้า (`VerifyByFaceAsync`):**
    - `"similarity"`: คะแนนความคล้ายคลึง (ช่วง `0.0` - `1.0` เช่น `0.965`)
    - `"confidence"`: เปอร์เซ็นต์ความมั่นใจ (เช่น `96.5`)
    - `"match"`: ผลการเปรียบเทียบใบหน้า (`true` / `false`)
    - `"threshold"`: เกณฑ์ความคล้ายคลึงที่ใช้ตัดสิน (เช่น `0.80`)
    - `"result_code"` / `"result_text"`: รหัสและข้อความผลการตรวจสอบใบหน้าจาก DOPA
  - **สำหรับขอ Token (`RequestAccessTokenAsync`):**
    - `"access_token"`, `"token_type"`, `"expires_in"`

---

## 4. รหัสข้อผิดพลาด (`result.ErrorCode`)
- `INVALID_PID_FORMAT`: เลขประจำตัวประชาชนไม่ครบ 13 หลัก หรือไม่ผ่านการคำนวณ Checksum Modulo 11
- `MISSING_PARAMETERS`: ข้อมูลบังคับไม่ครบถ้วน
- `MISSING_CONFIGURATION`: ไม่ได้กำหนด URL หรือข้อมูลเชื่อมต่อที่จำเป็น
- `CARD_VERIFICATION_REJECTED`: เชื่อมต่อ DOPA สำเร็จ แต่ DOPA ตอบกลับว่า `"IsError": true` (ข้อมูลดิบยังคงอยู่ใน `result.Data`)
- `FACE_VERIFICATION_REJECTED`: เชื่อมต่อ DOPA Face API สำเร็จ แต่ใบหน้าไม่ตรงกับฐานข้อมูล (`match == false` หรือ `similarity < threshold` ข้อมูลดิบยังคงอยู่ใน `result.Data`)
- `DOPA_TOKEN_FAILED`: ขอ OAuth 2.0 `client_credentials` Token จาก `TokenUrl` ไม่สำเร็จ
- `DOPA_API_FAILED`: DOPA ตอบกลับด้วย HTTP Non-2xx (ดู HTTP Status Code ได้จาก `result.Error?.HttpStatusCode`)
- `INVALID_DOPA_RESPONSE`: JSON ที่ได้รับไม่ถูกต้อง
- `NETWORK_ERROR`: เกิดข้อผิดพลาดในการเชื่อมต่อโครงข่าย (`HttpRequestException`)

---

## 5. รายละเอียดแยกตามราย Endpoint (Request / Response Specification & VS Code REST Client)

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
> - คุณสามารถคลิกปุ่ม **`Send Request`** ที่ปรากฏเหนือบล็อก `http` ด้านล่างนี้ในไฟล์ `.md` ได้โดยตรง หรือเปิดไฟล์ **[`docs/providers/dopa.http`](./dopa.http)** เพื่อยิงทดสอบครบทั้ง 4 Endpoints (ทดสอบกับ `MockServer` ที่ `http://localhost:5199` หรือสลับ `@baseUrl` ไปยัง DOPA Gateway ของหน่วยงานได้ทันที)

---

### Endpoint 5.1: ขอ OAuth 2.0 Access Token จาก DOPA Gateway (`POST /oauth/token`)
- **เมธอดใน SDK:** `await _dopaProvider.RequestAccessTokenAsync(ct)` *(ถูกเรียกและ Cache ให้อัตโนมัติเมื่อตั้งค่า `TokenUrl`, `ClientId`, และ `ClientSecret`)*
- **Content-Type:** `application/x-www-form-urlencoded`

#### ตาราง Form Parameters ขาเข้า
| ชื่อฟิลด์ | บังคับ | คำอธิบาย |
| :--- | :---: | :--- |
| `grant_type` | ✅ | `"client_credentials"` |
| `client_id` | ✅ | รหัส `ClientId` ของ DOPA Gateway |
| `client_secret` | ✅ | รหัส `ClientSecret` ของ DOPA Gateway |

#### Request (VS Code REST Client)
```http
### 1. DOPA OAuth 2.0 Token (client_credentials)
POST http://localhost:5199/oauth/token HTTP/1.1
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=mock-dopa-client-id&client_secret=mock-dopa-client-secret
```

#### Response กรณีสำเร็จ (`200 OK`)
```json
{
  "access_token": "mock-dopa-access-token-2026",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

#### Response กรณี Error (`400 Bad Request` -> SDK คืน `ErrorCode = "DOPA_TOKEN_FAILED"`)
```json
{
  "error": "invalid_client"
}
```

---

### Endpoint 5.2: ตรวจสอบสถานะบัตรด้วยข้อมูลหน้าบัตรและเลขหลังบัตร (`POST /CheckCardByLaser`)
- **เมธอดใน SDK:** `await _dopaProvider.VerifyByLaserAsync(pid, firstName, lastName, birthDate, laserId, ct)`
- **การยืนยันตัวตน:** `Authorization: Bearer <ApiKey หรือ AccessToken>`
- **Content-Type:** `application/json`

#### ตาราง JSON Body Parameters ขาเข้า
| ชื่อฟิลด์ JSON | ชนิดข้อมูล | บังคับ | คำอธิบาย |
| :--- | :---: | :---: | :--- |
| `PID` | `string` | ✅ | เลขประจำตัวประชาชน 13 หลัก (SDK ตรวจสูตร Checksum Modulo 11 ก่อนยิงเสมอ) |
| `FirstName` | `string` | ✅ | ชื่อตัวภาษาไทย (ไม่ต้องใส่คำนำหน้าชื่อ) |
| `LastName` | `string` | ✅ | นามสกุลภาษาไทย |
| `BirthDay` | `string` | ✅ | วันเดือนปีเกิด พ.ศ. รูปแบบ `YYYYMMDD` (เช่น `"25350131"`) |
| `Laser` | `string` | ✅ | รหัสหลังบัตรประชาชน 12 หลัก (ไม่ต้องมีขีด เช่น `"ME0123456789"`) |

#### Request กรณีบัตรปกติ (VS Code REST Client)
```http
### 2. DOPA CheckCardByLaser - สถานะบัตรปกติ
POST http://localhost:5199/CheckCardByLaser HTTP/1.1
Authorization: Bearer mock-dopa-access-token-2026
Content-Type: application/json

{
  "PID": "1234567890121",
  "FirstName": "สมชาย",
  "LastName": "ใจดี",
  "BirthDay": "25350131",
  "Laser": "ME0123456789"
}
```

#### Response กรณีบัตรปกติ (`200 OK` -> `result.IsSuccess = true`)
```json
{
  "IsError": false,
  "Code": 0,
  "Desc": "สถานะปกติ"
}
```

#### Request กรณีจำลองบัตรไม่ปกติ / รหัสหลังบัตรไม่ตรง (VS Code REST Client)
```http
### 2.1 DOPA CheckCardByLaser - สถานะบัตรไม่ปกติ (IsError: true)
POST http://localhost:5199/CheckCardByLaser HTTP/1.1
Authorization: Bearer mock-dopa-access-token-2026
Content-Type: application/json

{
  "PID": "1234567890121",
  "FirstName": "สมชาย",
  "LastName": "ใจดี",
  "BirthDay": "25350131",
  "Laser": "ME9999999999"
}
```

#### Response กรณีบัตรไม่ปกติ (`200 OK` แต่ `IsError: true` -> SDK คืน `IsSuccess = false`, `ErrorCode = "CARD_VERIFICATION_REJECTED"`)
```json
{
  "IsError": true,
  "Code": 1,
  "Desc": "สถานะไม่ปกติ",
  "ErrorMessage": "ไม่พบเลข Laser จาก PID นี้"
}
```

---

### Endpoint 5.3: ตรวจสอบสถานะบัตรด้วยเลขบัตรและเลขชิปสมาร์ทการ์ด (`POST /CheckCardByCID`)
- **เมธอดใน SDK:** `await _dopaProvider.VerifyByChipAsync(pid, chipNo, bp1No, ct)`
- **การยืนยันตัวตน:** `Authorization: Bearer <ApiKey หรือ AccessToken>`
- **Content-Type:** `application/json`

#### ตาราง JSON Body Parameters ขาเข้า
| ชื่อฟิลด์ JSON | ชนิดข้อมูล | บังคับ | คำอธิบาย |
| :--- | :---: | :---: | :--- |
| `PID` | `string` | ✅ | เลขประจำตัวประชาชน 13 หลัก (SDK ตรวจสูตร Checksum Modulo 11 ก่อนยิงเสมอ) |
| `ChipNo` | `string` | ✅ | หมายเลขชิปการ์ด (Chip Number) ที่อ่านได้จากเครื่องอ่านสมาร์ทการ์ด |
| `bp1no` | `string` | Optional | เลขคำขอมีบัตรประชาชน (BP1) ที่อ่านได้จากสมาร์ทการ์ด |

#### Request กรณีบัตรปกติ (VS Code REST Client)
```http
### 3. DOPA CheckCardByCID - ตรวจสอบด้วยเลขชิปสมาร์ทการ์ด (สถานะปกติ)
POST http://localhost:5199/CheckCardByCID HTTP/1.1
Authorization: Bearer mock-dopa-access-token-2026
Content-Type: application/json

{
  "PID": "1234567890121",
  "ChipNo": "0011223344556677",
  "bp1no": "1234567890"
}
```

#### Response กรณีบัตรปกติ (`200 OK` -> `result.IsSuccess = true`)
```json
{
  "IsError": false,
  "Code": 0,
  "Desc": "สถานะปกติ"
}
```

#### Response กรณีข้อมูลชิปไม่ตรง (`200 OK` แต่ `IsError: true` -> SDK คืน `IsSuccess = false`, `ErrorCode = "CARD_VERIFICATION_REJECTED"`)
```json
{
  "IsError": true,
  "Code": 1,
  "Desc": "สถานะไม่ปกติ",
  "ErrorMessage": "ข้อมูล ChipNo หรือ bp1no ไม่ถูกต้อง"
}
```

---

### Endpoint 5.4: ตรวจสอบเปรียบเทียบใบหน้า 1:1 (`POST /api/v1/face/verify` หรือ `/CheckFaceByPID`)
- **เมธอดใน SDK:** `await _dopaProvider.VerifyByFaceAsync(pid, faceImageBase64, threshold, ct)`
- **การยืนยันตัวตน:** `Authorization: Bearer <ApiKey หรือ AccessToken>`
- **Content-Type:** `application/json`

#### ตาราง JSON Body Parameters ขาเข้า
| ชื่อฟิลด์ JSON | ชนิดข้อมูล | บังคับ | คำอธิบาย |
| :--- | :---: | :---: | :--- |
| `pid` | `string` | ✅ | เลขประจำตัวประชาชน 13 หลัก (SDK ตรวจสูตร Checksum Modulo 11 ก่อนยิงเสมอ) |
| `image` | `string` | ✅ | ข้อมูลภาพใบหน้าแบบ Base64 (หากส่ง `data:image/jpeg;base64,...` เข้ามาใน SDK ตัว `VerifyByFaceAsync` จะตัด prefix ออกให้เหลือเฉพาะ Base64 ล้วนโดยอัตโนมัติ) |

#### Request กรณีใบหน้าตรงกัน (VS Code REST Client)
```http
### 4. DOPA Face Verification - กรณีใบหน้าตรงกัน (Match)
POST http://localhost:5199/api/v1/face/verify HTTP/1.1
Authorization: Bearer mock-dopa-access-token-2026
Content-Type: application/json

{
  "pid": "1234567890121",
  "image": "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/"
}
```

#### Response กรณีใบหน้าตรงกัน (`200 OK` -> `result.IsSuccess = true`)
```json
{
  "result_code": "0",
  "result_text": "ใบหน้าตรงกับฐานข้อมูลทะเบียนราษฎร",
  "similarity": 0.965,
  "confidence": 96.5,
  "match": true
}
```

#### Request กรณีจำลองใบหน้าไม่ตรงกัน (VS Code REST Client)
```http
### 4.1 DOPA Face Verification - กรณีใบหน้าไม่ตรงกัน (Mismatch)
POST http://localhost:5199/api/v1/face/verify HTTP/1.1
Authorization: Bearer mock-dopa-access-token-2026
Content-Type: application/json

{
  "pid": "1234567890121",
  "image": "MISMATCH_FACE_BASE64_PAYLOAD"
}
```

#### Response กรณีใบหน้าไม่ตรงกัน (`200 OK` แต่ `match: false` / `similarity < threshold` -> SDK คืน `IsSuccess = false`, `ErrorCode = "FACE_VERIFICATION_REJECTED"`)
```json
{
  "result_code": "1",
  "result_text": "ใบหน้าไม่ตรงกับฐานข้อมูลทะเบียนราษฎร",
  "similarity": 0.42,
  "confidence": 42.0,
  "match": false
}
```

---

### 5. DGA GDX - ขอ Token ยืนยันสิทธิ์ (`GET /ws/auth/validate`)
```http
### 5.1 DGA GDX Government API Validation (ขอ Token)
GET https://api.egov.go.th/ws/auth/validate?ConsumerSecret={{dgaConsumerSecret}}&AgentID={{dgaAgentId}} HTTP/1.1
Consumer-Key: {{dgaConsumerKey}}
Content-Type: application/x-www-form-urlencoded; charset=utf-8
```

#### Response (`200 OK`)
```json
{
  "Result": "8e1ac089-0000-aaaa-0000-403c0c9ab867"
}
```

---

### 6. DGA GDX - ตรวจสอบ Laser Code (`GET /ws/dopa/verification/personal`)
```http
### 6.1 DGA GDX Verify Card by Laser Code
GET https://api.egov.go.th/ws/dopa/verification/personal?CitizenID=1234567890121&FirstName=%E0%B8%AA%E0%B8%A1%E0%B8%8A%E0%B8%B2%E0%B8%A2&LastName=%E0%B9%83%E0%B8%88%E0%B8%94%E0%B8%B5&BEBirthDate=25381229&LaserCode=JT1234567890 HTTP/1.1
Consumer-Key: {{dgaConsumerKey}}
Token: {{dgaToken}}
Content-Type: application/x-www-form-urlencoded; charset=utf-8
```

#### Response กรณีบัตรปกติ (`200 OK` -> `result.IsSuccess = true`)
```json
{
  "Result": true
}
```

#### Response กรณีข้อมูลไม่ตรงหรือบัตรสิ้นสภาพ (`200 OK` แต่ `Result: false` หรือ HTTP `500`)
```json
{
  "Result": false
}
```

