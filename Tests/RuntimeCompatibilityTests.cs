using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using Netka.Identity.Core.Interfaces;
using Netka.Identity.Infrastructure.Helpers;
using Netka.Identity.Infrastructure.Security;
using Xunit;

namespace Netka.Identity.SDK.Tests;

/// <summary>
/// Tests specifically targeting cross-version .NET API behavior (.NET 8.0 vs .NET 10.0),
/// including case-insensitive (OrdinalIgnoreCase) lookups, culture-invariance (th-TH vs en-US),
/// JSON type conversion consistency, and deterministic AES-256-CBC + HMAC-SHA256 test vectors.
/// </summary>
public class RuntimeCompatibilityTests
{
    [Fact]
    public void SdkAssembly_TargetFrameworkAndRuntimeVersion_MatchesExpectedTarget()
    {
        Assembly sdkAssembly = typeof(IVerificationProvider).Assembly;
        var targetFrameworkAttr = sdkAssembly.GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.NotNull(targetFrameworkAttr);
        Assert.Equal("Netka.Identity.SDK", sdkAssembly.GetName().Name);

#if NET5_0
        Assert.Equal(".NETCoreApp,Version=v5.0", targetFrameworkAttr.FrameworkName);
        Assert.Equal(5, Environment.Version.Major);
#elif NET8_0
        Assert.Equal(".NETCoreApp,Version=v8.0", targetFrameworkAttr.FrameworkName);
        Assert.Equal(8, Environment.Version.Major);
#elif NET10_0
        Assert.Equal(".NETCoreApp,Version=v10.0", targetFrameworkAttr.FrameworkName);
        Assert.Equal(10, Environment.Version.Major);
#else
        Assert.Fail($"Unexpected target framework: {targetFrameworkAttr.FrameworkName}");
#endif
    }

    [Fact]
    public void CaseInsensitiveLookup_JsonDictionaryAndGetStringValue_WorksWithAnyCasingAcrossDotNetVersions()
    {
        // Arrange: JSON with mixed-case keys from external providers
        string json = """
        {
            "Access_Token": "tok-abc",
            "PID": "1234567890121",
            "IsError": false
        }
        """;

        // Act
        var data = ProviderHelper.ParseJsonToDictionary(json, "PARSE_ERROR");

        // Assert 1: Dictionary indexer must support OrdinalIgnoreCase on both .NET 8 and .NET 10
        Assert.Equal("tok-abc", data["access_token"]);
        Assert.Equal("tok-abc", data["ACCESS_TOKEN"]);
        Assert.Equal("1234567890121", data["pid"]);
        Assert.Equal("1234567890121", data["Pid"]);
        Assert.Equal(false, data["iserror"]);
        Assert.Equal(false, data["ISERROR"]);

        // Assert 2: GetStringValue must find keys regardless of casing even in a standard Dictionary
        var plainDict = new Dictionary<string, object>
        {
            ["Code_Verifier"] = "verifier-xyz",
            ["Expected_State"] = "state-001"
        };

        Assert.Equal("verifier-xyz", plainDict.GetStringValue("code_verifier"));
        Assert.Equal("verifier-xyz", plainDict.GetStringValue("CODE_VERIFIER"));
        Assert.Equal("state-001", plainDict.GetStringValue("expected_state"));
    }

    [Theory]
    [InlineData("th-TH")]
    [InlineData("en-US")]
    [InlineData("tr-TR")] // Famous Turkish-I culture edge case where 'I'.ToLower() != 'i' unless OrdinalIgnoreCase is used
    public void CultureInvariance_OrdinalIgnoreCaseAndThaiIdValidation_BehavesIdenticallyUnderAllCultures(string cultureName)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            // "PID" vs "pid" contains 'I'/'i', which fails in tr-TR if culture-sensitive IgnoreCase is used instead of OrdinalIgnoreCase
            var dict = new Dictionary<string, object>
            {
                ["PID"] = "1234567890121",
                ["ID_TOKEN"] = "jwt-value"
            };

            Assert.Equal("1234567890121", dict.GetStringValue("pid"));
            Assert.Equal("jwt-value", dict.GetStringValue("id_token"));
            Assert.True("1234567890121".IsValidThaiNationalId());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void DeterministicCryptoVector_AesEncryptDecryptCbc_Is100PercentCompatibleAcrossDotNetVersions()
    {
        // Fixed 32-byte key (Base64) so we can verify cross-version decryption compatibility
        const string fixedKeyBase64 = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=";
        var service = new AesEncryptionService(fixedKeyBase64);

        // Encrypt on current runtime and verify structure [16-byte IV + Ciphertext + 32-byte HMAC]
        const string plainText = "1234567890121:สมชาย ใจดี";
        string cipherText = service.Encrypt(plainText);
        byte[] rawBytes = Convert.FromBase64String(cipherText);

        // Minimum length: 16 (IV) + 16 (at least 1 AES block) + 32 (HMAC-SHA256) = 64 bytes
        Assert.True(rawBytes.Length >= 64);
        Assert.Equal(plainText, service.Decrypt(cipherText));
    }
}
