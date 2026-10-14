# บันทึกแนวทางการปรับปรุงสถาปัตยกรรม (Architecture Decision Note)
**เรื่อง:** การปรับปรุงการจัดเก็บ Configuration & Secret Management ในอนาคต  
**ผู้บันทึก:** ทีมสถาปัตยกรรมและความมั่นคงปลอดภัย TUM Identity SDK  
**วันที่:** 14 ตุลาคม 2026  

---

## 1. ที่มาและปัญหา (Context & Problem Statement)
ในปัจจุบัน ระบบตู้ Kiosk มีการกำหนดตัวแปรคอนฟิกในตาราง `variables` เป็นแบบ Flat Keys จำนวนถึง **36 ตัวแปร** ซึ่งพบข้อสังเกตและประเด็นความเสี่ยงสำคัญดังนี้:

1. **ความเสี่ยงด้านความมั่นคงปลอดภัย (Critical Security Risk - Plaintext Secrets in Database):**
   - มีการบันทึกค่า Secret สำคัญระดับประเทศ เช่น `kiosk_thaid_client_secret`, `kiosk_thaid_api_key`, และ `kiosk_dopa_consumer_secret` ลงในตารางฐานข้อมูลโดยตรง
   - **ประเด็นความเสี่ยง:** หากฐานข้อมูลหลุด (Database Leak) ไม่ว่าจะเกิดจาก SQL Injection, ไฟล์ Backup หลุด, หรือมีผู้ไม่ประสงค์ดีถอด Harddisk จากตู้ Kiosk ประจำสาขา ผู้โจมตีจะได้กุญแจลับไปสวมรอยเรียกข้อมูลประชาชนจากระบบ DOPA ได้ทันที
2. **ภาระในการดูแลระบบ (Configuration Management Sprawl):**
   - ตัวแปร 36 ตัวแปรทำให้หน้าจอ BackOffice มีความซับซ้อน และเสี่ยงต่อการกรอกค่าผิดพลาด (Human Error)

---

## 2. ข้อเสนอแนะเชิงสถาปัตยกรรมในอนาคต (Future Architectural Roadmap)

### เฟสที่ 1: แยก Configuration ออกจาก Secret (Principle of Least Privilege)
แบ่งข้อมูล 36 ตัวแปรออกเป็น 2 กลุ่มอย่างชัดเจน:

#### ก. ข้อมูลทั่วไปที่ไม่เป็นความลับ (Non-Sensitive Configuration)
- **ตัวอย่าง:** `redirect_uri`, `scope`, `face_threshold`, `use_mock`, `mock_scenario`
- **แนวทางจัดเก็บ:** รวมเป็น **JSON Config Block** เหลือเพียง 2 แถวในตาราง `variables`:
  1. `kiosk_thaid_config`
  2. `kiosk_dopa_config`
- **ข้อดี:** ลดแถวในฐานข้อมูลจาก 36 เหลือ 2 แถว และ C# สามารถ Deserialize เข้า Options Object ของ SDK ได้ในบรรทัดเดียว

#### ข. ข้อมูลความลับระดับสูง (Sensitive Secrets)
- **ตัวอย่าง:** `client_secret`, `api_key`, `consumer_secret`, `identity_encryption_key`
- **แนวทางจัดเก็บที่ปลอดภัย (เลือกตามความเหมาะสม):**
  - **แนวทาง A (Enterprise Best Practice):** ย้ายออกจาก Database ไปเก็บใน **OS Environment Variables** หรือ **Windows DPAPI / Machine KeyStore** ของตัวตู้ Kiosk ซึ่งเข้าถึงได้เฉพาะสิทธิ์ระดับ Administrator / Local Service เท่านั้น แม้ฐานข้อมูลจะหลุดไป ก็จะไม่มีกุญแจลับติดไปด้วย
  - **แนวทาง B (Data-at-Rest Encryption):** หากจำเป็นต้องเก็บบน Database เพื่อให้หน้าจอ Web Admin แก้ไขได้ ต้องบังคับเข้ารหัสด้วยอัลกอริทึม `AES-256-GCM` โดยใช้คีย์ถอดรหัส (`identity_encryption_key`) ที่เก็บอยู่นอกฐานข้อมูลเสมอ

---

