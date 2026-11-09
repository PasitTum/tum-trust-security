# TUM Trust Security — Kiosk Field Diagnostics Desktop Console (Electron)

Multi-platform desktop diagnostic application (Windows, macOS, Linux) designed for field engineers and technicians servicing self-service kiosks.

## Key Capabilities
1. **Smartcard & Contactless NFC (PC/SC HAL):**
   - Live discovery and status for ACS ACR1252U, HID OMNIKEY, Identiv readers.
   - ATR inspection and protocol detection (ISO/IEC 7816 & 14443 Type A/B).
   - Interactive APDU console with preset macros (Select AID, Read Citizen ID, Read Name, Read DOB, Read Address).
   - 1-click Thai National ID card read with photo display and masked PII.
2. **ePassport Optical & NFC LDS Scanner:**
   - Regula 7024 and ARH Combo Smart integration.
   - Optical MRZ OCR parsing and 100% check digit validation.
   - Chip LDS 1.7 Data Group verification (EF.COM, EF.DG1, EF.DG2 face photo, EF.DG14 Chip Auth, EF.DG15 Active Auth).
   - Document Security Object (EF.SOD) Passive Authentication signature verification against CSCA/DS certificates.
3. **Biometric Camera & Presentation Attack Detection (PAD):**
   - High-definition camera stream with real-time facial mesh overlay.
   - ISO/IEC 30107 Level 2 PAD scoring meter with 0.85 threshold.
   - Attack classification: 2D Print Photo, Screen Replay, 3D Silicone Mask, Deepfake.
   - Micro-vascular pulse waveform (Remote-PPG) heart-rate pulse monitoring.
4. **USB Bus Auto-Recovery & Chaos Testing:**
   - Real-time tracking of supervised peripherals.
   - Fault injection tools ("Simulate Cable Yank", "Simulate Cable Re-attach").
   - Verified recovery SLA under 1.5 seconds.
5. **Commercial Acceptance Certification:**
   - 1-click system self-test.
   - Export official JSON and printable diagnostic certificate.

## Running in Development
```bash
cd 01_TUM_Trust_Security/KioskDiagnosticsApp
npm install
npm start
```

## Packaging for Commercial Distribution
```bash
# Windows (.exe installer and .msi)
npm run dist:win

# macOS (.dmg and .zip)
npm run dist:mac

# Linux (.AppImage and .deb)
npm run dist:linux
```
