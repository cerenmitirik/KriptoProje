using System.Security.Cryptography;
using System.Text;

namespace KriptoProje.Services;

public class CryptoService
{
    private readonly string _key;

    public CryptoService(IConfiguration configuration)
    {
        _key = configuration.GetValue<string>("EncryptionKey") ?? throw new InvalidOperationException("EncryptionKey is missing in appsettings.json");
    }

    // --- SİMETRİK ŞİFRELEME (AES-256) ---
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        byte[] iv = new byte[16];
        byte[] array;

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(_key.PadRight(32).Substring(0, 32));
            RandomNumberGenerator.Fill(iv);
            aes.IV = iv;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(iv, 0, iv.Length);
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(plainText);
                    }
                    array = memoryStream.ToArray();
                }
            }
        }
        return Convert.ToBase64String(array);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        byte[] fullCipher = Convert.FromBase64String(cipherText);
        byte[] iv = new byte[16];
        byte[] cipher = new byte[fullCipher.Length - 16];

        Array.Copy(fullCipher, iv, 16);
        Array.Copy(fullCipher, 16, cipher, 0, cipher.Length);

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(_key.PadRight(32).Substring(0, 32));
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream memoryStream = new MemoryStream(cipher))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader streamReader = new StreamReader(cryptoStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }
    }

    // --- HASHLEME (SHA-256) ---
    public string ComputeHash(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder builder = new StringBuilder();
            foreach (var t in bytes)
            {
                builder.Append(t.ToString("x2"));
            }
            return builder.ToString();
        }
    }

    // --- ASİMETRİK ŞİFRELEME VE DİJİTAL İMZA (RSA-2048) ---

    public (string publicKey, string privateKey) GenerateRSAKeys()
    {
        using (RSA rsa = RSA.Create(2048))
        {
            string publicKey = rsa.ToXmlString(false);
            string privateKey = rsa.ToXmlString(true);
            return (publicKey, privateKey);
        }
    }

    public string SignData(string plainText, string privateKeyXml)
    {
        using (RSA rsa = RSA.Create())
        {
            rsa.FromXmlString(privateKeyXml);
            byte[] dataBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signatureBytes);
        }
    }

    public bool VerifySignature(string plainText, string signatureBase64, string publicKeyXml)
    {
        if (string.IsNullOrEmpty(signatureBase64)) return false;

        using (RSA rsa = RSA.Create())
        {
            rsa.FromXmlString(publicKeyXml);
            byte[] dataBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] signatureBytes = Convert.FromBase64String(signatureBase64);
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }

    // --- 4. HAFTA: ANAHTAR DEĞİŞİMİ (KEY EXCHANGE) METOTLARI ---

    // Gönderici tarafı: Simetrik anahtarı (AES Key) alıcının Public Key'i ile şifreler
    public string EncryptAESKey(string aesKey, string publicKeyXml)
    {
        using (RSA rsa = RSA.Create())
        {
            rsa.FromXmlString(publicKeyXml);
            byte[] keyBytes = Encoding.UTF8.GetBytes(aesKey);
            // Simetrik anahtar asimetrik olarak sarmalanıyor (Key Wrapping)
            byte[] encryptedKey = rsa.Encrypt(keyBytes, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encryptedKey);
        }
    }

    // Alıcı tarafı: Şifreli gelen anahtarı kendi Private Key'i ile çözer
    public string DecryptAESKey(string encryptedKeyBase64, string privateKeyXml)
    {
        using (RSA rsa = RSA.Create())
        {
            rsa.FromXmlString(privateKeyXml);
            byte[] encryptedKeyBytes = Convert.FromBase64String(encryptedKeyBase64);
            byte[] decryptedKeyBytes = rsa.Decrypt(encryptedKeyBytes, RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(decryptedKeyBytes);
        }
    }
}