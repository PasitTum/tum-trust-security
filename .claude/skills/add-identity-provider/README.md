# add-identity-provider Skill

ชุดทำงาน (Skill) ครบถ้วนสำหรับการสร้าง **Identity Provider ตัวใหม่** เข้าสู่โปรเจกต์ `Netka.Identity.SDK` โดยปฏิบัติตามกฎเหล็กสถาปัตยกรรมที่เคร่งครัด

## 📁 File Structure

```
.claude/skills/add-identity-provider/
├── README.md                 (ไฟล์นี้)
├── SKILL.md                  (คำอธิบายสั้นของ Skill)
├── INSTRUCTIONS.md           (ขั้นตอนโดยละเอียด)
├── TEMPLATES.md              (Code templates พร้อมใช้)
└── QUICK_START.md            (เริ่มต้นอย่างรวดเร็ว)
```

### ไฟล์ละเอียด

| ไฟล์ | ใช้เมื่อ | เนื้อหา |
|------|---------|--------|
| **SKILL.md** | คนแรกที่อ่าน | Overview ของ Skill, วิธีการเรียก, Key Rules |
| **INSTRUCTIONS.md** | กำลังสร้าง Provider | ขั้นตอนละเอียด 7 ขั้น (Spec → Build → Docs) |
| **TEMPLATES.md** | ต้อง Copy-Paste Code | Code templates พร้อมใช้ (Interface, Provider, Tests) |
| **QUICK_START.md** | อยากเริ่มเร็ว | ตัวอย่างจริง (OAuth 2.0, Direct REST) + checklist |
| **README.md** | ต้องคำแนะนำทั่วไป | ไฟล์นี้ — บอกทำไมต้องใช้ Skill นี้ |

---

## 🎯 ทำไมต้องใช้ Skill นี้?

### โปรเจกต์ Netka.Identity.SDK

- **Multi-Target Build:** ต้องรันผ่าน `.NET 5.0`, `.NET 8.0`, `.NET 10.0` พร้อมกัน
- **กฎสถาปัตยกรรมเข้มงวด:** Interface Strongly-Typed, Pass-Through Adapter, Reuse Helpers
- **Cross-Version Compatibility:** ห้ามใช้ API ใหม่ (`.NET 6+`), ต้องใช้ Workarounds
- **Audit & Encryption:** ข้อมูลสำคัญต้อง Log + Encrypt
- **Unit Tests บน 3 Runtimes:** ต้อง Build & Test ครบ 0 Warnings, 0 Errors

### ความท้าทาย

```
❌ ถ้าทำเองมือ:
   - ลืมใส่ XML Docs → CS1591 Warning
   - ใช้ ArgumentNullException.ThrowIfNull() → Build ล้มบน net5.0
   - FormUrlEncodedContent ไม่ encode null values → CS8620
   - ตัด JWT token จาก Response → ข้อมูล Lost, audit log broken
   - ลืมลงทะเบียน DI → Service ไม่สร้างได้
   - ลืมสร้าง Unit Tests → ไม่มี Code Coverage

✅ ถ้าใช้ Skill นี้:
   - Step-by-step guidance ครบทั้ง 5 ขั้นตอน
   - Code templates พร้อมใช้
   - Cross-version compatibility checks built-in
   - Dependency Injection walkthrough
   - Unit test scaffolding
   - Multi-target build verification
   - Documentation updates
```

---

## 🚀 Quick Start

### วิธีเรียกใช้

```bash
/add-identity-provider
```

จากนั้นทำตามขั้นตอน:

1. **ขั้นตอนที่ 1:** ส่งสเปก API (URL / ข้อความ / ชื่อ Provider)
2. **ขั้นตอนที่ 2:** Skill วิเคราะห์และสร้าง Interface + Provider Class
3. **ขั้นตอนที่ 3:** ตรวจสอบ Cross-Version Compatibility
4. **ขั้นตอนที่ 4:** ลงทะเบียน DI + Configuration
5. **ขั้นตอนที่ 5:** สร้าง Unit Tests + Build Multi-Target

---

## 📋 Type Classification

### Type A — OAuth 2.0 / OpenID Connect

```
[User Browser]
    ↓
[Authorization URL] → /authorize
    ↓
[User Login & Consent]
    ↓
[Redirect with Code]
    ↓
[Code ↔ Token Exchange] → /token
    ↓
[Get User Info] → /userinfo
    ↓
[Access Verified]
```

