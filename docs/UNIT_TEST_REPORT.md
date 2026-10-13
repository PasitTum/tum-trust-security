# รายงานผลการทดสอบ Unit Test (Unit Test Verification Report)
**โปรเจกต์:** `Netka.Identity.SDK`  
**โปรเจกต์ทดสอบ:** `Tests/Netka.Identity.SDK.Tests.csproj` (xUnit Test Framework)  
**Target Frameworks ที่ทดสอบ:** `.NET 5.0` (`net5.0`), `.NET 8.0` (`net8.0`), และ `.NET 10.0` (`net10.0`)

---

## 1. สรุปผลการรันทดสอบภาพรวม (Test Execution Summary)

| เป้าหมาย Runtime (Target Framework) | จำนวน Test Cases ทั้งหมด | ผ่าน (Passed) | ไม่ผ่าน (Failed) | ข้าม (Skipped) | อัตราความสำเร็จ |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **`.NET 5.0` (`net5.0`)** | 43 | **43** | 0 | 0 | **100%** |
| **`.NET 8.0` (`net8.0`)** | 43 | **43** | 0 | 0 | **100%** |
| **`.NET 10.0` (`net10.0`)** | 43 | **43** | 0 | 0 | **100%** |
| **รวมการรันทั้งหมด (Total Executions)** | **129** | **129** | **0** | **0** | **100%** |

> **คำสั่งสำหรับรันตรวจสอบซ้ำ:**
> ```powershell
> dotnet test "Netka.Identity.SDK.sln" -c Release
> ```

---

## 2. รายละเอียดชุดการทดสอบแยกตามหมวดหมู่ (43 Test Cases)

### หมวดที่ 1: `ThaIdProviderTests` (14 Test Cases)
ทดสอบการทำงานของ `ThaIdProvider` ครอบคลุม OAuth 2.0 / OIDC ทั้ง 6 เส้น, PKCE (`S256`), การรับข้อมูล 20 Scopes, End-to-End 302 Redirect, การจัดการข้อผิดพลาด และโหมด `UseMock = true`

