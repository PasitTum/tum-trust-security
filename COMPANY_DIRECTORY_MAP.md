# TUM Group Holdings — Company Directory Map & Navigation Guide
## แผนผังโครงสร้างโฟลเดอร์แยกตามรายบริษัท (Executive Navigation Map)

> **เรียน:** ท่านประธานกรรมการบริหาร (Chairman of the Board - คุณ Pasit)  
> **และ:** ท่านประธานเจ้าหน้าที่บริหาร (Chief Executive Officer - CEO)  
> **วัตถุประสงค์:** จัดระเบียบโฟลเดอร์ทั้งหมดในโปรเจกต์ให้แยกขาดตามรายบริษัทและเสาหลักทางธุรกิจอย่างชัดเจน แก้ปัญหาความสับสนในการเข้าถึงโค้ดและเอกสาร

---

## 1. ตารางสรุปภาพรวมโฟลเดอร์หลัก (Master Directory Overview)

เมื่อเปิดเข้ามายังรากฐานของโปรเจกต์ (`D:\TUM\tum-trust-security`) โครงสร้างจะถูกจัดเรียงตามลำดับเสาหลักธุรกิจทันที:

| ลำดับโฟลเดอร์ | ชื่อโฟลเดอร์ | บริษัท / หน่วยงานที่รับผิดชอบ | พันธกิจและขอบเขตงานหลัก |
| :---: | :--- | :--- | :--- |
| **`01_`** | **[`01_TUM_Trust_Security/`](file:///D:/TUM/tum-trust-security/01_TUM_Trust_Security/)** | **TUM Trust Security Co., Ltd.** | การพิสูจน์และยืนยันตัวตนแห่งชาติ (ThaID, DOPA, Borderless), ตู้คีออสอ่านสมาร์ตการ์ด (PC/SC HAL), พาสปอร์ต ICAO 9303 NFC, Edge Face Liveness |
| **`02_`** | **[`02_TUM_Socs/`](file:///D:/TUM/tum-trust-security/02_TUM_Socs/)** | **TUMSocs Co., Ltd.**<br/>*(วิวัฒนาการจาก TUMLog)* | ศูนย์บัญชาการไซเบอร์เชิงรุก (Active Cyber Defense), วิเคราะห์บริบท Where/Why/Risk, Ledger ทรงหลักฐาน (Merkle Hash Tree), ตัดต่อภัยคุกคามอัตโนมัติ |
| **`03_`** | **[`03_TUM_Quantum/`](file:///D:/TUM/tum-trust-security/03_TUM_Quantum/)** | **TUMQuantum Co., Ltd.**<br/>*(อนาคตของกลุ่ม TUM)* | ศูนย์วิจัยวิทยาการรหัสลับหลังควอนตัม (PQC), มาตรฐาน NIST FIPS 203 (ML-KEM) & FIPS 204 (ML-DSA), อะแดปเตอร์เชื่อมต่อระบบเก่าแบบไม่ทอดทิ้งใคร (Hybrid Migration) |
| **`04_`** | **[`04_Central_Director/`](file:///D:/TUM/tum-trust-security/04_Central_Director/)** | **Central Director Team** | หน้าจอภาพเสมือนจริงผู้บริหาร (Interactive Cyber Attack Map & Executive HUD), แดชบอร์ดสรุปความปลอดภัยรวม, ระบบรายงานความสอดคล้องคลิกเดียว |
| **`05_`** | **[`05_TUM_Group_Holdings/`](file:///D:/TUM/tum-trust-security/05_TUM_Group_Holdings/)** | **TUM Group Central / Holdings** | การกำกับดูแลกิจการ (Governance & BoD Resolutions), สภาวิจัยและนวัตกรรม (TUM Innovation Council), ฝ่ายบุคคลและการสรรหา (People & Culture / HR), แผนยุทธศาสตร์และ OKRs, สไลด์ Working Group |
| — | **[`Tests/`](file:///D:/TUM/tum-trust-security/Tests/)** | **Unified Test Suite** | ชุดทดสอบรวมระบบทั้งหมด 125 ชุดทดสอบ (xUnit) ครอบคลุมทั้ง Trust Security, TUMSocs, และ Quantum ผ่าน 100% |
| — | **[`MockServer/`](file:///D:/TUM/tum-trust-security/MockServer/)** | **Developer Local Sandbox** | เซิร์ฟเวอร์จำลอง ThaID OAuth 2.0 PKCE และ DOPA Sandbox สำหรับทดสอบออฟไลน์โดยไม่ต้องใช้คีย์จริง |
| — | **[`examples/`](file:///D:/TUM/tum-trust-security/examples/)** | **Integration Quickstarts** | โค้ดตัวอย่างการนำ SDK ไปเรียกใช้งานในแอปพลิเคชันจริงของลูกค้า |

---

## 2. เจาะลึกโครงสร้างภายในแต่ละบริษัท

### 🏢 1. TUM Trust Security (`01_TUM_Trust_Security/`)
- `Application/`: บริการ Business Logic (IdentityVerificationService, DopaService, ThaIdService)
- `Core/`: โดเมนโมเดล ข้อกำหนดสัญญาระบบ (Interfaces) และ Entity ตรวจสอบตัวตน
- `Infrastructure/`: ไดรเวอร์เชื่อมต่อฮาร์ดแวร์ตู้คีออส (PC/SC), เครื่องสแกนใบหน้า, และ HTTP Clients
- `docs/`: ข้อกำหนด API ผู้ให้บริการ (providers/thaid.md, dopa.md, borderless.md) และคู่มือผู้พัฒนา (developer-portal/)
- `Tum.Trust.Security.csproj`: โค้ดไลบรารี SDK หลักที่ส่งออกเป็น NuGet Package

### 🛡️ 2. TUMSocs (`02_TUM_Socs/`)
- `Tum.Log/`: เครื่องยนต์บันทึกหลักฐานความปลอดภัย (TamperProofLogChain, Merkle Tree, Ingestion RingBuffer)
- `Tum.Log.csproj`: ไลบรารีความปลอดภัยสำหรับติดตั้งในเซิร์ฟเวอร์
- `docs/`: มาตรฐานการเก็บบันทึก ISO 27001 (A.12.4), พ.ร.บ.คอมพิวเตอร์ฯ มาตรา 26, และ PDPA
- `docs/tumlog_dashboard.html`: หน้าจอแสดงทราฟฟิกและ Log สด

### ⚛️ 3. TUMQuantum (`03_TUM_Quantum/`)
- `Core/`: ข้อมูลกุญแจรหัสลับควอนตัม (PqcKeyPair) และโมเดลผลการตรวจสอบ (PqcVerificationResult)
- `Engine/`: เครื่องยนต์แลตทิซ NIST FIPS 204 (ML-DSA Dilithium-44/65) และ FIPS 203 (ML-KEM Kyber)
- `Tum.Quantum.csproj`: ไลบรารีรหัสลับทนทานคอมพิวเตอร์ควอนตัม
- `docs/`: รายงานวิจัยและสูตรคณิตศาสตร์แลตทิซ

### 🌐 4. Central Director (`04_Central_Director/`)
- `visualizers/`: หน้าเว็บแผนที่สงครามไซเบอร์ (Interactive Cyber Attack Map)
- `docs/`: ธรรมนูญทีมและสถาปัตยกรรมระบบรวมศูนย์

### 🏛️ 5. TUM Group Holdings (`05_TUM_Group_Holdings/`)
- `Governance/`: 
  - มติที่ประชุมบอร์ดและพิมพ์เขียวการจัดตั้งเครือ TUM (`TUM_GROUP_REBRANDING_AND_MIGRATION_BLUEPRINT.md`)
  - **ธรรมนูญสภาการวิจัยและนวัตกรรมแห่งเครือ TUM (`TUM_INNOVATION_AND_RESEARCH_COUNCIL_CHARTER.md`)** *(ทางเลือกที่ 3: R&D Guild & Innovation Council)*
  - โครงสร้างองค์กรและสายบังคับบัญชา (`ORGANIZATION_AND_OPERATING_MODEL.md`)
- `People_and_Culture/`: ข้อมูลฝ่ายบุคคล, ประกาศรับสมัคร **Scrum Master** (`JOB_SPEC_SCRUM_MASTER.md`), รายงานความคืบหน้าการรับสมัคร (`RECRUITMENT_PROGRESS_REPORT_SCRUM_MASTER.md`)
- `Strategy_and_OKRs/`: เป้าหมาย OKRs ปี 2026-2027 ของเครือและทั้ง 3 บริษัท
- `Presentations/`: สไลด์นำเสนอ Working Group (`tum_working_group_sprint_1_and_2.html` และ Markdown)
- `Townhall/`: สรุปการประชุม Town Hall ประจำงวด
- `Frameworks/`: ระเบียบวิธีปฏิบัติงานแบบ TUM Shape Up (รอบละ 6 สัปดาห์ + 2 สัปดาห์ Cooldown)
- `Research/`: คลังงานวิจัย Tri-Temporal Deep Research (อดีต ปัจจุบัน อนาคต)

---

## 3. ความพร้อมทางเทคนิคและการรับรองคุณภาพ (Technical Quality Gates)

โครงสร้างโฟลเดอร์ใหม่ผ่านการตรวจสอบตามมาตรฐาน **Verification Loop** ครบถ้วน:
- ✅ **Multi-Targeted Build:** `dotnet build --configuration Release` สำเร็จ **0 ข้อผิดพลาด (0 Errors)**
- ✅ **Comprehensive Test Suite:** `dotnet test -f net8.0` ผ่านทั้งหมด **125/125 ชุดทดสอบ (100% Passed)**
- ✅ **Clean Solution Linking:** Solution File `Tum.Trust.Security.sln` ผูกโยงทุกโปรเจกต์อย่างถูกต้อง