**ตัวอย่าง:** Google, Azure AD, LINE Login, GitHub

**Interface Methods:**
- `GetAuthorizationUrlAsync()` ✅
- `ExchangeCodeForTokenAsync()` ✅
- `GetUserProfileAsync()` ✅
- `RefreshAccessTokenAsync()` ✅
- `InspectTokenAsync()` ✅
- `RevokeTokenAsync()` ✅

---

### Type B — Direct REST / Hardware Verification

```
[Server A]
    ↓
[Direct API Call + Credentials] → /verify
    ↓
[Provider Validates]
    ↓
[Response with Result]
    ↓
[Verification Complete]
```

**ตัวอย่าง:** DOPA (Thai ID), Card Reader, Hardware Verification, SMS OTP

**Interface Methods:**
- `Verify[PROVIDER_NAME]DataAsync()` ✅
- `[METHOD_NAME]Async()` (custom) ✅
- ❌ NO `GetAuthorizationUrlAsync()`

---

## 🔒 Key Architecture Rules

### 1️⃣ One-to-One Mapping (Interface → Provider)

```
Core/Interfaces/I[NAME]Provider.cs
    ↓
Infrastructure/Providers/[NAME]Provider.cs
    ↓ implements
```

**ห้าม:**
- Mix multiple providers ในไฟล์เดียว
- Add helper classes ใน Providers folder
- Put DTO/Options classes ที่ไม่ใช่ public

---

### 2️⃣ Pass-Through Adapter

```
[Provider API Response]
    ↓
ProviderHelper.ParseJsonToDictionary()
    ↓
VerificationResult.Data (100% unchanged)
    ↓ MUST NOT cut tokens or fake fields
```

---

### 3️⃣ Reuse Helpers (Don't Reinvent)

```csharp
✅ ProviderHelper.GetRequiredValue(_configuration, "...")
✅ ProviderHelper.ParseJsonToDictionary(json, errorCode)
✅ ProviderHelper.CreateFailureResult(errorCode, message, statusCode)
✅ _logger.LogEncryptedPiiIfPresent(_encryptionService, ...)
✅ pid.IsValidThaiNationalId()

❌ Don't create custom config parsers
❌ Don't add custom JSON parsing
❌ Don't manually log tokens/PII
```

---

### 4️⃣ Cross-Version Compatibility

```csharp
❌ ArgumentNullException.ThrowIfNull(x)    → .NET 6+ only
✅ if (x is null) throw new ArgumentNullException(nameof(x));

❌ new FormUrlEncodedContent(dict)         → CS8620 on .NET 5.0
✅ new FormUrlEncodedContent(
     dict.Select(kvp => new KeyValuePair<string?, string?>(kvp.Key, kvp.Value)))

❌ if (key == "VALUE")                      → Culture-dependent
✅ key.Equals("VALUE", StringComparison.OrdinalIgnoreCase)

❌ RandomNumberGenerator.GetBytes(10)      → .NET 6+ only
✅ byte[] bytes = new byte[10]; RandomNumberGenerator.Fill(bytes);
```

---

### 5️⃣ Audit & Documentation

```csharp
[AuditLog]                              // Class-level attribute
public class GoogleProvider : IVerificationProvider
{
    /// <summary>                        // XML Docs (required)
    /// Gets authorization URL.
    /// </summary>
    public async Task<string> GetAuthorizationUrlAsync(...)
    {
        // Implementation
    }
}
```

**No CS1591 Warnings allowed!**

---

## 🧪 Build & Test Workflow

### Commands

```bash
# Restore dependencies
dotnet restore Netka.Identity.SDK.sln

# Build all target frameworks
dotnet build Netka.Identity.SDK.sln -c Release

# Run tests on all 3 runtimes
dotnet test Netka.Identity.SDK.sln

# Expected output:
# ✓ net5.0 tests passed
# ✓ net8.0 tests passed
# ✓ net10.0 tests passed
# 0 Warnings, 0 Errors
```

### Verification Checklist

- [ ] Build succeeded (0 Warnings)
- [ ] All 3 target frameworks compiled
- [ ] Unit tests passed on net5.0, net8.0, net10.0
- [ ] No CS1591 (missing XML Docs) warnings
- [ ] DI registration tests passed
- [ ] Mock HTTP responses working
- [ ] Sensitive data encryption working

