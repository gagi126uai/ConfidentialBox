using System.Security.Cryptography;
using System.Text;

namespace ConfidentialBox.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    public string GenerateEncryptionKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var key = new byte[32]; // 256 bits
        rng.GetBytes(key);
        return Convert.ToBase64String(key);
    }

    public string Encrypt(string plainText, string key)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(key);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var msEncrypt = new MemoryStream();

        // Escribir IV al inicio
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string Decrypt(string cipherText, string key)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(key);

        // Extraer IV
        var iv = new byte[aes.IV.Length];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }
}