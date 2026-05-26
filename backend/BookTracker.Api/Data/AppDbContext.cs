using Microsoft.EntityFrameworkCore;
using BookTracker.Api.Models;

namespace BookTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<UserBook> UserBooks => Set<UserBook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email)
                  .HasDatabaseName("IX_Users_Email")
                  .IsUnique();
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasIndex(b => b.ISBN)
                  .HasDatabaseName("UQ_Books_ISBN")
                  .IsUnique();
        });

        modelBuilder.Entity<UserBook>(entity =>
        {
            entity.Property(u => u.Status).HasConversion<string>();

            entity.HasOne(u => u.User)
                  .WithMany()
                  .HasForeignKey(u => u.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(u => u.Book)
                  .WithMany()
                  .HasForeignKey(u => u.BookId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
