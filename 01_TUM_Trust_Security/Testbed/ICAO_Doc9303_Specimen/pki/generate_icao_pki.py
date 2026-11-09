#!/usr/bin/env python3
"""
TUM Trust Security — ICAO Doc 9303 Test PKI Certificate Generator
Generates full X.509 PKI hierarchy for ICAO Doc 9303 ePassport validation:
1. Country Signing CA (CSCA) — Self-signed root CA
2. Document Signer (DS) — Signed by CSCA
3. Certificate Revocation List (CRL)
4. ICAO PKD LDIF Master List Export
"""

import sys
import os
import datetime
from cryptography import x509
from cryptography.x509.oid import NameOID, ExtensionOID
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import rsa

if hasattr(sys.stdout, 'reconfigure'):
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

def generate_pki():
    pki_dir = os.path.dirname(os.path.abspath(__file__))
    certs_dir = os.path.join(pki_dir, "certs")
    os.makedirs(certs_dir, exist_ok=True)

    print("=" * 70)
    print(" TUM Trust Security — ICAO Doc 9303 Test PKI Generator")
    print("=" * 70)

    # -------------------------------------------------------------
    # 1. Country Signing CA (CSCA)
    # -------------------------------------------------------------
    print("[1/4] Generating Country Signing CA (CSCA) [RSA-4096]...")
    csca_key = rsa.generate_private_key(public_exponent=65537, key_size=4096)
    
    csca_name = x509.Name([
        x509.NameAttribute(NameOID.COUNTRY_NAME, "UT"),
        x509.NameAttribute(NameOID.ORGANIZATION_NAME, "ICAO Specimen Authority Utopia"),
        x509.NameAttribute(NameOID.ORGANIZATIONAL_UNIT_NAME, "ICAO PKD Testbed"),
        x509.NameAttribute(NameOID.COMMON_NAME, "Country Signing CA Utopia")
    ])

    now = datetime.datetime.now(datetime.timezone.utc)
    csca_cert = (
        x509.CertificateBuilder()
        .subject_name(csca_name)
        .issuer_name(csca_name) # Self-signed
        .public_key(csca_key.public_key())
        .serial_number(x509.random_serial_number())
        .not_valid_before(now - datetime.timedelta(days=1))
        .not_valid_after(now + datetime.timedelta(days=365 * 15)) # 15 years
        .add_extension(x509.BasicConstraints(ca=True, path_length=1), critical=True)
        .add_extension(
            x509.KeyUsage(
                digital_signature=False,
                content_commitment=False,
                key_encipherment=False,
                data_encipherment=False,
                key_agreement=False,
                key_cert_sign=True,
                crl_sign=True,
                encipher_only=False,
                decipher_only=False
            ),
            critical=True
        )
        .add_extension(x509.SubjectKeyIdentifier.from_public_key(csca_key.public_key()), critical=False)
        .sign(csca_key, hashes.SHA256())
    )

    csca_key_path = os.path.join(certs_dir, "test_csca_key.pem")
    csca_cert_path = os.path.join(certs_dir, "test_csca_cert.pem")
    csca_cer_path = os.path.join(certs_dir, "test_csca_cert.cer")

    with open(csca_key_path, "wb") as f:
        f.write(csca_key.private_bytes(
            encoding=serialization.Encoding.PEM,
            format=serialization.PrivateFormat.PKCS8,
            encryption_algorithm=serialization.NoEncryption()
        ))

    with open(csca_cert_path, "wb") as f:
        f.write(csca_cert.public_bytes(serialization.Encoding.PEM))

    with open(csca_cer_path, "wb") as f:
        f.write(csca_cert.public_bytes(serialization.Encoding.DER))

    print(f"      CSCA Cert: {csca_cert_path}")
    print(f"      Validity:  15 Years (Expires: {csca_cert.not_valid_after_utc.strftime('%Y-%m-%d')})")

    # -------------------------------------------------------------
    # 2. Document Signer (DS)
    # -------------------------------------------------------------
    print("[2/4] Generating Document Signer (DS) Certificate [RSA-2048]...")
    ds_key = rsa.generate_private_key(public_exponent=65537, key_size=2048)

    ds_name = x509.Name([
        x509.NameAttribute(NameOID.COUNTRY_NAME, "UT"),
        x509.NameAttribute(NameOID.ORGANIZATION_NAME, "ICAO Specimen Authority Utopia"),
        x509.NameAttribute(NameOID.ORGANIZATIONAL_UNIT_NAME, "ICAO PKD Testbed"),
        x509.NameAttribute(NameOID.COMMON_NAME, "Document Signer Utopia 01")
    ])

    ds_cert = (
        x509.CertificateBuilder()
        .subject_name(ds_name)
        .issuer_name(csca_name) # Signed by CSCA
        .public_key(ds_key.public_key())
        .serial_number(x509.random_serial_number())
        .not_valid_before(now - datetime.timedelta(days=1))
        .not_valid_after(now + datetime.timedelta(days=365 * 3)) # 3 years
        .add_extension(x509.BasicConstraints(ca=False, path_length=None), critical=True)
        .add_extension(
            x509.KeyUsage(
                digital_signature=True,
                content_commitment=False,
                key_encipherment=False,
                data_encipherment=False,
                key_agreement=False,
                key_cert_sign=False,
                crl_sign=False,
                encipher_only=False,
                decipher_only=False
            ),
            critical=True
        )
        .add_extension(x509.SubjectKeyIdentifier.from_public_key(ds_key.public_key()), critical=False)
        .add_extension(x509.AuthorityKeyIdentifier.from_issuer_public_key(csca_key.public_key()), critical=False)
        .sign(csca_key, hashes.SHA256())
    )

    ds_key_path = os.path.join(certs_dir, "test_ds_key.pem")
    ds_cert_path = os.path.join(certs_dir, "test_ds_cert.pem")
    ds_cer_path = os.path.join(certs_dir, "test_ds_cert.cer")

    with open(ds_key_path, "wb") as f:
        f.write(ds_key.private_bytes(
            encoding=serialization.Encoding.PEM,
            format=serialization.PrivateFormat.PKCS8,
            encryption_algorithm=serialization.NoEncryption()
        ))

    with open(ds_cert_path, "wb") as f:
        f.write(ds_cert.public_bytes(serialization.Encoding.PEM))

    with open(ds_cer_path, "wb") as f:
        f.write(ds_cert.public_bytes(serialization.Encoding.DER))

    print(f"      DS Cert:   {ds_cert_path}")
    print(f"      Validity:  3 Years (Expires: {ds_cert.not_valid_after_utc.strftime('%Y-%m-%d')})")

    # -------------------------------------------------------------
    # 3. Certificate Revocation List (CRL)
    # -------------------------------------------------------------
    print("[3/4] Generating Certificate Revocation List (CRL)...")
    crl = (
        x509.CertificateRevocationListBuilder()
        .issuer_name(csca_name)
        .last_update(now - datetime.timedelta(days=1))
        .next_update(now + datetime.timedelta(days=90))
        .sign(csca_key, hashes.SHA256())
    )
    crl_path = os.path.join(certs_dir, "test_csca_crl.crl")
    with open(crl_path, "wb") as f:
        f.write(crl.public_bytes(serialization.Encoding.DER))
    print(f"      CRL File:  {crl_path}")

    # -------------------------------------------------------------
    # 4. ICAO PKD LDIF Master List Export
    # -------------------------------------------------------------
    print("[4/4] Generating ICAO PKD LDIF Master List Import File...")
    ldif_path = os.path.join(certs_dir, "icao_pkd_master_list.ldif")
    ds_der_b64 = ds_cert.public_bytes(serialization.Encoding.DER)
    import base64
    b64_str = base64.b64encode(ds_der_b64).decode("ascii")

    ldif_content = f"""dn: cn=Country Signing CA Utopia,o=ICAO Specimen Authority,c=UTO
objectClass: top
objectClass: cscaMasterList
cn: Country Signing CA Utopia
c: UTO
userCertificate;binary:: {base64.b64encode(csca_cert.public_bytes(serialization.Encoding.DER)).decode('ascii')}

dn: cn=Document Signer Utopia 01,o=ICAO Specimen Authority,c=UTO
objectClass: top
objectClass: documentSigner
cn: Document Signer Utopia 01
c: UTO
userCertificate;binary:: {b64_str}
"""
    with open(ldif_path, "w", encoding="utf-8") as f:
        f.write(ldif_content)

    print(f"      LDIF File: {ldif_path}")

    print()
    print("=" * 70)
    print(" [SUCCESS] ICAO Doc 9303 PKI Certificates & PKD Export Generated!")
    print("=" * 70)

if __name__ == "__main__":
    generate_pki()
