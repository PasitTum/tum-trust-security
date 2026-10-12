using System.Security.Cryptography;
using Netka.Identity.Infrastructure.Security;
using Xunit;

namespace Netka.Identity.SDK.Tests;

public class SecurityTests
{
    private static string GenerateBase64Key32Bytes()
    {
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }

    [Fact]
    public void AesEncryptionService_EncryptAndDecrypt_ReturnsOriginalPlainText()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateBase64Key32Bytes());
        string original = "1234567890121|สมชาย ใจดี";

        // Act
        string encrypted = service.Encrypt(original);
        string decrypted = service.Decrypt(encrypted);

        // Assert
        Assert.NotEqual(original, encrypted);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void AesEncryptionService_EncryptSameTextTwice_ProducesDifferentCipherTextDueToRandomIv()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateBase64Key32Bytes());
        string pid = "1234567890121";

        // Act
        string cipher1 = service.Encrypt(pid);
        string cipher2 = service.Encrypt(pid);

        // Assert
        Assert.NotEqual(cipher1, cipher2);
        Assert.Equal(pid, service.Decrypt(cipher1));
        Assert.Equal(pid, service.Decrypt(cipher2));
    }

    [Fact]
    public void AesEncryptionService_TamperedCipherText_ThrowsCryptographicException()
    {
        // Arrange
        var service = new AesEncryptionService(GenerateBase64Key32Bytes());
        string encrypted = service.Encrypt("1234567890121");

        byte[] bytes = Convert.FromBase64String(encrypted);
        bytes[20] ^= 0xFF; // Flip bits inside ciphertext
        string tampered = Convert.ToBase64String(bytes);

        // Act & Assert
        Assert.Throws<CryptographicException>(() => service.Decrypt(tampered));
    }

    [Fact]
    public void AesEncryptionService_InvalidKeyLength_ThrowsArgumentException()
    {
        // Arrange (16-byte key instead of required 32-byte key)
        byte[] key16 = new byte[16];
        RandomNumberGenerator.Fill(key16);
        string shortKey = Convert.ToBase64String(key16);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AesEncryptionService(shortKey));
    }

    [Fact]
    public void PkceHelper_GenerateAndVerifyChallenge_SucceedsForMatchingVerifier()
    {
        // Act
        string verifier = PkceHelper.GenerateCodeVerifier();
        string challenge = PkceHelper.GenerateCodeChallenge(verifier);
        string wrongVerifier = PkceHelper.GenerateCodeVerifier();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(verifier));
        Assert.False(string.IsNullOrWhiteSpace(challenge));
        Assert.True(PkceHelper.VerifyCodeChallenge(verifier, challenge));
        Assert.False(PkceHelper.VerifyCodeChallenge(wrongVerifier, challenge));
    }
}