---

## 📚 Documentation Updates

สุดท้าย ต้องอัปเดตเอกสารเหล่านี้:

| ไฟล์ | การเปลี่ยนแปลง |
|------|----------------|
| `README.md` | เพิ่ม Provider ใหม่ลงในรายการ Supported Providers |
| `ARCHITECTURE.md` | บรรยาย Flow Diagram + Configuration details |
| `IMPLEMENTATION_NOTES.md` | เขียน Known Issues, Performance Considerations |
| `identity_verification_sdk_blueprint.md` | ติดสถานะ "Completed" ใน checklist |

---

## 🎓 Learning Path

### ขั้นตอนที่ 1 - ทำความเข้าใจ (15 นาที)
1. อ่าน `SKILL.md` → เข้าใจ Type A vs Type B
2. ดูตัวอย่างใน `Core/Interfaces/IThaIdProvider.cs` และ `IDopaProvider.cs`
3. ดูตัวอย่างใน `Infrastructure/Providers/ThaIdProvider.cs` และ `DopaProvider.cs`

### ขั้นตอนที่ 2 - เตรียม Spec (30 นาที)
1. ค้นหา Official API Documentation ของ Provider
2. List Endpoints, HTTP Methods, Headers, Parameters, Response
3. Classify เป็น Type A หรือ Type B

### ขั้นตอนที่ 3 - รันทำการ (2-3 ชั่วโมง)
1. เรียก `/add-identity-provider`
2. ปฏิบัติตามขั้นตอน
3. Copy-paste Code templates จาก `TEMPLATES.md`
4. Verify Multi-target Build + Tests

### ขั้นตอนที่ 4 - Documentation (30 นาที)
1. อัปเดต `README.md`, `ARCHITECTURE.md` เป็นต้น
2. สร้าง Git commit
3. (Optional) สร้าง PR หรือ Push to branch

---

## 🐛 Troubleshooting

### Build Failed: "The type 'X' is not defined"
→ Namespace ผิด หรือ forgot to add `using` statement

### CS8620 Warning in .NET 5.0
→ ใช้ `new FormUrlEncodedContent(dict.Select(...))` pattern

### Tests Failed: "Service not registered"
→ ลืม register DI ใน `DependencyInjection.cs`

### "0 Warnings" ไม่ผ่าน
→ ต้องเขียน XML Docs ให้ครบทุก Public members

### HTTP Response บางฟิลด์ Missing
→ ห้ามตัด/แก้ไข Response — ให้ส่งข้อมูลต้นฉบับ 100%

---

## ❓ FAQ

**Q: ต้องสร้าง Provider ใหม่ตั้งแต่ 0 หรือใช้ Skill นี้ได้ทั้งแก้ไขเดิม?**
A: Skill นี้สำหรับ **สร้างใหม่** เท่านั้น การแก้ไข Provider ที่มีอยู่ให้ทำด้วยมือหรือเรียก `/code-review`

**Q: ถ้า Provider ต้อง Custom HTTP Headers หรือ Encryption?**
A: ใช้ `ConfigureProviderHttpClient()` ใน `DependencyInjection.cs` ได้ — helper นี้รองรับ custom headers แล้ว

**Q: Multi-Target Build ต้องทำทั้งหมดเสมอหรือ?**
A: ใช่ — ข้อกำหนด mandatory ของโปรเจกต์ SDK

**Q: ข้อมูล Token ต้อง Encrypt ในทุก Provider หรือ?**
A: ใช่ — `_logger.LogEncryptedPiiIfPresent()` ต้องใช้เพื่อป้องกัน Token Leakage ในทะเบียนเซิร์ฟเวอร์

---

## 📞 Support

หากมีปัญหา:
1. ตรวจสอบ `INSTRUCTIONS.md` ขั้นตอนที่เกี่ยวข้อง
2. ดู `TEMPLATES.md` ว่า Code pattern ถูกไหม
3. รัน `dotnet build` / `dotnet test` ดูข้อความ Error
4. เปิด Issue หากมีบั๊กใน Skill นี้

---

**Skill Version:** 1.0.0  
**Last Updated:** 2026-10-12  
**Status:** ✅ Ready for Production Use
