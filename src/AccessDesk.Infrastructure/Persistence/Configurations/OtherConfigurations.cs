using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccessDesk.Domain.Entities;

namespace AccessDesk.Infrastructure.Persistence.Configurations;

public class SodConflictConfiguration : IEntityTypeConfiguration<SodConflict>
{
    public void Configure(EntityTypeBuilder<SodConflict> builder)
    {
        builder.ToTable("SodConflicts");

        builder.Property(s => s.Description).HasMaxLength(500).IsRequired();
        builder.Property(s => s.FirstLevel).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.SecondLevel).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(s => s.FirstApplication)
            .WithMany(a => a.SodConflictsAsFirst)
            .HasForeignKey(s => s.FirstApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SecondApplication)
            .WithMany(a => a.SodConflictsAsSecond)
            .HasForeignKey(s => s.SecondApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Link).HasMaxLength(500);
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50);

        builder.HasOne(n => n.Recipient)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.RecipientId, n.IsRead });
    }
}

public class AccessReviewConfiguration : IEntityTypeConfiguration<AccessReview>
{
    public void Configure(EntityTypeBuilder<AccessReview> builder)
    {
        builder.ToTable("AccessReviews");

        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Ignore(r => r.IsCompleted);
        builder.Ignore(r => r.IsOverdue);

        builder.HasOne(r => r.InitiatedBy)
            .WithMany()
            .HasForeignKey(r => r.InitiatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AccessReviewItemConfiguration : IEntityTypeConfiguration<AccessReviewItem>
{
    public void Configure(EntityTypeBuilder<AccessReviewItem> builder)
    {
        builder.ToTable("AccessReviewItems");

        builder.Property(i => i.ReviewNote).HasMaxLength(1000);
        builder.Property(i => i.Decision).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(i => i.AccessReview)
            .WithMany(r => r.Items)
            .HasForeignKey(i => i.AccessReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.AccessRequest)
            .WithMany()
            .HasForeignKey(i => i.AccessRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Reviewer)
            .WithMany()
            .HasForeignKey(i => i.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.Property(t => t.TokenHash).HasColumnType("char(64)").IsRequired();
        builder.Property(t => t.ReplacedByTokenHash).HasColumnType("char(64)");

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash).IsUnique();
    }
}
