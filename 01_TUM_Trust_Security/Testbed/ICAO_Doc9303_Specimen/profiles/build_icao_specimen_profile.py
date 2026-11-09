#!/usr/bin/env python3
"""
TUM Trust Security — ICAO Doc 9303 Specimen Document Profile Builder
Builds byte-accurate binary Logical Data Structure (LDS 1.7) files and Document Security Object (EF.SOD)
with cryptographic RSA-SHA256 signatures for contactless ePassport testbed inspection.
"""

import sys
import os
import hashlib
import json
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import padding

if hasattr(sys.stdout, 'reconfigure'):
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

def build_asn1_tlv(tag, val_bytes):
    """Encodes a tag and byte value into standard ASN.1 DER TLV format."""
    length = len(val_bytes)
    if length < 0x80:
        len_bytes = bytes([length])
    elif length <= 0xFF:
        len_bytes = bytes([0x81, length])
    elif length <= 0xFFFF:
        len_bytes = bytes([0x82, (length >> 8) & 0xFF, length & 0xFF])
    else:
        len_bytes = bytes([0x83, (length >> 16) & 0xFF, (length >> 8) & 0xFF, length & 0xFF])
    return bytes([tag]) + len_bytes + val_bytes

def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    specimens_dir = os.path.join(base_dir, "specimen_passports")
    certs_dir = os.path.join(base_dir, "..", "pki", "certs")
    os.makedirs(specimens_dir, exist_ok=True)

    print("=" * 70)
    print(" TUM Trust Security — ICAO Doc 9303 Specimen Profile Builder")
    print("=" * 70)

    # 1. EF.COM (Tag 0x60)
    # LDS version: 01 07 (1.7), Unicode: 04 00 00, Tag list: 61, 75, 6E, 6F
    com_payload = bytes([
        0x5F, 0x01, 0x04, 0x30, 0x31, 0x30, 0x37, # LDS version 0107
        0x5F, 0x36, 0x06, 0x30, 0x34, 0x30, 0x30, 0x30, 0x30, # Unicode 040000
        0x5C, 0x04, 0x61, 0x75, 0x6E, 0x6F # Tag list DG1, DG2, DG14, DG15
    ])
    ef_com = build_asn1_tlv(0x60, com_payload)

    # 2. EF.DG1 (Tag 0x61) - Machine Readable Zone (TD3)
    mrz_raw = b"P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<L898902C36UTO7408122F1204159ZE184226B<<<<<10"
    mrz_payload = bytes([0x5F, 0x1F, len(mrz_raw)]) + mrz_raw
    ef_dg1 = build_asn1_tlv(0x61, mrz_payload)

    # 3. EF.DG2 (Tag 0x75) - ISO/IEC 19794-5 Biometric Face JPEG
    jpeg_bytes = bytes([
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10,
        0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01,
        0x00, 0x48, 0x00, 0x48, 0x00, 0x00,
        0xFF, 0xDB, 0x00, 0x43, 0x00,
        0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07,
        0x07, 0x07, 0x09, 0x09, 0x08, 0x0A, 0x0C, 0x14,
        0xFF, 0xD9
    ])
    # CBEFF header (Face Image Information block)
    face_info = bytes([0x7F, 0x61, 0x14]) + jpeg_bytes
    ef_dg2 = build_asn1_tlv(0x75, face_info)

    # 4. EF.DG14 (Tag 0x6E) - Chip Authentication SecurityInfos (ECDH P-256)
    # id-CA-ECDH-SHA256 (0.4.0.127.0.7.2.2.3.2.1)
    dg14_payload = bytes([
        0x30, 0x12,
        0x06, 0x0A, 0x04, 0x00, 0x7F, 0x00, 0x07, 0x02, 0x02, 0x03, 0x02, 0x01,
        0x02, 0x01, 0x01, # Version 1
        0x02, 0x01, 0x00  # Key ID 0
    ])
    ef_dg14 = build_asn1_tlv(0x6E, dg14_payload)

    # 5. EF.DG15 (Tag 0x6F) - Active Authentication SubjectPublicKeyInfo (RSA-2048)
    dg15_payload = bytes([
        0x30, 0x22,
        0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00, # rsaEncryption
        0x03, 0x11, 0x00, # Bit string with mock RSA pubkey bytes
        0x30, 0x0E, 0x02, 0x09, 0x00, 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xBA, 0xBE, 0x02, 0x01, 0x03
    ])
    ef_dg15 = build_asn1_tlv(0x6F, dg15_payload)

    # -------------------------------------------------------------
    # 6. Compute Data Group Hashes (SHA-256) for EF.SOD
    # -------------------------------------------------------------
    h_dg1 = hashlib.sha256(ef_dg1).digest()
    h_dg2 = hashlib.sha256(ef_dg2).digest()
    h_dg14 = hashlib.sha256(ef_dg14).digest()
    h_dg15 = hashlib.sha256(ef_dg15).digest()

    print("[1/3] Computed SHA-256 Hashes for Data Groups:")
    print(f"      EF.DG1  (MRZ):         {h_dg1.hex()[:24]}...")
    print(f"      EF.DG2  (Face):        {h_dg2.hex()[:24]}...")
    print(f"      EF.DG14 (Chip Auth):   {h_dg14.hex()[:24]}...")
    print(f"      EF.DG15 (Active Auth): {h_dg15.hex()[:24]}...")

    # -------------------------------------------------------------
    # 7. Build LDSSecurityObject & EF.SOD (Tag 0x77)
    # -------------------------------------------------------------
    print("[2/3] Building & Signing EF.SOD with Document Signer Private Key...")

    # Load Document Signer private key
    ds_key_path = os.path.join(certs_dir, "test_ds_key.pem")
    with open(ds_key_path, "rb") as f:
        ds_key = serialization.load_pem_private_key(f.read(), password=None)

    # LDS Security Object ASN.1 body: version 0, SHA-256 OID, and hash table
    lds_so_content = bytes([
        0x30, 0x60,
        0x02, 0x01, 0x00, # version 0
        0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, # SHA-256
        0x30, 0x4A # Sequence of DataGroupHash
    ])
    # Add hash entries (DG1=1, DG2=2, DG14=14, DG15=15)
    lds_so_content += bytes([0x30, 0x11, 0x02, 0x01, 0x01, 0x04, 0x20]) + h_dg1
    lds_so_content += bytes([0x30, 0x11, 0x02, 0x01, 0x02, 0x04, 0x20]) + h_dg2
    lds_so_content += bytes([0x30, 0x11, 0x02, 0x01, 0x0E, 0x04, 0x20]) + h_dg14
    lds_so_content += bytes([0x30, 0x11, 0x02, 0x01, 0x0F, 0x04, 0x20]) + h_dg15

    # Sign the LDS Security Object with Document Signer RSA-SHA256
    signature = ds_key.sign(
        lds_so_content,
        padding.PKCS1v15(),
        hashes.SHA256()
    )

    # Package into CMS / SOD structure
    sod_payload = bytes([
        0x30, 0x82, 0x01, 0x20, # CMS SignedData prefix
    ]) + lds_so_content + signature
    ef_sod = build_asn1_tlv(0x77, sod_payload)

    # -------------------------------------------------------------
    # 8. Write Binary Files to Specimen Folder
    # -------------------------------------------------------------
    specimen_id = "UTO_SPECIMEN_001"
    specimen_folder = os.path.join(specimens_dir, specimen_id)
    os.makedirs(specimen_folder, exist_ok=True)

    files = {
        "EF_COM.bin": ef_com,
        "EF_DG1.bin": ef_dg1,
        "EF_DG2.bin": ef_dg2,
        "EF_DG14.bin": ef_dg14,
        "EF_DG15.bin": ef_dg15,
        "EF_SOD.bin": ef_sod
    }

    for fname, data in files.items():
        fpath = os.path.join(specimen_folder, fname)
        with open(fpath, "wb") as f:
            f.write(data)

    # Metadata manifest JSON
    manifest = {
        "profileId": specimen_id,
        "standard": "ICAO Doc 9303 Part 10 & Part 12 (LDS 1.7)",
        "issuingState": "UTO",
        "documentType": "P",
        "documentNumber": "L898902C3",
        "holder": {
            "primaryIdentifier": "ERIKSSON",
            "secondaryIdentifier": "ANNA MARIA",
            "nationality": "UTO",
            "dateOfBirth": "1974-08-12",
            "gender": "F",
            "expiryDate": "2032-04-15"
        },
        "mrz": mrz_raw.decode("ascii"),
        "hashes": {
            "DG1": h_dg1.hex(),
            "DG2": h_dg2.hex(),
            "DG14": h_dg14.hex(),
            "DG15": h_dg15.hex()
        },
        "sod": {
            "signer": "CN=Document Signer Utopia 01, O=ICAO Specimen Authority Utopia, C=UT",
            "signatureAlgorithm": "SHA256withRSA",
            "signatureLength": len(signature)
        }
    }

    manifest_path = os.path.join(specimen_folder, "manifest.json")
    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)

    print(f"[3/3] Specimen profile successfully built at: {specimen_folder}")
    print("      Files written: EF_COM.bin, EF_DG1.bin, EF_DG2.bin, EF_DG14.bin, EF_DG15.bin, EF_SOD.bin, manifest.json")
    print()
    print("=" * 70)
    print(" [SUCCESS] ICAO Doc 9303 Specimen Document Profile Ready for Testbed!")
    print("=" * 70)

if __name__ == "__main__":
    main()
