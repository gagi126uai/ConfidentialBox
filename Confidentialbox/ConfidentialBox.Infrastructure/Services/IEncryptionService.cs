namespace ConfidentialBox.Infrastructure.Services;

public interface IEncryptionService
{
    string GenerateEncryptionKey();
    string Encrypt(string plainText, string key);
    string Decrypt(string cipherText, string key);
}