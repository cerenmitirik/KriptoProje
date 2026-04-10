using System.Security.Cryptography;
using System.Text;

namespace KriptoProje.Data;

public static class HashHelper
{
    // Kayıt olurken kullanılacak: Şifreyi mühürler.
    public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA256(); 
        passwordSalt = hmac.Key; // Bu otomatik üretilen benzersiz tuzdur.
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    // Giriş yaparken kullanılacak: Girilen şifre veritabanındakiyle aynı mı?
    public static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using var hmac = new HMACSHA256(passwordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(passwordHash);
    }
}