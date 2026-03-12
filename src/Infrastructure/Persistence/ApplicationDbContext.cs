using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Checklist> Checklists => Set<Checklist>();

    public DbSet<Section> Sections => Set<Section>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Checklist>(entity =>
        {
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);

            entity.Property(c => c.Status)
                .HasConversion<string>();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Login).IsRequired().HasMaxLength(50);
            entity.HasIndex(u => u.Login).IsUnique();

            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(50);

            entity.Property(u => u.AccountStatus).HasConversion<string>();
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Position).IsRequired();
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.Property(t => t.Content).IsRequired();
            entity.Property(t => t.Position).IsRequired();
        });
    }
}