| ลำดับ | ชื่อ Test Method | สิ่งที่ทดสอบ (Test Objective) | ผลลัพธ์ที่คาดหวัง | สถานะ |
| :---: | :--- | :--- | :--- | :---: |
| 1 | `GetAuthorizationUrl_BuildsValidUrlWithPkceAndNonce` | สร้าง URL สำหรับ Redirect / QR Code พร้อม PKCE (`code_challenge`, `S256`) และ `nonce` | ได้ URL ที่มี Query Parameters ครบถ้วนถูกต้อง | ✅ PASS |
| 2 | `GetAuthorizationUrl_WhenClientIdMissing_ThrowsInvalidOperationException` | เรียก `GetAuthorizationUrl` ในโหมดจริง (`UseMock = false`) โดยไม่ตั้งค่า `ClientId` | โยน `InvalidOperationException` แจ้งเตือนการตั้งค่าขาดหาย | ✅ PASS |
| 3 | `ExchangeCodeAsync_SendsBasicAuthAndFormBody_AndReturnsExactProviderResponse` | แลก `authorization_code` เป็น Token พร้อมตรวจสอบ Header `Authorization: Basic` | ส่ง Form Body ถูกต้อง และคืน JSON Response ตรงตามต้นทาง 100% | ✅ PASS |
| 4 | `VerifyAsync_WhenExpectedStateDoesNotMatch_ReturnsInvalidStateError` | ทดสอบป้องกัน CSRF เมื่อค่า `state` ที่ได้จาก Callback ไม่ตรงกับ `expected_state` (`UC-THAID-02`) | คืน `IsSuccess = false`, `ErrorCode = "INVALID_STATE"` โดยไม่ยิง API | ✅ PASS |
| 5 | `RefreshTokenAsync_SendsRefreshTokenGrant_AndReturnsNewTokenData` | ทดสอบการต่ออายุ Token ด้วย `grant_type=refresh_token` | คืน `access_token` และ `refresh_token` ใหม่สำเร็จ | ✅ PASS |
| 6 | `InspectTokenAsync_SendsTokenField_AndReturnsActiveStatus` | ทดสอบการตรวจสอบสถานะ Token (`active: true`) | คืน `Data["active"] = true` และข้อมูล Scope | ✅ PASS |
| 7 | `RevokeTokenAsync_SendsTokenField_AndReturnsSuccess` | ทดสอบการยกเลิก Token เมื่อจบรายการที่ตู้ Kiosk (`UC-THAID-05`) | คืน `IsSuccess = true` พร้อมข้อความยืนยันการยกเลิก | ✅ PASS |
| 8 | `GetUserInfoAsync_SendsBearerHeader_AndReturnsUnmodifiedClaims` | ทดสอบดึงข้อมูลประชาชนผ่าน `/userinfo/` ด้วย Header `Authorization: Bearer` | คืนข้อมูล Claims ของประชาชนครบถ้วนและบันทึก `[PII-VAULT]` Log | ✅ PASS |
| 9 | `EndToEndOAuthRedirectFlow_WithPkceAndAll20OfficialScopes_RedirectsAndExchangesCodeSuccessfully` | ทดสอบ Flow เต็มรูปแบบตั้งแต่สร้าง URL -> Browser ได้รับ `HTTP 302 Found` Redirect -> นำ `code` + `code_verifier` ไปแลกข้อมูลครบทั้ง **20 Scopes** (`UC-THAID-01`) | ตรวจ PKCE ผ่าน และได้ข้อมูลครบทั้ง 20 Scopes (รวม `address` และ `house_address` ที่เป็น Nested Object) | ✅ PASS |
| 10 | `EndToEndOAuthRedirectFlow_WhenCitizenDeniesConsent_RedirectsWithErrorAccessDeniedAndFailsMissingCode` | ทดสอบกรณีประชาชนกดยกเลิกในแอป ThaID แล้ว Redirect กลับมาพร้อม `?error=access_denied` (`UC-THAID-06`) | จับค่า `error=access_denied` ได้ และหากส่งเข้า `VerifyAsync` จะคืน `MISSING_AUTH_CODE` | ✅ PASS |
| 11 | `ExchangeCodeAsync_WhenCodeExpiredOrInvalidGrant_ReturnsTokenExchangeFailedWithHttp400` | ทดสอบกรณี `authorization_code` หมดอายุหรือถูกใช้ซ้ำ (`UC-THAID-03`) | คืน `ErrorCode = "TOKEN_EXCHANGE_FAILED"`, `HttpStatusCode = 400` | ✅ PASS |
| 12 | `ExchangeCodeAsync_WhenNetworkTimeoutOccurs_ReturnsNetworkError` | ทดสอบกรณีโครงข่ายล่มหรือเกิด Timeout ขณะติดต่อ ThaID (`UC-THAID-04`) | ดักจับ `HttpRequestException` และคืน `ErrorCode = "NETWORK_ERROR"` | ✅ PASS |
| 13 | `InspectTokenAsync_WhenTokenIsExpired_ReturnsActiveFalse` | ทดสอบตรวจ Token ที่หมดอายุแล้ว (`UC-THAID-05`) | คืน `IsSuccess = true` พร้อม `Data["active"] = false` | ✅ PASS |
| 14 | `BuiltInUseMock_WhenEnabled_PerformsLoopbackRedirectAndReturnsAll20ScopesWithoutHttpCalls` | ทดสอบโหมด `UseMock = true` ในตัว `.dll` (Loopback Redirect + แลกข้อมูล 20 Scopes + จำลอง Code หมดอายุ + จำลอง Timeout โดยไม่ยิง HTTP จริง) | ทำงานใน Memory 100% คืน Loopback URL และข้อมูล 20 Scopes ครบถ้วน | ✅ PASS |

---

### หมวดที่ 2: `DopaProviderTests` (10 Test Cases)
ทดสอบการทำงานของ `DopaProvider` ทั้งการตรวจสอบด้วยเลขหลังบัตร (`VerifyByLaserAsync`) และชิปสมาร์ทการ์ด (`VerifyByChipAsync`) รวมถึงโหมด `UseMock = true`

