namespace Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;

/// <summary>
/// Entity Framework Core DbContext for CleanArch database.
/// Manages database connection and migrations.
/// </summary>
public class CleanArchDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    public CleanArchDbContext(DbContextOptions<CleanArchDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.RefreshToken)
                .HasMaxLength(512);

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("User");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.IsActive);
        });
    }
}
