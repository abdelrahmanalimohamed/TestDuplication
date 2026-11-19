using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ItemDeduplication.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<MasterItem> MasterItems => Set<MasterItem>();
    public DbSet<ItemAlias> Aliases => Set<ItemAlias>();
    public DbSet<DuplicateCandidate> DuplicateCandidates => Set<DuplicateCandidate>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<MasterItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CanonicalName).HasMaxLength(500).IsRequired();
        });

        b.Entity<ItemAlias>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(500).IsRequired();
            e.Property(x => x.NormalizedName).HasMaxLength(500).IsRequired();
            e.Property(x => x.SourceSystem).HasMaxLength(100).IsRequired();
            e.Property(x => x.SourceCode).HasMaxLength(100).IsRequired();

            e.HasOne(x => x.MasterItem)
                .WithMany(m => m.Aliases)
                .HasForeignKey(x => x.MasterItemId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.SourceSystem, x.SourceCode }).IsUnique();
            e.HasIndex(x => x.NormalizedName);
        });

        b.Entity<DuplicateCandidate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SourceName).HasMaxLength(500).IsRequired();
            e.Property(x => x.NormalizedName).HasMaxLength(500).IsRequired();
            e.Property(x => x.SourceSystem).HasMaxLength(100).IsRequired();
            e.Property(x => x.SourceCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.ReviewedBy).HasMaxLength(200);
            e.Property(x => x.ReviewNotes).HasMaxLength(1000);

            e.HasOne(x => x.MasterItem)
                .WithMany()
                .HasForeignKey(x => x.MasterItemId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.Status);
        });

        b.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.PerformedBy).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.EntityType, x.EntityId });
        });
    }
}
