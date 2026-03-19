using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

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

            entity.HasOne<ApplicationUser>()
                .WithMany(u => u.Checklists)
                .HasForeignKey(c => c.UserId);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
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
