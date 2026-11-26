using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessDesk.Domain.Entities;

namespace AccessDesk.Infrastructure.Persistence.Configurations;

public class AccessRequestConfiguration : IEntityTypeConfiguration<AccessRequest>
{
    public void Configure(EntityTypeBuilder<AccessRequest> builder)
    {
        builder.ToTable("AccessRequests");

        builder.Property(r => r.Justification).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.RejectionReason).HasMaxLength(2000);

        // Convert enum to string in DB so migrations are readable
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.RequestedLevel).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(r => r.Requester)
            .WithMany(u => u.SubmittedRequests)
            .HasForeignKey(r => r.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Application)
            .WithMany(a => a.AccessRequests)
            .HasForeignKey(r => r.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ManagerApprover)
            .WithMany()
            .HasForeignKey(r => r.ManagerApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.OwnerApprover)
            .WithMany()
            .HasForeignKey(r => r.OwnerApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Computed column — do not map ExpiresAt to DB, it's calculated in C#
        builder.Ignore(r => r.ExpiresAt);

        // Index to quickly find requests by status (the approval queues query this a lot)
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RequesterId);
    }
}
