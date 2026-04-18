namespace KriptoProje.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public byte[] PasswordHash { get; set; } = null!;
    public byte[] PasswordSalt { get; set; } = null!;

    // --- 2. Hafta Eklenen Hassas Veriler ---
    public string? TcNo { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Iban { get; set; }
    public string? CreditCard { get; set; }
    public string? IpAddress { get; set; }
}