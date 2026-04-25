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

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        byte[] iv = new byte[16];
        byte[] array;

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(_key.PadRight(32).Substring(0, 32)); // Ensure 32 bytes for AES-256
            aes.IV = iv; // For demonstration, IV is 0s. Best practice is to generate it randomly and prepend it.
            // Ama proje basitliği için IV sabit bırakılabilir veya rastgele oluşturulup başa eklenebilir.
            // Biz rastgele oluşturalım:
            RandomNumberGenerator.Fill(iv);
            aes.IV = iv;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                // IV'yi en başa yazalım ki çözerken kullanabilelim
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

        // 0'dan 16'ya kadar olan kısım IV
        Array.Copy(fullCipher, iv, 16);
        // Kalan kısım şifreli metin
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
}
