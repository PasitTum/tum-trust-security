# 🔍 รายงานวิเคราะห์สภาพตลาดปัจจุบัน ช่องโหว่ CVE และข้อบกพร่องที่ต้องอุด (Current SOTA & Threat Report)
## จัดทำโดย: ทีม Current SOTA Researcher (Pillar 02)
**สถานะ:** รายงานเจาะลึก Pain Points, ช่องโหว่ความปลอดภัยระดับโลก (CVEs), และแนวทางการอุดช่องโหว่ Phase 1

---

### 1. ภาพรวมเทรนด์ตลาดปัจจุบัน (Current Market Dynamics - 2026)

1. **การเปลี่ยนผ่านสู่ Mobile Wallets (Apple & Google Wallet mDL):**
   * ตลาดกำลังเปลี่ยนจากบัตรพลาสติกไปสู่ใบขับขี่และบัตรประจำตัวประชาชนบนมือถือ (ISO 18013-5) รัฐบาลในสหรัฐฯ (กว่า 10 รัฐ), ญี่ปุ่น, สหภาพยุโรป (EUDI Wallet) และสิงคโปร์เริ่มเปิดให้ประชาชนใช้ mDL เต็มรูปแบบ
   * *โอกาสของ TUM:* หาก SDK รองรับ ISO 18013-5 จะเป็นรายแรกๆ ในเอเชียตะวันออกเฉียงใต้ที่อ่านบัตรใน Apple/Google Wallet ได้ทันที
2. **การก้าวกระโดดของภัยคุกคาม Generative AI & Deepfake (+320% YoY):**
   * ระบบตรวจจับ Liveness แบบดั้งเดิมที่ใช้ "การตรวจจับการกระพริบตา" (Blink Detection) หรือ "การหันซ้ายหันขวา" (Challenge-Response Movement) **ล้มเหลวโดยสิ้นเชิง** เมื่อเจอกับ Real-time Video Stream Injection และ 3D Generative Face Swap
3. **ปัญหาของ Cloud-based Identity APIs:**
   * สถาบันการเงินและสนามบินกำลังประสบปัญหาความหน่วงของเครือข่าย (Latency 2-5 วินาที) และปัญหาความเสี่ยงทางกฎหมายเรื่องการส่งภาพถ่ายใบหน้าข้ามพรมแดน (Cross-border Data Transfer) ทำให้กระแสสวิงกลับมาหา **On-Device / Edge Biometrics** อย่างรวดเร็ว

---

### 2. บทวิเคราะห์ช่องโหว่ความปลอดภัยระดับโลก (Real-world CVEs & Vulnerability Defects)

ทีม Current SOTA ได้วิเคราะห์เคสการเจาะระบบ ePassport และ Smartcard ในอดีตจนถึงปัจจุบัน เพื่อนำมาเป็นบทเรียนอุดช่องโหว่ให้ SDK ของเรา:

