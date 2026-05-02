namespace KriptoProje.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public byte[] PasswordHash { get; set; } = null!;
    public byte[] PasswordSalt { get; set; } = null!;

    // --- 2. Hafta: Hassas Veriler ---
    public string? TcNo { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Iban { get; set; }
    public string? CreditCard { get; set; }
    public string? IpAddress { get; set; }
    
    public string? IntegrityHash { get; set; }

    // --- 4. Hafta: Asimetrik Şifreleme ve Dijital İmza (RSA) ---
    // Her kullanıcıya özel RSA Açık Anahtarı (Doğrulama için)
    public string? RSAPublicKey { get; set; }

    // Her kullanıcıya özel RSA Gizli Anahtarı (İmzalama için)
    public string? RSAPrivateKey { get; set; }

    // Verinin kurcalanmadığını kanıtlayan Dijital İmza (İnkar Edilemezlik)
    public string? DigitalSignature { get; set; }
}