namespace Tum.Trust.Security.Core.Interfaces;

/// <summary>
/// Defines the contract for encrypting and decrypting sensitive Personally Identifiable Information (PII).
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plaintext using AES-256.
    /// </summary>
    /// <param name="plainText">The unencrypted sensitive text.</param>
    /// <returns>Base64-encoded ciphertext with IV and integrity tag.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts Base64-encoded ciphertext back into plaintext.
    /// </summary>
    /// <param name="cipherText">The Base64-encoded ciphertext.</param>
    /// <returns>The decrypted plaintext.</returns>
    string Decrypt(string cipherText);
}
