using Microsoft.EntityFrameworkCore;
using TcpLicenseServer.Models;

namespace TcpLicenseServer.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Config> Configs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        => optionsBuilder.UseSqlite("Data Source=licenseServer.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.Key);
        modelBuilder.Entity<User>().HasIndex(u => u.Hwid);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Configs)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
