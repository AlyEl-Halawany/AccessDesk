using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;

namespace AccessDesk.Infrastructure.Persistence;

/// <summary>
/// The main EF Core database context. We inherit from IdentityDbContext so that
/// ASP.NET Core Identity's tables (users, roles, claims, tokens) are managed here too.
/// Using int as the key type to keep it simple and to avoid GUIDs in URLs.
/// </summary>
public class AccessDeskDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public AccessDeskDbContext(DbContextOptions<AccessDeskDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.Application> Applications => Set<Domain.Entities.Application>();
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SodConflict> SodConflicts => Set<SodConflict>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AccessReview> AccessReviews => Set<AccessReview>();
    public DbSet<AccessReviewItem> AccessReviewItems => Set<AccessReviewItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Must call base — sets up Identity tables

        // Apply all entity configurations from this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(AccessDeskDbContext).Assembly);
    }
}
