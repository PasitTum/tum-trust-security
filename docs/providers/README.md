# สารบัญเอกสารการเชื่อมต่อ Identity Providers (`docs/providers/`)

โฟลเดอร์นี้เก็บข้อมูลอ้างอิงทางเทคนิค (API Specification, การตั้งค่า `appsettings.json`, พารามิเตอร์ขาเข้า `VerificationRequest` และโครงสร้างข้อมูลขาออก `VerificationResult.Data`) ของแต่ละ Provider ใน `Netka.Identity.SDK`

> **สำคัญสำหรับ AI Agents / Claude Code:**
> - **เมื่อใช้ Skill `/add-identity-provider`:** ให้สร้างหรืออัปเดตไฟล์ `<provider-name>.md` ในโฟลเดอร์นี้ทุกครั้งที่มีการเพิ่ม Provider ใหม่ และเพิ่มรายชื่อลงในตารางด้านล่าง
> - **เมื่อใช้ Skill `/integrate-netka-identity`:** ให้อ่านไฟล์ของ Provider ที่ต้องการเชื่อมในโฟลเดอร์นี้ เพื่อดูชื่อ Key ใน `appsettings.json`, พารามิเตอร์ใน `VerificationRequest`, และชื่อ Key ที่อยู่ใน `VerificationResult.Data`

## รายชื่อ Providers ที่รองรับปัจจุบัน

| Provider Key | ชื่อระบบ | Interface | เอกสารอ้างอิง |
| :--- | :--- | :--- | :--- |
| `"ThaID"` | ThaID (DOPA Digital ID — OAuth 2.0 + PKCE) | `IThaIdProvider` | [thaid.md](./thaid.md) |
| `"DOPA"` | กรมการปกครอง (ตรวจสอบสถานะบัตรประชาชน / Laser ID) | `IDopaProvider` | [dopa.md](./dopa.md) |
