# 01_TUM_Trust_Security — Sovereign Identity & Hardware Platform
## บริษัทที่ 1 ในเครือ TUM Group Holdings Co., Ltd.

> **คำขวัญ:** *"Sovereign Identity, Zero-Trust Hardware & Omnichannel Verification"*  
> **สถานะ:** Active Production Pillar 🟢  
> **ผู้รับผิดชอบหลัก:** Managing Director (Identity & Hardware Security) & Solutions Architect

---

## 1. ขอบเขตและหน้าที่ความรับผิดชอบ (Company Scope)

**TUM Trust Security Co., Ltd.** เป็นเสาหลักแรกของกลุ่มบริษัท TUM มีหน้าที่พัฒนาและส่งมอบแพลตฟอร์มการพิสูจน์และยืนยันตัวตนระดับอธิปไตย (Sovereign Identity & e-KYC):

1. **National Identity Verification:**
   - **ThaID (DOPA OIDC/PKCE):** เชื่อมต่อระบบยืนยันตัวตนแห่งชาติของกรมการปกครองด้วย OAuth 2.0 PKCE และ 20 Data Scopes
   - **DOPA Gateway:** ตรวจสอบสถานะบัตรประชาชนแบบ Real-time กับฐานข้อมูลทะเบียนราษฎร์
   - **Borderless Trust:** ระบบตรวจสอบอัตลักษณ์ข้ามพรมแดนสำหรับชาวต่างชาติ
2. **Hardware & Kiosk Drivers (PC/SC HAL):**
   - ไดรเวอร์เชื่อมต่อเครื่องอ่านสมาร์ตการ์ดสำหรับตู้คีออสและจุดบริการประชาชน
   - การอ่านข้อมูลบัตรประชาชนไทย (Smart Card APDU) แบบไม่ต้องใช้ไดรเวอร์ภายนอก
3. **ICAO 9303 ePassport NFC:**
   - อ่านและถอดรหัส Machine Readable Zone (MRZ) ของพาสปอร์ตกว่า 150 ประเทศ
   - ตรวจสอบลายมือชื่อดิจิทัลและเชื่อมโยงกับฐานข้อมูล ICAO PKD MasterList
4. **Biometrics & AI Behavioral Defense:**
   - ระบบ Face Liveness Detection ป้องกันการหลอกลวงด้วยภาพถ่ายหรือหน้ากาก (Presentation Attack Detection)
   - โมเดลวิเคราะห์พฤติกรรมมนุษย์ vs AI เพื่อตัดสิทธิ์บอตประสงค์ร้าย

---

## 2. โครงสร้างโฟลเดอร์ภายใน (Internal Architecture)

```
01_TUM_Trust_Security/
├── Application/                 # Application Services & Dependency Injection
│   ├── DependencyInjection.cs  # ServiceCollection extension methods
│   └── Services/               # IdentityVerificationService, DopaService, ThaIdService...
├── Core/                        # Enterprise Domain Models & Interfaces
│   ├── Entities/               # Domain records & invariants
│   ├── Interfaces/             # Service contracts (IThaIdProvider, IDopaProvider...)
│   └── Models/                 # Request/Response models, Options, Exceptions
├── Infrastructure/              # Concrete Drivers, Clients & Adapters
│   ├── Biometrics/             # Face matching & edge liveness detectors
│   ├── Hardware/               # PC/SC Smart card reader HAL
│   ├── Helpers/                # PII masking & string utilities
│   ├── Mobile/                 # Cross-platform NFC bridges (iOS/Android/Windows)
│   └── Providers/              # DOPA, ThaID, and Borderless HTTP client implementations
├── docs/                        # เอกสารข้อกำหนดทางเทคนิค
│   ├── developer-portal/       # OpenAPI specification & Developer Guide
│   ├── providers/              # API Specs (thaid.md, dopa.md, borderless.md)
│   └── *.md                    # SDK Specifications, Secret Management Roadmap
├── Tum.Trust.Security.csproj   # โปรเจกต์ SDK หลัก (.NET 5.0, .NET 8.0, .NET 10.0)
└── PACKAGE_README.md           # NuGet Package Documentation
```

---

## 3. ความเชื่อมโยงกับบริษัทย่อยอื่น (Synergy & Cross-Company Links)

- ➡️ **ส่งข้อมูลไปยัง `02_TUM_Socs`:** ส่ง Audit Logs ทุกการยืนยันตัวตนเข้าสู่ Ingestion RingBuffer ของ TUMSocs เพื่อลงบันทึกหลักฐานตาม ISO 27001 และ พ.ร.บ.คอมพิวเตอร์
- ⬅️ **รับการป้องกันจาก `03_TUM_Quantum`:** ใช้โมดูล Post-Quantum Cryptography จาก TUMQuantum เพื่อทำ Dual-Envelope Hybrid Signature ปกป้องข้อมูลอัตลักษณ์จากภัยคอมพิวเตอร์ควอนตัม


---

## 3. Commercial Turnkey Packaging & Biometric Pose Gate (`D:\TUM\setup-product\tum-trust-security`)

แพ็กเกจสมบูรณ์สำหรับการติดตั้งใช้งานจริงในตู้ Kiosk และ E-Gate:

### 3.1 Executable & Diagnostic Console
1. **`service-bin/Tum.Trust.KioskService.exe`**: Service สำหรับ Windows พัฒนาด้วย .NET 8 AOT Self-Contained ขนาด 67.1 MB ทำหน้าที่จัดการ Hardware Abstraction Layer (PC/SC, UVC Camera, Watchdog Auto-Recovery)
2. **`installer/`**: สคริปต์ติดตั้ง Windows Service (`install-service.ps1`) และ WiX Toolset MSI Specification
3. **`kiosk-diagnostics-console/TUM-Kiosk-Diagnostics.exe`**: แอปพลิเคชัน Electron Operator Console (238 MB) สำหรับเจ้าหน้าที่ภาคสนาม:
   - ตรวจสอบสถานะ Smartcard APDU และอ่านบัตรประชาชนไทย
   - ตรวจสอบ ePassport ICAO Doc 9303 (DG1, DG2, DG14, DG15, EF.SOD Passive Authentication)
   - **Biometric Pose Gate (ISO/IEC 19794-5 Section 7.2):** ฝังโมเดล Computer Vision Frontal Face Cascade (`pico.js` + `facefinder`) สำหรับตรวจจับตำแหน่งใบหน้าแบบเรียลไทม์ (10 FPS HUD Overlay) หากผู้ใช้หันหน้าหนี (Yaw > ±15° Profile view) หรือมีคนมากกว่า 1 คน ระบบจะทำการ REJECT ทันที (Score: 0.085) ป้องกันการ Bypass ทุกรูปแบบ

### 3.2 Physical HIL Testbeds
- **`hil-testbed/run_all_testbeds.py`**: ชุดทดสอบ Hardware-in-the-Loop จำลอง APDU Contact/Contactless, ICAO Passive Authentication และ NIST FRVT / CASIA-FAS Presentation Attack Detection ผ่าน 100%

### 3.3 คำสั่งรัน Smoke Test:
```powershell
powershell -ExecutionPolicy Bypass -File D:\TUM\setup-product\tum-trust-security\smoke-test.ps1
```
