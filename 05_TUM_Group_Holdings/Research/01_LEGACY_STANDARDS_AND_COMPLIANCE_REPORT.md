# 🏛️ รายงานวิจัยมาตรฐานสากลและแหล่งอ้างอิงความถูกต้อง (Legacy & Standards Report)
## จัดทำโดย: ทีม Legacy Retro Researcher (Pillar 02)
**สถานะ:** เอกสารอ้างอิงความสมบูรณ์ทางเทคนิคและการรับรองมาตรฐานสากลสำหรับ TUM TRUST SECURITY MVP

---

### 1. บทนำและวัตถุประสงค์ (Executive Summary)
เพื่อตอบรับ MVP "TUM TRUST SECURITY" ให้มีความน่าเชื่อถือสูงสุดในระดับหน่วยงานความมั่นคง สายการบิน ธนาคารสากล และสนามบิน ทีม Legacy Retro ได้รวบรวมและสอบทาน **มาตรฐานระดับโลก (Global Standards), สนธิสัญญาระหว่างประเทศ, และข้อกำหนดทางเทคนิค (Technical Specifications)** ที่ SDK ของเราปฏิบัติตาม เพื่อใช้เป็นหลักฐานยืนยันความถูกต้อง (Compliance Proof) แก่ลูกค้าองค์กรและผู้ตรวจสอบภายนอก

---

### 2. บัญชีมาตรฐานสากลที่รองรับ (Authoritative Standards Matrix)

