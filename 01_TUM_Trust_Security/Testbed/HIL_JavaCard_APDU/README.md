# TUM Trust Security — HIL JavaCard APDU Emulator (NXP JCOP)

## Overview
Hardware-In-The-Loop (HIL) smart card testbed for testing physical Kiosk card readers without requiring real citizen national ID cards or real passports.

## Architecture
- **Target Microcontrollers:** NXP JCOP3 (J3H081), JCOP4 (J3R180), J2A040 series smart card chips.
- **Standards:** Java Card 3.0.4+, GlobalPlatform 2.2+, ISO/IEC 7816-4, ISO/IEC 14443 Type A/B, ICAO Doc 9303 Part 10 & 11.
- **Dual Personality Support:**
  1. **Thai National ID Applet (MoI):** AID `A0 00 00 00 54 48 00 01` (Citizen ID, demographics, photo segment).
  2. **ICAO Doc 9303 eMRTD Passport Applet:** AID `A0 00 00 02 47 10 01` (TD3 MRZ, DG1, Active Auth Challenge).

## Flashing Instructions
```bash
# Connect USB smart card reader with blank JCOP card inserted
python scripts/flash_jcop.py --reader "ACS ACR1252U Contactless NFC Reader 0"
```

## Running HIL APDU Test Suite
```bash
python scripts/hil_apdu_test_runner.py
```
