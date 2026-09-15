using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using VariableCompensation.Application.Abstractions.Security;

namespace VariableCompensation.Infrastructure.Security;

public sealed class AesSensitiveDataEncryptionService : ISensitiveDataEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] key;

    public AesSensitiveDataEncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["SalaryEncryption:Key"];
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException("SalaryEncryption:Key is not configured.");
        }

        this.key = Convert.FromBase64String(keyBase64);
        if (this.key.Length != 32)
        {
            throw new InvalidOperationException("SalaryEncryption:Key must be a 32-byte base64 value.");
        }
    }

    public byte[] EncryptDecimal(decimal value)
    {
        var plaintext = Encoding.UTF8.GetBytes(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(this.key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, payload, NonceSize + ciphertext.Length, TagSize);
        return payload;
    }

    public decimal DecryptDecimal(byte[] encryptedPayload)
    {
        if (encryptedPayload.Length < NonceSize + TagSize + 1)
        {
            throw new InvalidOperationException("Encrypted payload is invalid.");
        }

        var nonce = encryptedPayload.AsSpan(0, NonceSize);
        var tagStart = encryptedPayload.Length - TagSize;
        var ciphertextLength = tagStart - NonceSize;
        var ciphertext = encryptedPayload.AsSpan(NonceSize, ciphertextLength);
        var tag = encryptedPayload.AsSpan(tagStart, TagSize);

        var plaintext = new byte[ciphertextLength];
        using var aes = new AesGcm(this.key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        var text = Encoding.UTF8.GetString(plaintext);
        return decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
    }
}
