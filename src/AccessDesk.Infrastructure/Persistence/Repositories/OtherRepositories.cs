using Microsoft.EntityFrameworkCore;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;
using AccessDesk.Domain.Interfaces;

namespace AccessDesk.Infrastructure.Persistence.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly AccessDeskDbContext _db;

    public ApplicationRepository(AccessDeskDbContext db) => _db = db;

    public async Task<Domain.Entities.Application?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.Applications.Include(a => a.Owner).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<Domain.Entities.Application>> GetAllActiveAsync(CancellationToken ct = default)
        => await _db.Applications.Where(a => a.IsActive).Include(a => a.Owner).OrderBy(a => a.Name).ToListAsync(ct);

    public async Task<IEnumerable<Domain.Entities.Application>> GetByOwnerIdAsync(int ownerId, CancellationToken ct = default)
        => await _db.Applications.Where(a => a.OwnerId == ownerId && a.IsActive).ToListAsync(ct);

    public async Task AddAsync(Domain.Entities.Application application, CancellationToken ct = default)
        => await _db.Applications.AddAsync(application, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}

public class SodConflictRepository : ISodConflictRepository
{
    private readonly AccessDeskDbContext _db;

    public SodConflictRepository(AccessDeskDbContext db) => _db = db;

    public async Task<IEnumerable<SodConflict>> GetActiveConflictsAsync(CancellationToken ct = default)
        => await _db.SodConflicts
            .Where(s => s.IsActive)
            .Include(s => s.FirstApplication)
            .Include(s => s.SecondApplication)
            .ToListAsync(ct);

    public async Task<IEnumerable<SodConflict>> FindConflictsForRequestAsync(
        int userId, int applicationId, AccessLevel level, CancellationToken ct = default)
    {
        // Get the user's currently-approved accesses
        var currentAccesses = await _db.AccessRequests
            .Where(r => r.RequesterId == userId && r.Status == RequestStatus.Approved)
            .Select(r => new { r.ApplicationId, r.RequestedLevel })
            .ToListAsync(ct);

        // Get all active SoD rules involving the requested application+level
        var relevantRules = await _db.SodConflicts
            .Where(s => s.IsActive &&
                       ((s.FirstApplicationId == applicationId && s.FirstLevel == level) ||
                        (s.SecondApplicationId == applicationId && s.SecondLevel == level)))
            .Include(s => s.FirstApplication)
            .Include(s => s.SecondApplication)
            .ToListAsync(ct);

        // Find rules where the user already has the other side
        return relevantRules.Where(rule =>
        {
            // Determine which side is the "other" side the user might already have
            bool requestIsFirst = rule.FirstApplicationId == applicationId && rule.FirstLevel == level;

            int otherAppId = requestIsFirst ? rule.SecondApplicationId : rule.FirstApplicationId;
            AccessLevel otherLevel = requestIsFirst ? rule.SecondLevel : rule.FirstLevel;

            return currentAccesses.Any(a => a.ApplicationId == otherAppId && a.RequestedLevel == otherLevel);
        });
    }

    public async Task AddAsync(SodConflict conflict, CancellationToken ct = default)
        => await _db.SodConflicts.AddAsync(conflict, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}

public class NotificationRepository : INotificationRepository
{
    private readonly AccessDeskDbContext _db;

    public NotificationRepository(AccessDeskDbContext db) => _db = db;

    public async Task<IEnumerable<Notification>> GetUnreadByUserAsync(int userId, CancellationToken ct = default)
        => await _db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Notification>> GetByUserAsync(int userId, int page = 1, int pageSize = 20, CancellationToken ct = default)
        => await _db.Notifications
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default)
        => await _db.Notifications.CountAsync(n => n.RecipientId == userId && !n.IsRead, ct);

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
        => await _db.Notifications.AddAsync(notification, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}

public class AccessReviewRepository : IAccessReviewRepository
{
    private readonly AccessDeskDbContext _db;

    public AccessReviewRepository(AccessDeskDbContext db) => _db = db;

    public async Task<AccessReview?> GetByIdWithItemsAsync(int id, CancellationToken ct = default)
        => await _db.AccessReviews
            .Include(r => r.Items).ThenInclude(i => i.AccessRequest).ThenInclude(r => r.Requester)
            .Include(r => r.Items).ThenInclude(i => i.AccessRequest).ThenInclude(r => r.Application)
            .Include(r => r.Items).ThenInclude(i => i.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IEnumerable<AccessReview>> GetActiveReviewsAsync(CancellationToken ct = default)
        => await _db.AccessReviews
            .Where(r => r.CompletedAt == null)
            .Include(r => r.InitiatedBy)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(ct);

    public async Task<IEnumerable<AccessReviewItem>> GetPendingItemsForReviewerAsync(int reviewerId, CancellationToken ct = default)
        => await _db.AccessReviewItems
            .Where(i => i.ReviewerId == reviewerId && i.Decision == null)
            .Include(i => i.AccessReview)
            .Include(i => i.AccessRequest).ThenInclude(r => r.Requester)
            .Include(i => i.AccessRequest).ThenInclude(r => r.Application)
            .ToListAsync(ct);

    public async Task AddAsync(AccessReview review, CancellationToken ct = default)
        => await _db.AccessReviews.AddAsync(review, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