| ลำดับ | ชื่อ Test Method | สิ่งที่ทดสอบ (Test Objective) | ผลลัพธ์ที่คาดหวัง | สถานะ |
| :---: | :--- | :--- | :--- | :---: |
| 15 | `VerifyByLaserAsync_SendsCorrectPayloadAndBearerToken_AndReturnsUnmodifiedResponse` | ตรวจสอบสถานะบัตรด้วย Laser ID ปกติ (`UC-DOPA-01`) | ส่ง JSON (`PID`, `FirstName`, `LastName`, `BirthDay`, `Laser`) และคืน `Code: 0`, `Desc: "สถานะปกติ"` | ✅ PASS |
| 16 | `VerifyByLaserAsync_WhenProviderReturnsIsErrorTrue_ReturnsFailureWithProviderData` | ทดสอบเมื่อกรมการปกครองตอบกลับว่า `IsError: true` (เลขหลังบัตรไม่ตรง / บัตรถูกยกเลิก) (`UC-DOPA-04`) | คืน `IsSuccess = false`, `ErrorCode = "CARD_VERIFICATION_REJECTED"` พร้อมแนบ `Data` ต้นทาง | ✅ PASS |
| 17 | `VerifyByLaserAsync_WithInvalidPidChecksum_ReturnsInvalidPidFormatWithoutHttpCall` | ทดสอบส่งเลขบัตร 13 หลักที่ไม่ผ่านสูตร Modulo 11 (`UC-DOPA-02`) | คืน `ErrorCode = "INVALID_PID_FORMAT"` ทันทีโดยไม่ยิง HTTP | ✅ PASS |
| 18 | `VerifyByChipAsync_SendsPidChipNoAndBp1No_AndReturnsUnmodifiedResponse` | ตรวจสอบสถานะบัตรผ่านชิปสมาร์ทการ์ด (`PID`, `ChipNo`, `bp1no`) (`UC-DOPA-01`) | คืน `IsSuccess = true`, `Code: 0` | ✅ PASS |
| 19 | `GetAuthorizationUrl_ThrowsNotSupportedException` | ทดสอบเรียก `GetAuthorizationUrl` บน `DopaProvider` | โยน `NotSupportedException` ตามสัญญาของ Direct API | ✅ PASS |
| 20 | `VerifyByLaserAsync_WithMissingParameters_ReturnsMissingParametersWithoutHttpCall` | ทดสอบส่งชื่อ/วันเกิด/เลขหลังบัตรเป็นค่าว่าง (`UC-DOPA-03`) | คืน `ErrorCode = "MISSING_PARAMETERS"` ทันทีโดยไม่ยิง HTTP | ✅ PASS |
| 21 | `VerifyByChipAsync_WithMissingChipNo_ReturnsMissingParametersWithoutHttpCall` | ทดสอบส่ง `chipNo` เป็นค่าว่าง (`UC-DOPA-03`) | คืน `ErrorCode = "MISSING_PARAMETERS"` ทันทีโดยไม่ยิง HTTP | ✅ PASS |
| 22 | `VerifyByLaserAsync_WhenDopaGatewayReturnsHttp503_ReturnsDopaApiFailedWithStatusCode` | ทดสอบกรณี DOPA Gateway ล่ม ตอบกลับ HTTP 503 (`UC-DOPA-05`) | คืน `ErrorCode = "DOPA_API_FAILED"`, `HttpStatusCode = 503` | ✅ PASS |
| 23 | `VerifyByChipAsync_WhenNetworkTimeoutOccurs_ReturnsNetworkError` | ทดสอบกรณี VPN / โครงข่าย DOPA ตัดการเชื่อมต่อ (`UC-DOPA-06`) | คืน `ErrorCode = "NETWORK_ERROR"` | ✅ PASS |
| 24 | `BuiltInUseMock_WhenEnabled_SimulatesAllDopaUseCasesWithoutHttpCalls` | ทดสอบโหมด `UseMock = true` ในตัว `.dll` สำหรับ DOPA ครบทุกสถานการณ์ (`UC-DOPA-01`, `04`, `05`, `06`) โดยไม่ยิง HTTP | คืนผลลัพธ์จำลองถูกต้องครบทุกกรณีโดยไม่แตะต้องระบบเครือข่าย | ✅ PASS |

---

### หมวดที่ 3: `DependencyInjectionTests` (8 Test Cases)
ทดสอบการลงทะเบียน DI ทั้ง 4 รูปแบบ, การดึง Config จาก Database (`DbContext`/Repository), การทำ Hot-Reload, และ Lazy Validation

