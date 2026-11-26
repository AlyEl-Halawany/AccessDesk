using Microsoft.EntityFrameworkCore;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;
using AccessDesk.Domain.Interfaces;

namespace AccessDesk.Infrastructure.Persistence.Repositories;

public class AccessRequestRepository : IAccessRequestRepository
{
    private readonly AccessDeskDbContext _db;

    public AccessRequestRepository(AccessDeskDbContext db)
    {
        _db = db;
    }

    public async Task<AccessRequest?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.AccessRequests.FindAsync([id], ct);

    public async Task<AccessRequest?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default)
        => await _db.AccessRequests
            .Include(r => r.Requester)
            .Include(r => r.Application).ThenInclude(a => a.Owner)
            .Include(r => r.ManagerApprover)
            .Include(r => r.OwnerApprover)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IEnumerable<AccessRequest>> GetByRequesterIdAsync(int requesterId, CancellationToken ct = default)
        => await _db.AccessRequests
            .Where(r => r.RequesterId == requesterId)
            .Include(r => r.Application)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<AccessRequest>> GetPendingForManagerAsync(int managerId, CancellationToken ct = default)
        => await _db.AccessRequests
            .Where(r => r.Status == RequestStatus.PendingManager
                     && r.Requester.ManagerId == managerId)
            .Include(r => r.Requester)
            .Include(r => r.Application)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<AccessRequest>> GetPendingForOwnerAsync(int ownerId, CancellationToken ct = default)
        => await _db.AccessRequests
            .Where(r => r.Status == RequestStatus.PendingOwner
                     && r.Application.OwnerId == ownerId)
            .Include(r => r.Requester)
            .Include(r => r.Application)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<AccessRequest>> GetApprovedByUserAndAppAsync(int userId, int applicationId, CancellationToken ct = default)
        => await _db.AccessRequests
            .Where(r => r.RequesterId == userId
                     && r.ApplicationId == applicationId
                     && r.Status == RequestStatus.Approved)
            .ToListAsync(ct);

    public async Task<IEnumerable<AccessRequest>> GetExpiredApprovedRequestsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        // We store AccessGrantedAt and DurationDays separately, so we filter in memory
        // after fetching approved requests. For production, consider a persisted ExpiresAt column.
        var approved = await _db.AccessRequests
            .Where(r => r.Status == RequestStatus.Approved && r.AccessGrantedAt != null)
            .ToListAsync(ct);

        return approved.Where(r => r.ExpiresAt.HasValue && r.ExpiresAt.Value <= now);
    }

    public async Task<IEnumerable<AccessRequest>> GetExpiringSoonAsync(int withinDays, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(withinDays);
        var approved = await _db.AccessRequests
            .Where(r => r.Status == RequestStatus.Approved && r.AccessGrantedAt != null)
            .Include(r => r.Requester)
            .Include(r => r.Application)
            .ToListAsync(ct);

        return approved.Where(r => r.ExpiresAt.HasValue
                                && r.ExpiresAt.Value > now
                                && r.ExpiresAt.Value <= cutoff);
    }

    public async Task AddAsync(AccessRequest request, CancellationToken ct = default)
        => await _db.AccessRequests.AddAsync(request, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
