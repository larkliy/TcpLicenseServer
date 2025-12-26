using Microsoft.EntityFrameworkCore;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=licenseServer.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.Key);
        modelBuilder.Entity<User>().HasIndex(u => u.Hwid);
    }
}