| ลำดับ | ชื่อ Test Method | สิ่งที่ทดสอบ (Test Objective) | ผลลัพธ์ที่คาดหวัง | สถานะ |
| :---: | :--- | :--- | :--- | :---: |
| 25 | `AddIdentityVerification_RegistersAllCoreAndExplicitProviderInterfaces` | ทดสอบลงทะเบียนผ่าน `IConfiguration` (`appsettings.json`) | Resolve `IEncryptionService`, `IThaIdProvider`, `IDopaProvider`, `IEnumerable<IVerificationProvider>` ได้ครบถ้วน | ✅ PASS |
| 26 | `AddIdentityVerification_WhenEncryptionKeyMissing_GeneratesEphemeralKeyAndStillStartsUp` | ทดสอบเริ่มระบบโดยไม่ตั้งค่า `EncryptionKey` (Lazy Fallback) | ระบบ Startup ผ่านปกติโดยสร้าง Ephemeral Key ชั่วคราวให้อัตโนมัติ | ✅ PASS |
| 27 | `AddIdentityVerification_WithOptionsAction_ResolvesAllProvidersFromOptionsObject` | ทดสอบลงทะเบียนผ่าน `Action<IdentityVerificationOptions>` | นำค่าจาก Options ไปสร้าง URL และใช้งาน Provider ได้ถูกต้อง | ✅ PASS |
| 28 | `AddIdentityVerification_WithServiceProviderCallback_CanLoadConfigFromDatabaseService` | ทดสอบลงทะเบียนผ่าน `Action<IServiceProvider, IdentityVerificationOptions>` | ดึงค่า Config จาก Service จำลองฐานข้อมูลได้สำเร็จ | ✅ PASS |
| 29 | `AddIdentityVerification_WithCustomOptionsProvider_SupportsDynamicDatabaseReloadPerCall` | ทดสอบ `AddIdentityVerification<TOptionsProvider>()` เมื่อเปลี่ยนค่าใน Database ขณะรัน | ค่า `ClientId` และ URL เปลี่ยนตามฐานข้อมูลทันทีแบบ Hot-Reload โดยไม่ต้อง Restart | ✅ PASS |
| 30 | `AddIdentityVerification_WithScopedDatabaseService_ResolvesScopedRepositoryWithoutError` | ทดสอบดึง Scoped Service (เช่น EF Core `DbContext` / `ISettingRepository`) ในบล็อกตั้งค่า DI | ไม่เกิด `Cannot resolve scoped service from root provider` เพราะ SDK สร้าง Scope ให้ภายในอัตโนมัติ | ✅ PASS |
| 31 | `AddIdentityVerification_WhenDatabaseHasEmptySettingsAtStartup_StartsUpSuccessfullyAndFailsLazilyOnCall` | ทดสอบเมื่อตาราง Settings ใน Database ยังว่างเปล่าตอนเปิดระบบครั้งแรก | แอปพลิเคชัน Startup ผ่านปกติ และเมื่อเรียกใช้งานจะคืน `ErrorCode = "MISSING_CONFIGURATION"` แทนการทำแอปล่ม | ✅ PASS |
| 32 | `AddIdentityVerification_WithServiceProviderCallback_PrePopulatesFromIConfigurationAndAllowsDatabaseOverride` | ทดสอบผสมผสานค่าเริ่มต้นจาก `appsettings.json` แล้วเขียนทับเฉพาะบางค่าจาก Database | ค่าที่ไม่ได้อยู่ใน DB ใช้ค่าจาก `appsettings.json` ส่วนค่าที่มีใน DB ใช้ค่าจาก DB | ✅ PASS |

---

### หมวดที่ 4: `SecurityTests` (6 Test Cases)
ทดสอบมาตรฐานความปลอดภัย PDPA (AES-256-CBC + HMAC-SHA256) และ PKCE (RFC 7636)

