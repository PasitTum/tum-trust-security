#!/usr/bin/env python3
"""
TUM Trust Security — Master Real-World Testbed Harness Runner
Executes the full suite of hardware-free real-world testbed tools:
1. HIL JavaCard APDU Emulator (Thai ID + ICAO ePassport)
2. Virtual Camera Loopback & ISO/IEC 30107 Biometric PAD Benchmarks
3. ICAO Doc 9303 PKI & Passive Authentication Cryptographic Verification
"""

import sys
import subprocess
import os

if hasattr(sys.stdout, 'reconfigure'):
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))

    suites = [
        ("1. HIL JavaCard APDU Emulator", os.path.join(base_dir, "HIL_JavaCard_APDU", "scripts", "hil_apdu_test_runner.py")),
        ("2. Biometric PAD Benchmarks", os.path.join(base_dir, "VirtualCameraLoopback", "runner", "test_liveness_benchmarks.py")),
        ("3. ICAO Doc 9303 Cryptographic PA", os.path.join(base_dir, "ICAO_Doc9303_Specimen", "profiles", "verify_specimen_passport.py"))
    ]

    print("=" * 80)
    print(" TUM Trust Security — Master Real-World Testbed Harness Runner")
    print(" Production Acceptance Validation Suite")
    print("=" * 80)

    results = []

    for name, script_path in suites:
        print(f"\n>>> Running Suite: {name}...")
        proc = subprocess.run([sys.executable, script_path], capture_output=True, text=True, encoding="utf-8")
        print(proc.stdout)
        if proc.stderr:
            print(proc.stderr, file=sys.stderr)

        passed = (proc.returncode == 0)
        results.append((name, passed))

    print("\n" + "=" * 80)
    print(" MASTER TESTBED EXECUTION SUMMARY")
    print("=" * 80)
    all_passed = True
    for name, passed in results:
        status_str = "[PASS]" if passed else "[FAIL]"
        print(f"  {status_str:<8} {name}")
        if not passed:
            all_passed = False

    print("=" * 80)
    if all_passed:
        print(" [ALL SUITES PASSED] Kiosk Hardware & Real-World Testbed 100% Validated!")
    else:
        print(" [FAILED] One or more test suites failed.")
    print("=" * 80)

    return all_passed

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)
