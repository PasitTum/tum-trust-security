# 📘 TUM Trust Security: Enterprise Developer & Integration Guide
## The Sovereign Trust & Proof-of-Humanity Engine (Powered by TUM)

---

### 1. Overview & Architecture
**TUM Trust Security** is an omnichannel, offline-first sovereign identity engine designed to run inside self-service kiosks, mobile applications (iOS/Android), and enterprise edge gateways without real-time external cloud dependencies.

```
 [ Client Layer ]
   ├── Kiosk Hardware (PC/SC NFC Reader + HD Webcam)
   ├── Mobile App (iOS CoreNFC / Android NfcAdapter / Flutter)
   └── Enterprise Web Application (Browser / WebAssembly)
              │
              ▼
 [ Tum.Trust.Security SDK Core ]
   ├── MrzParser: ICAO 9303 Modulo-10 Checksum (7-3-1 Weight)
   ├── SovereignTrustAnchorService: CSCA MasterList + Active Authentication
   ├── EdgeFaceLivenessDetector: ISO 30107 Level 2 Neuromorphic PAD (<10ms)
   ├── EdgeBiometricFaceMatcher: 512-D Cosine Face Matching
   ├── IcaoPkdSyncService: MasterList Automated Delta Updates
   └── SovereignKeyMesh: Air-Gapped Zero-Trust Peer Gossip Protocol
              │
              ▼
 [ Cryptographic Output ]
   └── Zero-Knowledge Proof Assertion (ntk_zkp_...)
```

---

### 2. Installation & Quickstart

#### Via NuGet Package:
```shell
dotnet add package Tum.Trust.Security --version 1.2.0
```

#### In Dependency Injection (`Program.cs` / `Startup.cs`):
```csharp
using Tum.Trust.Security.Application;

var builder = WebApplication.CreateBuilder(args);

// Registers IBorderlessTrustProvider, IFaceLivenessDetector, IHardwareReaderManager, and IIcaoPkdSyncService
builder.Services.AddIdentityVerification(options =>
{
    // In-memory customization if needed
});
```

---

### 3. Core Recipes

#### Recipe 1: 3-Line ePassport Verification
```csharp
var provider = app.Services.GetRequiredService<IBorderlessTrustProvider>();

var result = await provider.VerifyPassportAsync(new PassportVerificationRequest
{
    Mrz = MrzParser.ParseTd3(line1, line2),
    Dg1Bytes = rawDg1,
    Dg2FacePhotoBytes = rawDg2,
    SodBytes = rawSod,
    ActiveAuthenticationSignature = rawAaSig
}, liveCameraFrame);

if (result.IsSuccess)
{
    Console.WriteLine($"Verified Citizen of {result.IssuingCountry}");
    Console.WriteLine($"ZKP Token: {result.ZeroKnowledgeTrustToken}");
}
```

#### Recipe 2: Event-Driven Kiosk Card Taps
```csharp
var hardwareManager = app.Services.GetRequiredService<IHardwareReaderManager>();

hardwareManager.OnCardInserted += (sender, args) =>
{
    Console.WriteLine($"Document detected on reader: {args.ReaderName}");
    // Automatically trigger verification workflow
};
```

#### Recipe 3: Mobile NFC Scan with TUM SmartGuide AR
```csharp
var mobileBridge = app.Services.GetRequiredService<IMobileNfcBridge>();

mobileBridge.OnRfResonanceChanged += (sender, dbm) =>
{
    // Provide haptic feedback when dbm > -40.0 for optimal chip alignment
};

var scanResult = await mobileBridge.StartPassportScanSessionAsync(docNumber, dob, expiry);
```

#### Recipe 4: Air-Gapped Kiosk Key Sync (TUM Sovereign KeyMesh)
```csharp
var pkdService = app.Services.GetRequiredService<IIcaoPkdSyncService>();

// Ingest peer gossip payload from authenticated technician tablet
var syncResult = await pkdService.IngestSovereignKeyMeshPayloadAsync(technicianGossipToken);
```

---

### 4. Inventive Features (Pain Point Solutions)

| Feature | Solved Industry Pain Point |
| :--- | :--- |
| **TUM SmartGuide AR** | **40% Mobile NFC Failures:** Solves user confusion in locating the passport chip antenna via real-time RF resonance haptics. |
| **TUM Sovereign KeyMesh** | **Air-Gapped Key Drift:** Solves outdated Country Signing Keys in remote kiosks via zero-trust peer gossip payloads. |
| **Edge Neuromorphic PAD** | **GenAI Deepfakes & Replays:** Neutralizes high-frequency 4K screen replays and 3D silicone masks in <10ms without cloud latency. |

---

© 2026 TUM Trust Security (Powered by TUM). All rights reserved.
