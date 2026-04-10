using Microsoft.AspNetCore.Mvc;
using KriptoProje.Data;
using KriptoProje.Models;

namespace KriptoProje.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public IActionResult Register(string username, string email, string password)
    {
        // Şifreyi HashHelper ile mühürle
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

        return RedirectToAction("Index", "Home");
    }
}