### เฟสที่ 2: ใช้หลักการ Convention Over Configuration
- ย้าย Endpoint URLs ที่เป็นค่าตายตัวของ DOPA เช่น `authorize_url`, `token_url`, `userinfo_url`, `jwks_url` ไปเป็น **Built-in Default ใน SDK** หรืออ่านอัตโนมัติจาก Discovery Endpoint (`.well-known/openid-configuration`)
- **ผลลัพธ์:** ลดตัวแปรที่คนต้องมากรอกหน้าตู้ Kiosk จาก 36 ตัว เหลือเพียง **4-5 ตัวที่จำเป็นจริงๆ** (เช่น Client ID, Secret, Redirect URI)

---

### เฟสที่ 3: ค้นหาและรวมผู้ให้บริการด้านตัวตน (Provider Discovery & Integration)
นอกจาก ThaID (OAuth 2.0/OIDC) และ DOPA Card Service (REST API) ที่มีอยู่แล้ว ต้องเตรียมโครงสร้างสำหรับเพิ่มผู้ให้บริการรูปแบบใหม่ในอนาคต

#### ก. ระบุผู้ให้บริการที่อาจขยายได้ (Candidate Providers)
1. **Microsoft Entra ID (Azure AD)** — สำหรับโครงการรัฐที่ใช้บริการ Microsoft government cloud
2. **Google One Tap / Identity Services** — สำหรับ B2C portal อื่นๆ ของเอกชน
3. **LINE Login / Facebook Login** — สำหรับช่องทางมือถือและ Social Login ทั่วไป
4. **Apple SignIn** — สำหรับ iOS kiosk ในอนาคต
5. **Smart Card Readers (eIDAS)** — สำหรับการรู้จำบัตรสุดท้ายทั่วโลก (ถ้าขยายไปต่างประเทศ)

#### ข. ไฮบริดสถาปัตยกรรม (Hybrid Architecture)
- **Provider Registry:** ตาราง `identity_providers` ในฐานข้อมูล จดทะเบียนผู้ให้บริการทั้งหมด (enabled/disabled, priority)
- **Provider Factory:** `IProviderFactory` เลือกผู้ให้บริการ Runtime ตามการกำหนดค่า Kiosk
- **Abstraction Layers:**
  - `IVerificationProvider` — Base interface สำหรับทุกผู้ให้บริการ
  - `IOAuthProvider` — สำหรับ OAuth 2.0/OIDC providers (ThaID, Microsoft Entra, Google)
  - `IRestApiProvider` — สำหรับ REST API providers (DOPA Card Service, eIDAS)
  - `ICardReaderProvider` — สำหรับ Physical card readers (Smart Card, mPIC)

#### ค. ส่วนขยาย SDK (SDK Extension Points)
```csharp
// ผู้ใช้ SDK สามารถเพิ่มผู้ให้บริการเอง
public class CustomAzureAdProvider : IOAuthProvider
{
    public async Task<VerificationResult> VerifyAsync(VerificationRequest request, CancellationToken cancellationToken)
    {
        // Implement Azure AD verification logic
    }
}

// ลงทะเบียนใน DI
services.AddIdentityProvider<CustomAzureAdProvider>();
```

---

## 3. สรุปผลลัพธ์ที่จะได้รับ (Expected Business & Technical Benefits)
1. **ยกระดับความปลอดภัย (Zero Plaintext Secrets):** ป้องกันการรั่วไหลของกุญแจสำคัญระดับประเทศ สอดคล้องกับ พ.ร.บ. คุ้มครองข้อมูลส่วนบุคคล (PDPA) และมาตรฐาน ISO/IEC 27001
2. **ลดความซับซ้อน (Clean & Maintainable):** ระบบฐานข้อมูลสะอาดขึ้น 80% ตรวจสอบสถานะและ Audit ได้ง่าย
3. **ลดเวลา Onboarding ตู้ Kiosk ใหม่:** ช่างหน้างานกรอกค่าเพียงไม่กี่ช่อง ตู้ Kiosk ก็พร้อมเปิดให้บริการทันที
4. **ความยืดหยุ่นในการเพิ่มผู้ให้บริการใหม่:** โครงสร้าง provider-agnostic ช่วยให้ขยายรองรับผู้ให้บริการรูปแบบต่างๆ โดยไม่ต้องแก้ไขโค้ด Core
