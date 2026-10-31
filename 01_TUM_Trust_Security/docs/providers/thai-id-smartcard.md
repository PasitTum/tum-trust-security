# Thai National ID Smart Card HAL (PC/SC Kiosk Driver)

The **Thai National ID Smart Card Hardware Abstraction Layer (HAL)** provides production-grade, low-latency, and zero-trust hardware communication with physical Thai Citizen Identification Cards conforming to Ministry of Interior (MoI) and Department of Provincial Administration (DOPA) smart card specifications (ISO/IEC 7816-4).

---

## 1. Architecture Overview

```
+-------------------------------------------------------------+
|                     Kiosk Application                       |
+-------------------------------------------------------------+
                              |
                              v
+-------------------------------------------------------------+
|                 IThaiIdSmartCardReader                      |
|             (ThaiIdSmartCardReader.cs)                      |
|  - APDU Command Pipeline (MoI Applet AID: A000000054480001) |
|  - Modulo 11 Citizen ID Checksum Validation                 |
|  - TIS-620 / UTF-8 Bilingual String Parser                  |
|  - Event-driven Card Insertion / Removal Detection          |
+-------------------------------------------------------------+
                              |
                              v
+-------------------------------------------------------------+
|                  IHardwareReaderManager                     |
|             (PcScSmartcardReaderAdapter.cs)                 |
+-------------------------------------------------------------+
               |                               |
               v                               v
+-----------------------------+ +-----------------------------+
|      NativePcScInterop      | |  Mock / Simulation Mode     |
| (winscard.dll / libpcsclite)| |    (CI/CD & Virtual Kiosk)  |
+-----------------------------+ +-----------------------------+
```

---

## 2. APDU Command Specifications

The Thai National ID card applet conforms to ISO/IEC 7816-4 APDU standards using MoI proprietary command sets (`CLA = 0x80`, `INS = 0xB0`).

| Step | Operation | APDU Command (Hex) | Expected Status / Response |
| :--- | :--- | :--- | :--- |
| **1** | **SELECT MoI Applet** | `00 A4 04 00 08 A0 00 00 00 54 48 00 01` | `90 00` (Success) |
| **2** | **READ Citizen ID (PID)** | `80 B0 00 04 02 00 0D` | 13-byte ASCII PID + `90 00` |
| **3** | **READ Personal Info** | `80 B0 00 11 02 00 64` | Bilingual Names (`#`-delimited) + `90 00` |
| **4** | **READ Date of Birth** | `80 B0 00 D1 02 00 08` | 8-byte ASCII Buddhist Date (YYYYMMDD) + `90 00` |
| **5** | **READ Gender** | `80 B0 00 E1 02 00 01` | 1-byte ASCII (`1`=Male, `2`=Female) + `90 00` |
| **6** | **READ Address** | `80 B0 15 79 02 00 64` | Residential address string + `90 00` |
| **7** | **READ Issue Date** | `80 B0 01 67 02 00 08` | 8-byte ASCII Date (YYYYMMDD) + `90 00` |
| **8** | **READ Expiry Date** | `80 B0 01 6F 02 00 08` | 8-byte ASCII Date (YYYYMMDD) + `90 00` |
| **9** | **READ Issuer Office** | `80 B0 00 F6 02 00 64` | Issuer Office string + `90 00` |
| **10** | **READ Photo** | `80 B0 01 7B 02 00 FF` | JPEG facial image bytes (`FF D8 ...`) + `90 00` |

---

## 3. Usage Examples

### 3.1 Dependency Injection Setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tum.Trust.Security.Application;

var services = new ServiceCollection();
services.AddLogging();
services.AddTumTrustSecurity();

var serviceProvider = services.BuildServiceProvider();
var cardReader = serviceProvider.GetRequiredService<IThaiIdSmartCardReader>();
```

### 3.2 Reading an Inserted Card

```csharp
using System;
using Tum.Trust.Security.Core.Interfaces;

// Reader name obtained from IHardwareReaderManager.EnumerateReadersAsync()
string readerName = "ACS ACR1252U Contactless NFC Reader 0";

if (await cardReader.IsThaiIdCardPresentAsync(readerName))
{
    var cardData = await cardReader.ReadThaiIdCardAsync(readerName, includePhoto: true);
    
    Console.WriteLine($"Citizen ID: {cardData.MaskedCitizenId}");
    Console.WriteLine($"Name (TH): {cardData.FullNameTh}");
    Console.WriteLine($"Name (EN): {cardData.FullNameEn}");
    Console.WriteLine($"DOB: {cardData.DateOfBirth}");
    Console.WriteLine($"Address: {cardData.Address}");
    Console.WriteLine($"Photo Bytes: {cardData.PhotoBytes?.Length} bytes");
}
```

### 3.3 Event-Driven Card Wait & Read (Kiosk Flow)

```csharp
using System;
using System.Threading;

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

try
{
    Console.WriteLine("Please insert your Thai Citizen ID Card into the kiosk reader...");
    var cardData = await cardReader.WaitForCardAndReadAsync(
        readerName: "ACS ACR1252U Contactless NFC Reader 0",
        timeout: TimeSpan.FromSeconds(30),
        includePhoto: true,
        cancellationToken: cts.Token);

    Console.WriteLine($"Card detected and verified: {cardData.FullNameTh}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Reader timed out waiting for citizen card insertion.");
}
```

---

## 4. Zero-Trust Security & PII Protection

1. **Fail-Closed Principle**:
   If the MoI Applet selection returns any status word other than `0x90 0x00`, execution fails immediately with `VerificationException(SMARTCARD_SELECTION_FAILED)`.
2. **Modulo 11 Checksum Enforcement**:
   The 13-digit Thai National ID is verified against the official Modulo 11 checksum algorithm. Invalid numbers throw `IdentityValidationException(INVALID_CITIZEN_ID_CHECKSUM)`.
3. **No Secret or Raw PII Leakage**:
   All exception messages and audit log entries mask the National ID (e.g. `1********3451`) using `StringMaskingHelper.MaskPid(...)`.
4. **Sub-second Performance Guarantee**:
   The pipeline executes in under 800ms end-to-end to ensure smooth user experience at physical kiosks.
