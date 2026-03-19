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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Checklist>(entity =>
        {
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);

            entity.Property(c => c.Status)
                .HasConversion<string>();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.UserId);

            entity.Ignore(c => c.Author);
        });

        builder.Entity<Section>(entity =>
        {
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Position).IsRequired();
        });

        builder.Entity<TaskItem>(entity =>
        {
            entity.Property(t => t.Content).IsRequired();
            entity.Property(t => t.Position).IsRequired();
        });
    }
}
