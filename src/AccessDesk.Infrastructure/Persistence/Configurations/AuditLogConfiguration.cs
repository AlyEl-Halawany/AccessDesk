using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessDesk.Domain.Entities;

namespace AccessDesk.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.Property(l => l.Action).HasMaxLength(200).IsRequired();
        builder.Property(l => l.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Details).HasMaxLength(4000);
        // OldValues and NewValues are JSON strings, can be large
        builder.Property(l => l.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(l => l.NewValues).HasColumnType("nvarchar(max)");

        builder.HasOne(l => l.Actor)
            .WithMany()
            .HasForeignKey(l => l.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.AccessRequest)
            .WithMany(r => r.AuditLogs)
            .HasForeignKey(l => l.AccessRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for the common search patterns
        builder.HasIndex(l => l.Timestamp);
        builder.HasIndex(l => new { l.EntityType, l.EntityId });
        builder.HasIndex(l => l.ActorId);
    }
}
