# สารบัญเอกสารการเชื่อมต่อ Identity Providers (`docs/providers/`)

โฟลเดอร์นี้เก็บข้อมูลอ้างอิงทางเทคนิค (API Specification, แหล่งอ้างอิง Endpoint จริง, การตั้งค่า `appsettings.json` / Database, พารามิเตอร์ขาเข้า `VerificationRequest` และโครงสร้างข้อมูลขาออก `VerificationResult.Data`) ของแต่ละ Provider ใน `Netka.Identity.SDK`

> **กฎเหล็กสำคัญสำหรับ AI Agents / Claude Code (ห้ามละเมิดโดยเด็ดขาด):**
> 1. **ห้ามลบหรือเขียนทับข้อมูลอ้างอิงเดิม (Append-Only Documentation Rule):** เมื่ออัปเดตไฟล์ใน `docs/providers/*.md` **ห้ามลบ** URL อ้างอิง, ข้อมูล OpenID Configuration, รายชื่อ Scopes, ตารางพารามิเตอร์ หรือตัวอย่างที่มีอยู่เดิมโดยเด็ดขาด ให้ใช้วิธี **เพิ่มเติม (Append/Expand)** เท่านั้น
> 2. **ห้ามตัดทอนหรือเลือกทำเฉพาะบาง Endpoint (100% Full Provider Endpoint Rule):** โปรเจคนี้คือ Middleware ตัวกลางของทีม เมื่อเพิ่มหรืออัปเดต Provider ใดๆ ต้องรองรับ Endpoint และโหมดการยืนยันตัวตน **ครบ 100% ทุกตัว** ตามสเปกของ Provider นั้น ห้ามตัดสินใจตัด Endpoint ใดออกเองโดยเด็ดขาด
> 3. **ห้ามใส่ Key หรือ Secret จริงลงในเอกสารเด็ดขาด (Strict No-Secret In Docs Rule):** ห้ามนำ API Key, Consumer-Key, ConsumerSecret, Client Secret, Private Key, Encryption Key หรือ Secret จริงใดๆ มาบันทึกหรือใส่ลงในโฟลเดอร์ `docs/providers/` โดยเด็ดขาด ทั้งในไฟล์ `.md` และ `.http` ให้ใช้ Placeholder เช่น `<YOUR_API_KEY>`, `<YOUR_CLIENT_SECRET>` หรือ `{{variable}}` เท่านั้น
> 4. **เมื่อใช้ Skill `/add-identity-provider`:** ให้สร้างหรืออัปเดตไฟล์ `<provider-name>.md` ในโฟลเดอร์นี้ทุกครั้งที่มีการเพิ่ม Provider ใหม่ และเพิ่มรายชื่อลงในตารางด้านล่าง
> 5. **เมื่อใช้ Skill `/integrate-netka-identity`:** ให้อ่านไฟล์ของ Provider ที่ต้องการเชื่อมในโฟลเดอร์นี้ เพื่อดูชื่อ Key ใน `appsettings.json` / `IdentityVerificationOptions`, พารามิเตอร์ใน `VerificationRequest`, และชื่อ Key ที่อยู่ใน `VerificationResult.Data`

## รายชื่อ Providers ที่รองรับปัจจุบัน

| Provider Key | ชื่อระบบ | Interface | Endpoints ที่รองรับทั้งหมด | เอกสารอ้างอิง (Spec + Request/Response) | ไฟล์ยิงทดสอบ VS Code (`REST Client`) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `"ThaID"` | ThaID (DOPA Digital ID — OAuth 2.0 + PKCE + OIDC) | `IThaIdProvider` | `GetAuthorizationUrl`, `ExchangeCodeAsync`, `RefreshTokenAsync`, `InspectTokenAsync`, `RevokeTokenAsync`, `GetUserInfoAsync`, `GetOpenIdConfigurationAsync`, `GetJwksAsync`, `ValidateIdTokenAsync`, `VerifyAsync` | [thaid.md](./thaid.md) | [thaid.http](./thaid.http) |
| `"DOPA"` | กรมการปกครอง (ตรวจสอบสถานะบัตร Laser / Chip / เปรียบเทียบใบหน้า 1:1) | `IDopaProvider` | `VerifyByLaserAsync`, `VerifyByChipAsync`, `VerifyByFaceAsync`, `RequestAccessTokenAsync`, `VerifyAsync` | [dopa.md](./dopa.md) | [dopa.http](./dopa.http) |

---

## วิธีใช้ยิง API ทดสอบผ่าน VS Code Extension (`REST Client`)

### ขั้นตอนที่ 1: รัน MockServer จำลอง (ไม่ต้องสนตำแหน่ง Path / ย้ายเครื่องก็รันได้ทันที)
ก่อนยิงทดสอบแบบ Local จำลอง สามารถเปิด MockServer ได้หลายวิธี:
- **วิธีที่ 1 (VS Code Task - สะดวกที่สุด):** ใน Markdown Preview ให้คลิก **[▶️ กดรัน MockServer ทันที](command:workbench.action.tasks.runTask?%5B%22Run%20MockServer%22%5D)** หรือกด `Ctrl+Shift+P` ➔ พิมพ์ `Tasks: Run Task` ➔ เลือก `Run MockServer` (ระบบคำนวณ Path `${workspaceFolder}` ให้อัตโนมัติทุกเครื่อง)
- **วิธีที่ 2 (ดับเบิลคลิกสคริปต์ 1-Click):**
  - บน Windows: ดับเบิลคลิก [`run-mockserver.cmd`](../../run-mockserver.cmd) หรือ [`run-mockserver.ps1`](../../run-mockserver.ps1)
  - บน Mac/Linux: รัน [`./run-mockserver.sh`](../../run-mockserver.sh)
- **วิธีที่ 3 (Terminal คำสั่งเดียวจากทุกโฟลเดอร์):**
  ```bash
  dotnet run --project "$((git rev-parse --show-toplevel)/MockServer/Netka.Identity.MockServer.csproj)"
  ```

### ขั้นตอนที่ 2: ติดตั้ง Extension และยิงทดสอบ
1. ติดตั้ง Extension **[REST Client (`humao.rest-client`)](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)** ใน VS Code
2. สามารถคลิกปุ่ม **`Send Request`** ได้ทั้ง 2 รูปแบบ:
   - **แบบที่ 1:** เปิดไฟล์ **[`thaid.http`](./thaid.http)** หรือ **[`dopa.http`](./dopa.http)** แล้วคลิกปุ่ม `Send Request` เหนือหัวข้อ `###` ที่ต้องการทดสอบ
   - **แบบที่ 2:** เปิดอ่านไฟล์คู่มือ **[`thaid.md`](./thaid.md)** หรือ **[`dopa.md`](./dopa.md)** ในโหมดแก้ไขของ VS Code แล้วคลิกปุ่ม `Send Request` เหนือบล็อก ` ```http ` ของแต่ละ Endpoint ได้โดยตรง

