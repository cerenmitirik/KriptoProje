using Microsoft.EntityFrameworkCore;
using KriptoProje.Models;

namespace KriptoProje.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
}