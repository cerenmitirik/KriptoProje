using Microsoft.EntityFrameworkCore; // Bunu ekle
using KriptoProje.Data;              // Bunu ekle

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
    });

// --- BURAYI EKLE ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// -------------------

var app = builder.Build();

// ... (Geri kalan kodlar aynı kalabilir)

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
pattern: "{controller=Account}/{action=Register}/{id?}")    .WithStaticAssets();

app.Run();