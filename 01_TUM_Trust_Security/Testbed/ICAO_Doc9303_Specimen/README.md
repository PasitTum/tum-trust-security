# TUM Trust Security — ICAO Doc 9303 Specimen Test Profiles & PKI Infrastructure

## Overview
Comprehensive Public Key Infrastructure (PKI) and byte-accurate Logical Data Structure (LDS 1.7) specimen ePassport profiles conforming to ICAO Doc 9303 Part 10 & 12, allowing complete eMRTD testing without using real citizen travel documents.

## PKI Hierarchy
1. **Country Signing CA (CSCA):**
   - Self-signed X.509 Root CA (`test_csca_cert.pem`, RSA-4096 / SHA-256).
   - Validity: 15 Years.
   - Purpose: Trust anchor for Country `UT` (Utopia Specimen Authority).
2. **Document Signer (DS):**
   - Issued & signed by CSCA (`test_ds_cert.pem`, RSA-2048 / SHA-256).
   - Validity: 3 Years.
   - KeyUsage: `DigitalSignature` (critical).
3. **Certificate Revocation List (CRL):**
   - Signed by CSCA (`test_csca_crl.crl`).
4. **ICAO PKD Master List Export:**
   - Pre-formatted LDIF file (`icao_pkd_master_list.ldif`) ready for import into ICAO Public Key Directory.

## Specimen Document Profile (`UTO_SPECIMEN_001`)
- **Holder:** ERIKSSON, ANNA MARIA (Nationality: UTO)
- **Document Number:** L898902C3 (Type P, TD3)
- **Generated Binary Files:**
  - `EF_COM.bin` (Tag `0x60`): LDS 1.7 version & Tag List `[0x61, 0x75, 0x6E, 0x6F]`.
  - `EF_DG1.bin` (Tag `0x61`): TD3 MRZ OCR lines with 100% verified check digits.
  - `EF_DG2.bin` (Tag `0x75`): ISO/IEC 19794-5 Biometric Face JPEG record.
  - `EF_DG14.bin` (Tag `0x6E`): Chip Authentication (id-CA-ECDH-SHA256, P-256 curve).
  - `EF_DG15.bin` (Tag `0x6F`): Active Authentication Public Key (RSA-2048).
  - `EF_SOD.bin` (Tag `0x77`): Document Security Object with SHA-256 digests of DG1, DG2, DG14, DG15 digitally signed with Document Signer RSA key.

## Scripts & Validation
```bash
# 1. Regenerate PKI Certificates & ICAO PKD LDIF
python pki/generate_icao_pki.py

# 2. Build Specimen LDS Data Groups and EF.SOD
python profiles/build_icao_specimen_profile.py

# 3. Cryptographically Verify Passive Authentication
python profiles/verify_specimen_passport.py
```
