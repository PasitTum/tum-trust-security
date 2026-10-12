using System.Security.Cryptography;
using System.Text;
using Netka.Identity.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Netka.Identity.Infrastructure.Security;

/// <summary>
/// Implements AES-256-CBC encryption with HMAC-SHA256 integrity verification for PII protection.
/// Compatible across .NET 5.0, .NET 8.0, and .NET 10.0.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private const int KeySizeBytes = 32; // 256-bit key
    private const int IvSizeBytes = 16;  // 128-bit IV
    private const int TagSizeBytes = 32; // 256-bit HMAC-SHA256 tag

    private readonly byte[] _key;
    private readonly ILogger<AesEncryptionService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AesEncryptionService"/>.
    /// </summary>
    /// <param name="base64Key">Base64-encoded 32-byte (256-bit) key.</param>
    /// <param name="logger">Optional logger for activity logging.</param>
    public AesEncryptionService(string base64Key, ILogger<AesEncryptionService>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new ArgumentException("Encryption key cannot be null or whitespace.", nameof(base64Key));
        }

        try
        {
            _key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Encryption key must be valid Base64.", nameof(base64Key), ex);
        }

        if (_key.Length != KeySizeBytes)
        {
            throw new ArgumentException($"Encryption key must be exactly {KeySizeBytes} bytes (256 bits).", nameof(base64Key));
        }

        _logger = logger ?? NullLogger<AesEncryptionService>.Instance;
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        if (plainText is null)
        {
            throw new ArgumentNullException(nameof(plainText));
        }

        byte[] iv = new byte[IvSizeBytes];
        RandomNumberGenerator.Fill(iv);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Combine IV + CipherText to compute HMAC-SHA256 integrity tag
        byte[] ivAndCipher = new byte[iv.Length + cipherBytes.Length];
        Buffer.BlockCopy(iv, 0, ivAndCipher, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, 0, ivAndCipher, iv.Length, cipherBytes.Length);

        using var hmac = new HMACSHA256(_key);
        byte[] tag = hmac.ComputeHash(ivAndCipher);

        // Final payload layout: [16-byte IV][CipherText][32-byte HMAC]
        byte[] result = new byte[ivAndCipher.Length + tag.Length];
        Buffer.BlockCopy(ivAndCipher, 0, result, 0, ivAndCipher.Length);
        Buffer.BlockCopy(tag, 0, result, ivAndCipher.Length, tag.Length);

        return Convert.ToBase64String(result);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        if (cipherText is null)
        {
            throw new ArgumentNullException(nameof(cipherText));
        }

        byte[] fullBytes;
        try
        {
            fullBytes = Convert.FromBase64String(cipherText);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Ciphertext is not valid Base64.", ex);
        }

        int minLength = IvSizeBytes + TagSizeBytes + 1;
        if (fullBytes.Length < minLength)
        {
            throw new CryptographicException("Ciphertext payload is too short to be valid.");
        }

        int ivAndCipherLength = fullBytes.Length - TagSizeBytes;

        byte[] ivAndCipher = new byte[ivAndCipherLength];
        byte[] expectedTag = new byte[TagSizeBytes];
        Buffer.BlockCopy(fullBytes, 0, ivAndCipher, 0, ivAndCipherLength);
        Buffer.BlockCopy(fullBytes, ivAndCipherLength, expectedTag, 0, TagSizeBytes);

        // 1. Verify HMAC-SHA256 integrity in constant time before decrypting
        using var hmac = new HMACSHA256(_key);
        byte[] actualTag = hmac.ComputeHash(ivAndCipher);
        if (!CryptographicOperations.FixedTimeEquals(expectedTag, actualTag))
        {
            _logger.LogWarning("HMAC integrity check failed during PII decryption.");
            throw new CryptographicException("Ciphertext integrity check (HMAC-SHA256) failed.");
        }

        // 2. Extract IV and CipherBytes and decrypt
        byte[] iv = new byte[IvSizeBytes];
        int cipherLength = ivAndCipherLength - IvSizeBytes;
        byte[] cipherBytes = new byte[cipherLength];
        Buffer.BlockCopy(ivAndCipher, 0, iv, 0, IvSizeBytes);
        Buffer.BlockCopy(ivAndCipher, IvSizeBytes, cipherBytes, 0, cipherLength);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
