using Microsoft.EntityFrameworkCore;
using BookTracker.Api.Models;

namespace BookTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email)
                  .HasDatabaseName("IX_Users_Email")
                  .IsUnique();
        });
    }
}
