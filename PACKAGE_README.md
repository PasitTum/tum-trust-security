# TUM.Identity.SDK

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/TUMsystem/TUM-enterprise-identity)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/TUMsystem/TUM-enterprise-identity/blob/main/LICENSE)
[![Target Frameworks](https://img.shields.io/badge/.NET-5.0%20%7C%208.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/)

**TUM Enterprise Omnichannel Sovereign Trust & Identity Verification SDK** is a high-performance, offline-first verification engine designed for Kiosks, Mobile, Web, and Enterprise Gateways.

---

## 🌟 Key Capabilities

* 🌐 **ICAO 9303 ePassport NFC Engine:** Supports TD1, TD2, TD3 MRZ checksum calculations (Modulo 10 with 7-3-1 weights) and cryptographic validation across 150+ nations.
* 🛡️ **Sovereign Trust Anchor:** Built-in Passive Authentication (PA) via Country Signing CA (CSCA) MasterList and Active Authentication (AA) challenge-response for chip anti-cloning.
* 🍏 **ISO/IEC 18013-5 Mobile Driving License (mDL):** Validates digital credentials and selective disclosure from Apple Wallet and Google Wallet.
* ⚡ **Neuromorphic Edge AI Biometrics:** ISO/IEC 30107 Level 2 Presentation Attack Detection (PAD) and sub-dermal pulse analysis. Neutralizes 4K screen replays, 3D silicone masks, and generative deepfakes in <10ms.
* 🔒 **Zero PII Exposure:** Emits cryptographically verifiable Zero-Knowledge Proof (ZKP) identity assertions (`ntk_zkp_...`) with zero raw PII storage.
* 🇹🇭 **Thai Sovereign Citizen Support:** Fully compatible with Thai National ID Card (DOPA Gateway) and ThaID digital ID.

---

## 🚀 Quickstart in 3 Lines of Code

```csharp
using TUM.Identity.Core.Interfaces;
using TUM.Identity.Core.Models.Passport;

// 1. Resolve or instantiate the Borderless Trust Provider
IBorderlessTrustProvider provider = serviceProvider.GetRequiredService<IBorderlessTrustProvider>();

// 2. Verify an ePassport
var result = await provider.VerifyPassportAsync(new PassportVerificationRequest
{
    MrzRawText = "P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<\nL898902C36UTO7408122F1204159ZE184226B<<<<<10",
    ValidatePassiveAuthentication = true,
    ValidateActiveAuthentication = true,
    ValidateFaceLiveness = true
});

// 3. Inspect Result & Sovereign Token
if (result.IsAuthentic && result.IsLivePerson)
{
    Console.WriteLine($"Verified Citizen: {result.Nationality}");
    Console.WriteLine($"Zero-Knowledge Token: {result.ZeroKnowledgeAssertionToken}");
}
```

---

## 📦 Compatibility & Zero Dependencies

* **Target Frameworks:** `.NET 5.0`, `.NET 8.0` (LTS), and `.NET 10.0`
* **Zero Third-Party Dependencies:** 100% self-contained in `TUM.Identity.SDK.dll`
* **Offline-Ready:** Operates seamlessly without real-time external cloud API dependencies.

---

© TUM Trust Security All rights reserved.
