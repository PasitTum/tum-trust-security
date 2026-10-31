# เอกสารการนำเสนอ Working Group: สรุปผลงาน Sprint 1 และนำเสนอแผนงาน Sprint 2
## (TUM Group Bi-Weekly Working Group Presentation)
**วันที่:** 1 พฤศจิกายน 2026  
**ประธานการประชุม:** Chief Executive Officer (CEO)  
**ช่องทางการประชุม:** Discord Live Voice & Stage Channel (`#doc`)  
**ไฟล์สไลด์ Interactive HTML:** [docs/presentations/tum_working_group_sprint_1_and_2.html](file:///D:/TUM/tum-trust-security/docs/presentations/tum_working_group_sprint_1_and_2.html)  

---

## สไลด์ที่ 1: หน้าปกและวัตถุประสงค์ (Cover & Purpose)
* **หัวข้อ:** Sprint 1 Delivery Review & Sprint 2 Strategic Pitch
* **ผู้สนับสนุนและผู้เข้าร่วม:**
  * คุณ Pasit (Chairman of the Board)
  * ท่าน Chief Executive Officer (CEO)
  * AI Scrum Master & Engineering Squads
* **วัตถุประสงค์:** สรุปการส่งมอบรากฐานความปลอดภัยและกลไกการทำงานของ Sprint 1 พร้อมเสนอขออนุมัติขอบเขตการพัฒนาสำหรับ Sprint 2

---

## สไลด์ที่ 2: สรุปผลงาน Sprint 1 (Executive Scorecard)
* **จำนวนงานที่ปิดสำเร็จ:** 6 / 6 Tasks (**100% Done**)
* **ผลการรันชุดทดสอบ (Unit Tests):** ผ่านฉลุย **125 / 125 ข้อ** (Zero Failures, 100% Success Rate)
* **เวลาที่ใช้ในการทดสอบ:** เพียง **137 มิลลิวินาที**
* **ข้อผิดพลาดและหนี้ทางเทคนิค:** **0 Errors, 0 Warnings** บน Target Runtime .NET 8.0
* **ไฮไลต์สำคัญ:** ปรับปรุงความมั่นคงปลอดภัย Kiosk Secret Hardening ย้ายรหัสลับออกจากฐานข้อมูลสู่วงจร OS Environment Variables ได้สำเร็จ

---

## สไลด์ที่ 3: สรุปผลงานแยกตามบริษัทย่อย — TUM Trust Security
1. **[SCRUM-9] OS Environment Secret Management:**
   * ดึงค่า `ClientId`, `ClientSecret`, `ApiKey` และ `EncryptionKey` จาก OS Environment Variables / Windows DPAPI ป้องกันปัญหา Database Leak
2. **[SCRUM-10] Biometrics Facial Liveness & Anti-Spoofing:**
   * รองรับการตรวจสอบการปลอมแปลงใบหน้าและ Deepfake ตามมาตรฐาน ISO/IEC 30107 Level 2
   * ความล่าช้าต่ำกว่า 500ms บน Edge Hardware ตู้ Kiosk

---

## สไลด์ที่ 4: สรุปผลงานแยกตามบริษัทย่อย — TUMSocs & TUMQuantum
1. **[SCRUM-11] TUMSocs: Lock-Free RingBuffer Ingestion Engine:**
   * รองรับปริมาณข้อมูลสูงถึง 1,000,000 Events/วินาที (p99 Latency < 0.15ms)
   * โครงสร้าง SHA-256 Merkle Chain of Custody ปฏิบัติตาม พ.ร.บ.คอมพิวเตอร์ ม.26 และ PDPA แบบ 90-Day Immutable WORM
2. **[SCRUM-12] TUMQuantum: NIST PQC Primitives:**
   * โครงสร้างรหัสลับต้านทานควอนตัมมาตรฐานสากล NIST FIPS 203 (ML-KEM) และ FIPS 204 (ML-DSA) รองรับการเปลี่ยนผ่านสู่ยุค Post-Quantum

---

## สไลด์ที่ 5: สรุปผลงานทีมส่วนกลาง — Central Director Team
1. **[SCRUM-13] TUM Unified Cyber Defense Console (Web App):**
   * ออกแบบและพัฒนาแดชบอร์ด WebGL Canvas 60 FPS แสดงผลกราฟิกเครือข่ายภัยคุกคามแบบ Real-time ระดับ User-Level สำหรับผู้บริหาร CISO
2. **[SCRUM-14] Discord & Jira Live Automation:**
   * เชื่อมต่อสาย Webhook ระหว่าง Jira Cloud เข้าสู่ Discord `#doc` ทำให้การแจ้งเตือนงานและการปิด Sprint ส่งตรงถึงทีมงานทันที
3. **การแต่งตั้ง Scrum Master:**
   * ประกาศข้อกำหนดตำแหน่งงาน [JOB_SPEC_SCRUM_MASTER.md](file:///D:/TUM/tum-trust-security/docs/people/JOB_SPEC_SCRUM_MASTER.md) ขึ้นระบบ People & Culture เพื่อดูแล Cadence ให้ท่าน CEO

---

## สไลด์ที่ 6: นำเสนอแผนงาน Sprint 2 (Sprint 2 Strategic Pitch)
**ธีมของ Sprint 2:** *"Hardware Kiosk Integration, ePassport & Sovereign Borderless Trust"*

| ลำดับ Ticket | รายการงานที่นำเสนอเข้า Sprint 2 | หน่วยงานที่รับผิดชอบ | ความสำคัญ (Priority) |
| :---: | :--- | :---: | :---: |
| **SCRUM-15** | `[Trust] PC/SC Smart Card Driver Hardware Abstraction Layer (HAL) for Kiosk Readers` | TUM Trust Security | **Highest** 🔥 |
| **SCRUM-16** | `[Trust] ICAO 9303 ePassport MRZ Parser & NFC Data Group Inspection` | TUM Trust Security | **High** |
| **SCRUM-17** | `[TUMSocs] Live eBPF Kernel Network Packet Sniffing & Telemetry Hook` | TUMSocs | **High** |
| **SCRUM-18** | `[Quantum] Hybrid Permissive Migration Adapter for Classical PKI Systems` | TUMQuantum | **Medium** |
| **SCRUM-19** | `[Director] Multi-Tenant Executive Alerting & One-Click Audit Report (PDF/Excel)` | Central Director | **Medium** |

---

## สไลด์ที่ 7: มติและการตัดสินใจที่เสนอต่อที่ประชุม (Decisions & Approvals)
1. ขอให้ท่าน CEO และคณะทำงาน **มีมติรับรองผลการส่งมอบงาน Sprint 1** (ปิดสถานะ Done อย่างสมบูรณ์)
2. ขอให้ท่าน CEO **อนุมัติขอบเขตการพัฒนา Sprint 2** เพื่อให้ทีมวิศวกรดึง Ticket เข้าสู่กระบวนการ Sprint Planning ทันที
3. มอบหมายให้แผนก People & Culture ดำเนินการคัดเลือกและนัดสัมภาษณ์ผู้สมัครตำแหน่ง Scrum Master ในลำดับถัดไป

---

## สไลด์ที่ 8: เปิดฟลอร์ถาม-ตอบ (Q&A & Sprint 2 Kick-off)
* เปิดรับฟังข้อเสนอแนะและข้อสั่งการจากท่าน CEO เพื่อเริ่มฉาก Sprint 2 อย่างเป็นทางการ