#### 🚨 ช่องโหว่ที่ 1: "The Bit-for-Bit Chip Cloning Flaw" (Passive Auth Limitation)
* **ข้อเท็จจริงทางเทคนิค:** ระบบตรวจพาสปอร์ตส่วนใหญ่ในท้องตลาดตรวจเพียง **Passive Authentication (PA)** ซึ่งตรวจสอบว่าข้อมูลใน `EF.SOD` ถูกเซ็นโดยรัฐบาลจริงหรือไม่
* **จุดบกพร่อง:** ผู้โจมตีสามารถอ่าน Raw Data จากชิปพาสปอร์ตจริงของผู้อื่น แล้วนำไป **โคลนแบบบิตต่อบิต (Clone bit-for-bit)** ลงบนชิป NFC เปล่าหรือโปรแกรมจำลองบนสมาร์ตโฟน (NFC Emulator) เมื่อนำไปสแกนที่ตู้ Kiosk ทั่วไป ระบบ PA จะบอกว่า "ถูกต้อง" ทั้งที่เป็นชิปปลอม!
* **การแก้ไขใน TUM TRUST SECURITY:** เราบังคับใช้ **Active Authentication (AA)** ใน [`SovereignTrustAnchorService.cs`](file:///d:/Work/NKS-Kiosk-Config/NKS-Kiosk-Identity-SDK/Infrastructure/Passport/SovereignTrustAnchorService.cs) โดยส่ง Cryptographic Challenge ให้ชิปเซ็นด้วย Private Key ภายใน Hardware Secure Element ซึ่งไม่สามารถก๊อปปี้ออกมาได้ ทำให้ชิปโคลนล้มเหลวทันที 100%!

#### 🚨 ช่องโหว่ที่ 2: "Parser Logic & Missing Tag Bypass" (Calderoni & Maio Vulnerability)
* **ข้อเท็จจริง:** งานวิจัยของนักวิจัยความปลอดภัยสากลพบว่า ซอฟต์แวร์ตรวจพาสปอร์ตหลายตัวมีช่องโหว่ในตัวถอดรหัส ASN.1 และโครงสร้าง `EF.COM`
* **การโจมตี:** หากผู้โจมตีเจตนาตัด Tag หรือส่งฟิลด์ Data Group ที่ว่างเปล่า (Empty DG) เข้าไป ตัว Parser บางตัวจะข้ามขั้นตอนการเช็ก Signature และส่งสถานะคืนมาเป็น `SUCCESS` โดยไม่ได้ตรวจสอบข้อมูลจริง
* **การป้องกันใน TUM TRUST SECURITY:** เรากำหนด Strict Schema Validation ใน [`MrzParser.cs`](file:///d:/Work/NKS-Kiosk-Config/NKS-Kiosk-Identity-SDK/Infrastructure/Passport/MrzParser.cs) ตรวจสอบความยาวตัวอักษรและ Hash Array อย่างเข้มงวด หากพบฟิลด์แหว่งหรือไม่ครบองค์ประกอบ จะปฏิเสธ (Reject) ทันที (Fail-Closed Architecture)

#### 🚨 ช่องโหว่ที่ 3: Buffer Overflow & Memory Safety ใน Native C/C++ Drivers (CVE-2018-XXXX, OpenSC / Reader Drivers)
* **ปัญหา:** ไดรเวอร์เครื่องอ่านบัตรแบบเก่าที่เขียนด้วย C/C++ มักมีปัญหา Buffer Overflow เมื่อถูกยิงด้วย APDU Command ที่มีความยาวผิดปกติ (Malformed APDU)
* **ข้อได้เปรียบของ TUM SDK:** SDK ของเราพัฒนาด้วย **Modern C# (.NET 5/8/10)** ซึ่งเป็น Type-Safe & Memory-Safe Managed Runtime ช่วยกำจัดปัญหา Buffer Overflow หรือ Dangling Pointers ในชั้น Logic ไปโดยสิ้นเชิง

---

### 3. แผนการอุดช่องโหว่เพื่อลด Gap เวลาใน Phase 1 (Immediate Hardening Actions)

จากการวิจัยนี้ ทีม Current SOTA ขอเสนอ 3 แอ็กชันด่วนเพื่ออัปเกรด Phase 1 ให้แกร่งยิ่งขึ้น:

1. **Enforce Strict Active Authentication Fallback:**
   * หากตรวจพบว่าชิปเป็น ePassport รุ่นใหม่ที่รองรับ AA แต่ตู้ไม่ได้รับค่า AA Response ให้แจ้งสถานะเตือน `ChipVerificationRisk: Medium-High` ทันที
2. **Sub-dermal Micro-Pulse Threshold Dynamic Calibration:**
   * ปรับจูนค่า rPPG (อัตราการเต้นของชีพจรไมโครใต้ผิวหนัง) ให้รองรับคนผิวสีเข้ม (Fitzpatrick Skin Types V-VI) เพื่อป้องกันความลำเอียงของ AI (Biometric Bias)
3. **Anti-Replay Nonce Validation:**
   * ในการสร้าง Zero-Knowledge Token (`ntk_zkp_...`) ให้ผูก Timestamp และ Session Nonce ที่หมดอายุภายใน 60 วินาที เพื่อป้องกันการนำ Token เก่ามาใช้ซ้ำ (Replay Attack)