| ลำดับ | ชื่อ Test Method | สิ่งที่ทดสอบ (Test Objective) | ผลลัพธ์ที่คาดหวัง | สถานะ |
| :---: | :--- | :--- | :--- | :---: |
| 33 | `AesEncryption_EncryptAndDecrypt_ReturnsOriginalPlainText` | เข้ารหัสและถอดรหัสข้อความภาษาไทยและเลขบัตรประชาชน 13 หลัก | ถอดรหัสกลับมาได้ข้อความเดิมตรงกัน 100% | ✅ PASS |
| 34 | `AesEncryption_SameInputTwice_ProducesDifferentCipherTextsDueToRandomIv` | เข้ารหัสข้อมูลเดียวกัน 2 ครั้งติดกัน | ได้ Ciphertext ไม่ซ้ำกันเลย (เพราะสุ่ม IV ใหม่ทุกครั้ง) | ✅ PASS |
| 35 | `AesEncryption_WhenCipherTextTampered_ThrowsCryptographicException` | ทดสอบดัดแปลงแก้ไขข้อมูล Ciphertext ระหว่างทาง (Tamper Detection) | HMAC-SHA256 ตรวจจับได้และโยน `CryptographicException` ทันที | ✅ PASS |
| 36 | `AesEncryption_WithInvalidKeyLength_ThrowsArgumentException` | ทดสอบส่งคีย์ที่มีขนาดไม่ใช่ 32 bytes (256 bits) | โยน `ArgumentException` | ✅ PASS |
| 37 | `PkceHelper_GenerateVerifierAndChallenge_VerifiesSuccessfully` | ทดสอบสร้าง `code_verifier` และ `code_challenge` (`S256`) | ตรวจสอบผ่านด้วย `PkceHelper.VerifyCodeChallenge` | ✅ PASS |
| 38 | `PkceHelper_GenerateCodeChallenge_MatchesRfc7636AppendixBTestVector` | ทดสอบคำนวณค่า SHA-256 Base64Url เทียบกับค่ามาตรฐานในเอกสาร **RFC 7636 Appendix B** | ได้ค่าตรงตามมาตรฐานสากล RFC 7636 ทุกตัวอักษร | ✅ PASS |

---

### หมวดที่ 5: `ProviderHelperTests` (3 Test Cases)
ทดสอบฟังก์ชันช่วยเหลือ (Modulo 11 Checksum และ JSON Parser)

| ลำดับ | ชื่อ Test Method | สิ่งที่ทดสอบ (Test Objective) | ผลลัพธ์ที่คาดหวัง | สถานะ |
| :---: | :--- | :--- | :--- | :---: |
| 39 | `IsValidThaiNationalId_ValidatesCorrectly` | ทดสอบสูตรคำนวณ Modulo 11 ของเลขบัตรประชาชน 13 หลัก ทั้งเลขที่ถูกและผิด | คืน `true` เฉพาะเลข 13 หลักที่ Checksum ถูกต้อง | ✅ PASS |
| 40 | `ParseJsonToDictionary_PreservesNestedObjectsArraysBooleansAndNumbers` | ทดสอบแปลง JSON ที่มี Nested Object (`address`), Array, ตัวเลข, และ Boolean | รักษาชนิดข้อมูลและโครงสร้าง JSON ต้นทางครบถ้วน | ✅ PASS |
| 41 | `GetStringValue_FindsFirstMatchingKeyCaseInsensitively` | ทดสอบดึงค่าจาก Dictionary แบบไม่สนใจตัวพิมพ์เล็ก-ใหญ่ | ดึงค่าได้ถูกต้องตามลำดับ Key ที่ระบุ | ✅ PASS |

---

### หมวดที่ 6: `RuntimeCompatibilityTests` (2 Test Cases)
ทดสอบความเข้ากันได้ของ Multi-Targeting Assembly

| ลำดับ | ชื่อ Test Method | สิ่งที่ทดสอบ (Test Objective) | ผลลัพธ์ที่คาดหวัง | สถานะ |
| :---: | :--- | :--- | :--- | :---: |
| 42 | `SdkAssembly_IsCompiledForCurrentTargetFramework` | ตรวจสอบ `TargetFrameworkAttribute` ของ `Netka.Identity.SDK.dll` ในแต่ละ Runtime | ตรงกับ `.NETCoreApp,Version=v5.0`, `v8.0`, และ `v10.0` ตามลำดับ | ✅ PASS |
| 43 | `SdkCoreTypes_CanBeInstantiatedOnCurrentRuntime` | ทดสอบสร้าง Instance และเรียกใช้งานฟีเจอร์หลักบน Runtime ปัจจุบัน | ทำงานได้สมบูรณ์ทั้งบน .NET 5.0, .NET 8.0 และ .NET 10.0 | ✅ PASS |