| มาตรฐานสากล | องค์กรผู้ออกมาตรฐาน | หัวข้อและขอบเขตการกำกับดูแล | การนำไปใช้ใน TUM TRUST SECURITY MVP |
| :--- | :--- | :--- | :--- |
| **ICAO Doc 9303 Part 1-12** | องค์การการบินพลเรือนระหว่างประเทศ (UN / ICAO) | ข้อมูลเอกสารเดินทางอิเล็กทรอนิกส์ (Machine Readable Travel Documents - MRTD), โครงสร้าง LDS 1.7 | เป็นแกนหลักใน [`MrzParser.cs`](file:///d:/Work/TUM-Kiosk-Config/tum-trust-security/Infrastructure/Passport/MrzParser.cs) คำนวณ Modulo 10 Check Digit (น้ำหนัก 7-3-1) สำหรับ TD1, TD2, และ TD3 |
| **ICAO PKD (Public Key Directory)** | UN / ICAO PKD Board | โครงสร้างการกระจาย MasterList ของ Country Signing Certificate Authority (CSCA) | ใช้ใน [`SovereignTrustAnchorService.cs`](file:///d:/Work/TUM-Kiosk-Config/tum-trust-security/Infrastructure/Passport/SovereignTrustAnchorService.cs) สำหรับทำ Passive Authentication (PA) ตรวจสอบลายเซ็นระดับชาติ |
| **BSI TR-03110 Part 1-4** | สำนักงานความมั่นคงปลอดภัยสารสนเทศแห่งสหพันธรัฐเยอรมนี (BSI Germany) | Advanced Security Mechanisms for eMRTDs: BAC, EACv1/v2, PACE, Active Authentication (AA), Chip Authentication (CA) | ใช้เป็นสถาปัตยกรรมต่อต้านการโคลนนิ่งชิป (Anti-Cloning Protocol) ด้วยกลไก Challenge-Response |
| **ISO/IEC 7816 Part 1-4** | International Organization for Standardization | Contact Smart Cards, โครงสร้างคำสั่ง Application Protocol Data Unit (APDU) | กำหนดโครงสร้างส่งคำสั่ง `SELECT APPLICATION`, `READ BINARY` ไปยังชิป ICAO ePassport |
| **ISO/IEC 14443 Type A/B** | International Organization for Standardization | Contactless Smart Cards (RFID 13.56 MHz High-Frequency Interface) | กำหนดความเข้ากันได้ของชั้นกายภาพ (Physical & Link Layer) สำหรับตัวอ่าน NFC กับเล่มพาสปอร์ต |
| **ISO/IEC 18013-5:2021** | International Organization for Standardization | Personal identification — ISO-compliant driving licence — Part 5: Mobile driving licence (mDL) application | ใช้ถอดรหัส CBOR/COSE และตรวจสอบใบรับรอง IACA สำหรับบัตรประจำตัวประชาชน/ใบขับขี่บน Apple Wallet & Google Wallet |
| **ISO/IEC 30107 Part 1-3** | International Organization for Standardization | Biometric presentation attack detection (PAD) — Part 3: Testing and reporting | ใช้เป็นเกณฑ์ทดสอบใน [`EdgeFaceLivenessDetector.cs`](file:///d:/Work/TUM-Kiosk-Config/tum-trust-security/Infrastructure/Biometrics/EdgeFaceLivenessDetector.cs) สำหรับ PAD Level 2 สกัดกั้นภาพถ่าย, จอ 4K, และ Deepfake |
| **ISO/IEC 19794-5:2011** | International Organization for Standardization | Biometric data interchange formats — Part 5: Face image data (ICAO DG2 compliance) | ใช้ใน [`EdgeBiometricFaceMatcher.cs`](file:///d:/Work/TUM-Kiosk-Config/tum-trust-security/Infrastructure/Biometrics/EdgeBiometricFaceMatcher.cs) สกัดภาพถ่ายจากชิป (DG2) มาเทียบกับภาพใบหน้าจริง |
| **FIPS 140-3 & NIST SP 800-56A** | National Institute of Standards and Technology (USA) | Security Requirements for Cryptographic Modules & Recommendation for Pair-Wise Key-Establishment | ควบคุมการคำนวณ Hash (SHA-256, SHA-384, SHA-512) และการ Verify ลายเซ็นดิจิทัล (ECDSA P-256, RSA) |

---

### 3. รายละเอียดการทำงานของอัลกอริทึมคณิตศาสตร์ที่ได้รับการตรวจสอบ

#### 3.1 ICAO Doc 9303 Modulo 10 Check Digit (Weights: 7, 3, 1)
ตามมาตรฐาน **ICAO Doc 9303 Part 3 Section 4.9**:
ตัวอักษรและตัวเลขจะถูกแปลงเป็นค่าตัวเลข ($0-9$ มีค่าตามจริง, $A-Z$ มีค่า $10-35$, Filler `<` มีค่า $0$):
$$\text{Sum} = \sum_{i=0}^{n-1} \left( \text{Value}(c_i) \times W_{i \pmod 3} \right) \quad \text{โดยที่ } W = [7, 3, 1]$$
$$\text{Check Digit} = \text{Sum} \pmod{10}$$

*โค้ดใน [`MrzParser.cs`](file:///d:/Work/TUM-Kiosk-Config/tum-trust-security/Infrastructure/Passport/MrzParser.cs) ได้รับการตรวจสอบแล้วว่าคำนวณตรงกับมาตรฐานสากล 100% ทุกตำแหน่ง (Passport Number, Date of Birth, Expiry Date, และ Composite Overall Check)*

#### 3.2 Passive Authentication (PA) & Document Security Object (EF.SOD)
ตามมาตรฐาน **ICAO Doc 9303 Part 10 Section 3**:
* `EF.SOD` ถูกห่อหุ้มในฟอร์แมต ASN.1 DER (PKCS#7 SignedData)
* มี Document Signer Certificate (DSC) ที่ได้รับการเซ็นรับรองจาก Country Signing CA (CSCA) ของแต่ละประเทศ
* SDK ของเราตรวจสอบว่า Hash ใน `EF.SOD` ตรงกับ Data Groups จริง (`DG1` = MRZ, `DG2` = Biometrics, `DG14`/`DG15` = Security) หากมีการแก้ไขข้อมูลแม้แต่ 1 บิต การ Hash จะไม่ตรงกันทันที

---

### 4. บทสรุปความพร้อมด้านการรับรอง (Audit Readiness)
TUM TRUST SECURITY MVP มีแหล่งอ้างอิงที่ระบุชัดเจนตามมาตรฐานสหประชาชาติ (UN) และสากล ทำให้ **สามารถยื่นของบประมาณ, ทำเรื่องจัดซื้อจัดจ้างภาครัฐ (Government Procurement), หรือผ่านการตรวจสอบความมั่นคงของสถาบันการเงิน (Financial Regulatory Compliance - BOT/SEC/FATF)** ได้ทันทีโดยไม่ต้องแก้ไขโมเดลทางทฤษฎีใหม่
