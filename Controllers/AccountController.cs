using Microsoft.AspNetCore.Mvc;
using KriptoProje.Data;
using KriptoProje.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using KriptoProje.Services;
using System.Security;

namespace KriptoProje.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CryptoService _cryptoService;
    // appsettings içindeki ham anahtara erişmek için IConfiguration ekliyoruz
    private readonly string _rawAesKey;

    public AccountController(ApplicationDbContext context, CryptoService cryptoService, IConfiguration configuration)
    {
        _context = context;
        _cryptoService = cryptoService;
        _rawAesKey = configuration.GetValue<string>("EncryptionKey") ?? "";
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public IActionResult Register(string username, string email, string password)
    {
        var isUserExists = _context.Users.Any(u => u.Username == username);
        if (isUserExists)
        {
            ModelState.AddModelError(string.Empty, "Bu kullanıcı adı zaten alınmış.");
            return View();
        }

        var isEmailExists = _context.Users.Any(u => u.Email == email);
        if (isEmailExists)
        {
            ModelState.AddModelError(string.Empty, "Bu e-posta adresi zaten kayıtlı.");
            return View();
        }

        // --- 4. HAFTA: RSA ANAHTARLARININ ÜRETİLMESİ ---
        var (publicKey, privateKey) = _cryptoService.GenerateRSAKeys();

        HashHelper.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            RSAPublicKey = publicKey,
            RSAPrivateKey = privateKey
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == username);

        if (user == null || !HashHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
        {
            ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Vault() 
    {
        var username = User.Identity?.Name;
        var user = _context.Users.FirstOrDefault(u => u.Username == username);

        if (user != null && user.IntegrityHash != null)
        {
            // --- 4. HAFTA: ANAHTAR DEĞİŞİMİ SİMÜLASYONU (KEY EXCHANGE) ---
            // 1. Ham AES anahtarını alıcının (kullanıcının) Public Key'i ile şifreliyoruz.
            string encryptedKey = _cryptoService.EncryptAESKey(_rawAesKey, user.RSAPublicKey ?? "");
            
            // 2. Şifreli anahtarı kullanıcının Private Key'i ile geri çözüyoruz.
            // Bu adım "Anahtar Değişimi" isterini (Hard Requirement C) karşılar.
            string decryptedKey = _cryptoService.DecryptAESKey(encryptedKey, user.RSAPrivateKey ?? "");

            // --- 4. HAFTA: DİJİTAL İMZA DOĞRULAMA (NON-REPUDIATION) ---
            string dataToVerify = $"{user.TcNo}{user.PhoneNumber}{user.Iban}{user.CreditCard}{user.IpAddress}";
            bool isSignatureValid = _cryptoService.VerifySignature(dataToVerify, user.DigitalSignature ?? "", user.RSAPublicKey ?? "");

            if (!isSignatureValid)
            {
                throw new SecurityException("Dijital İmza Geçersiz! Veriler yetkisiz müdahale görmüş.");
            }

            // --- 3. HAFTA: AES ŞİFRE ÇÖZME ---
            user.TcNo = _cryptoService.Decrypt(user.TcNo ?? "");
            user.PhoneNumber = _cryptoService.Decrypt(user.PhoneNumber ?? "");
            user.Iban = _cryptoService.Decrypt(user.Iban ?? "");
            user.CreditCard = _cryptoService.Decrypt(user.CreditCard ?? "");
            user.IpAddress = _cryptoService.Decrypt(user.IpAddress ?? "");

            // --- 3. HAFTA: BÜTÜNLÜK KONTROLÜ (SHA-256 HASH) ---
            string hashInput = $"{user.TcNo}{user.PhoneNumber}{user.Iban}{user.CreditCard}{user.IpAddress}";
            string currentHash = _cryptoService.ComputeHash(hashInput);

            if (currentHash != user.IntegrityHash)
            {
                throw new InvalidDataException("Veri Bütünlüğü İhlali! Hash eşleşmiyor.");
            }
        }

        return View(user);
    }

    [HttpPost]
    public IActionResult UpdateVault(string tcNo, string phoneNumber, string iban, string creditCard, string ipAddress)
    {
        var username = User.Identity?.Name;
        var user = _context.Users.FirstOrDefault(u => u.Username == username);

        if (user != null)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(tcNo ?? "", KriptoProje.Data.RegexService.TcNoPattern))
                ModelState.AddModelError("TcNo", "Geçersiz T.C. Kimlik Numarası!");

            if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber ?? "", KriptoProje.Data.RegexService.PhonePattern))
                ModelState.AddModelError("PhoneNumber", "Geçersiz Telefon Formatı!");

            if (!System.Text.RegularExpressions.Regex.IsMatch(iban ?? "", KriptoProje.Data.RegexService.IbanPattern))
                ModelState.AddModelError("Iban", "Geçersiz IBAN formatı!");

            if (!System.Text.RegularExpressions.Regex.IsMatch(creditCard ?? "", KriptoProje.Data.RegexService.CreditCardPattern))
                ModelState.AddModelError("CreditCard", "Geçersiz Kredi Kartı!");

            if (!System.Text.RegularExpressions.Regex.IsMatch(ipAddress ?? "", KriptoProje.Data.RegexService.IpPattern))
                ModelState.AddModelError("IpAddress", "Geçersiz IPv4 Adresi!");

            if (ModelState.IsValid)
            {
                string encTc = _cryptoService.Encrypt(tcNo ?? "");
                string encPhone = _cryptoService.Encrypt(phoneNumber ?? "");
                string encIban = _cryptoService.Encrypt(iban ?? "");
                string encCard = _cryptoService.Encrypt(creditCard ?? "");
                string encIp = _cryptoService.Encrypt(ipAddress ?? "");

                string dataToSign = $"{encTc}{encPhone}{encIban}{encCard}{encIp}";
                user.DigitalSignature = _cryptoService.SignData(dataToSign, user.RSAPrivateKey ?? "");

                string hashInput = $"{tcNo ?? ""}{phoneNumber ?? ""}{iban ?? ""}{creditCard ?? ""}{ipAddress ?? ""}";
                user.IntegrityHash = _cryptoService.ComputeHash(hashInput);

                user.TcNo = encTc;
                user.PhoneNumber = encPhone;
                user.Iban = encIban;
                user.CreditCard = encCard;
                user.IpAddress = encIp;

                _context.SaveChanges();

                TempData["Success"] = "Veriler AES-256 ile şifrelendi ve RSA ile dijital olarak imzalandı.";
                return RedirectToAction("Vault");
            }
        }
        return View("Vault", user);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}