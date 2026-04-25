using Microsoft.AspNetCore.Mvc;
using KriptoProje.Data;
using KriptoProje.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using KriptoProje.Services;

namespace KriptoProje.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CryptoService _cryptoService;

    public AccountController(ApplicationDbContext context, CryptoService cryptoService)
    {
        _context = context;
        _cryptoService = cryptoService;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
public IActionResult Register(string username, string email, string password)
{
    // 1. KONTROL: Bu kullanıcı adı zaten var mı?
    var isUserExists = _context.Users.Any(u => u.Username == username);
    
    if (isUserExists)
    {
        // Eğer kullanıcı varsa, hata mesajı ekle ve sayfayı tekrar yükle
        ModelState.AddModelError(string.Empty, "Bu kullanıcı adı zaten alınmış. Lütfen başka bir ad deneyin.");
        return View();
    }

    // 2. KONTROL: Bu e-posta zaten kullanılıyor mu? (Opsiyonel ama önerilir)
    var isEmailExists = _context.Users.Any(u => u.Email == email);
    if (isEmailExists)
    {
        ModelState.AddModelError(string.Empty, "Bu e-posta adresi zaten kayıtlı.");
        return View();
    }

    // Kullanıcı adı ve e-posta müsaitse kayıt işlemlerine geç:
    HashHelper.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

    var user = new User
    {
        Username = username,
        Email = email,
        PasswordHash = hash,
        PasswordSalt = salt
    };

    _context.Users.Add(user);
    _context.SaveChanges();

    return RedirectToAction("Login", "Account"); // Kayıt sonrası direkt Login'e yönlendir
}
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == username);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
            return View();
        }

        if (!HashHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
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

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Vault() 
    {
        var username = User.Identity?.Name;
        var user = _context.Users.FirstOrDefault(u => u.Username == username);

        if (user != null && user.IntegrityHash != null)
        {
            // Veritabanındaki şifreli verileri çöz
            string decTcNo = _cryptoService.Decrypt(user.TcNo ?? "");
            string decPhoneNumber = _cryptoService.Decrypt(user.PhoneNumber ?? "");
            string decIban = _cryptoService.Decrypt(user.Iban ?? "");
            string decCreditCard = _cryptoService.Decrypt(user.CreditCard ?? "");
            string decIpAddress = _cryptoService.Decrypt(user.IpAddress ?? "");

            // Hash'i hesapla
            string hashInput = $"{decTcNo}{decPhoneNumber}{decIban}{decCreditCard}{decIpAddress}";
            string currentHash = _cryptoService.ComputeHash(hashInput);

            // Veritabanındaki hash ile şu anki hesaplanan hash'i kontrol et
            if (currentHash != user.IntegrityHash)
            {
                throw new InvalidDataException("Veri Bütünlüğü İhlali! Veriler veritabanı dışında değiştirilmiş olabilir.");
            }

            // Şifreli verilerin çözülmüş hallerini view'a göndermek için modele ata
            // (EF Core takibi açık olduğu için bu değerlerin veritabanına geri kaydedilmemesi gerekir.
            // Sadece okunup gösterileceği için SaveChanges çalışırsa sorun olur.
            // Bu action içinde SaveChanges() çağırmıyoruz.)
            user.TcNo = decTcNo;
            user.PhoneNumber = decPhoneNumber;
            user.Iban = decIban;
            user.CreditCard = decCreditCard;
            user.IpAddress = decIpAddress;
        }

        return View(user);
    }

[HttpPost]
public IActionResult UpdateVault(string tcNo, string phoneNumber, string iban, string creditCard, string ipAddress)
{
    // Giriş yapmış kullanıcıyı veritabanından çekiyoruz
    var username = User.Identity?.Name;
    var user = _context.Users.FirstOrDefault(u => u.Username == username);

    if (user != null)
    {
        // 1. TC No Kontrolü
        if (!System.Text.RegularExpressions.Regex.IsMatch(tcNo ?? "", KriptoProje.Data.RegexService.TcNoPattern))
            ModelState.AddModelError("TcNo", "Geçersiz T.C. Kimlik Numarası!");

        // 2. Telefon Kontrolü
        if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber ?? "", KriptoProje.Data.RegexService.PhonePattern))
            ModelState.AddModelError("PhoneNumber", "Geçersiz Telefon Formatı (05XXXXXXXXX)!");

        // 3. IBAN Kontrolü
        if (!System.Text.RegularExpressions.Regex.IsMatch(iban ?? "", KriptoProje.Data.RegexService.IbanPattern))
            ModelState.AddModelError("Iban", "Geçersiz IBAN (TR ile başlamalı ve 24 rakam olmalı)!");

        // 4. Kredi Kartı Kontrolü
        if (!System.Text.RegularExpressions.Regex.IsMatch(creditCard ?? "", KriptoProje.Data.RegexService.CreditCardPattern))
            ModelState.AddModelError("CreditCard", "Geçersiz Kredi Kartı (16 hane olmalı)!");

        // 5. IP Adresi Kontrolü
        if (!System.Text.RegularExpressions.Regex.IsMatch(ipAddress ?? "", KriptoProje.Data.RegexService.IpPattern))
            ModelState.AddModelError("IpAddress", "Geçersiz IPv4 Adresi!");

        // Eğer Regex kontrollerinden geçtiyse veritabanını güncelle
        if (ModelState.IsValid)
        {
            // Tüm geçerli hassas veri bilgilerinden güncel Integrity Hash hesapla
            string hashInput = $"{tcNo ?? ""}{phoneNumber ?? ""}{iban ?? ""}{creditCard ?? ""}{ipAddress ?? ""}";
            user.IntegrityHash = _cryptoService.ComputeHash(hashInput);

            // Verileri şifreleyerek kaydet
            user.TcNo = _cryptoService.Encrypt(tcNo ?? "");
            user.PhoneNumber = _cryptoService.Encrypt(phoneNumber ?? "");
            user.Iban = _cryptoService.Encrypt(iban ?? "");
            user.CreditCard = _cryptoService.Encrypt(creditCard ?? "");
            user.IpAddress = _cryptoService.Encrypt(ipAddress ?? "");

            _context.SaveChanges();
            TempData["Success"] = "Veriler başarıyla şifrelenip, veri bütünlük özeti alınarak kaydedildi.";
            return RedirectToAction("Vault");
        }
    }

    // Eğer hata varsa (ModelState.IsValid değilse), verileri kaybetmeden sayfaya geri döner
    return View("Vault", user);
}

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}