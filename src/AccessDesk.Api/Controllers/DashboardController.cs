using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccessDesk.Domain.Enums;
using AccessDesk.Infrastructure.Persistence;

namespace AccessDesk.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AccessDeskDbContext _db;

    public DashboardController(AccessDeskDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        bool isAdmin = User.IsInRole("SecurityAdmin");
        bool isManager = User.IsInRole("Manager");
        bool isOwner = User.IsInRole("AppOwner");

        // Pending approvals relevant to current user (or all for SecurityAdmin)
        int pendingApprovals = await _db.AccessRequests
            .CountAsync(r =>
                (isAdmin && (r.Status == RequestStatus.PendingManager || r.Status == RequestStatus.PendingOwner || r.Status == RequestStatus.FlaggedForReview)) ||
                (isManager && r.Status == RequestStatus.PendingManager && r.Requester.ManagerId == CurrentUserId) ||
                (isOwner && r.Status == RequestStatus.PendingOwner && r.Application.OwnerId == CurrentUserId), ct);

        // Approved requests expiring within 7 days
        var approvedRequests = await _db.AccessRequests
            .Include(r => r.Application)
            .Include(r => r.Requester)
            .Where(r => r.Status == RequestStatus.Approved && r.AccessGrantedAt != null)
            .ToListAsync(ct);

        var expiringSoonList = approvedRequests
            .Where(r => (isAdmin || r.RequesterId == CurrentUserId) &&
                        r.ExpiresAt.HasValue &&
                        r.ExpiresAt.Value > now &&
                        r.ExpiresAt.Value <= now.AddDays(7))
            .Select(r => new
            {
                r.Id,
                RequesterName = r.Requester.FullName,
                ApplicationName = r.Application.Name,
                Level = r.RequestedLevel.ToString(),
                ExpiresAt = r.ExpiresAt
            })
            .ToList();

        // Overdue review campaigns & items
        int overdueReviews = await _db.AccessReviews
            .CountAsync(r => r.CompletedAt == null && r.DueDate < now, ct);

        // Requests per application (for chart)
        var requestsPerApp = await _db.AccessRequests
            .GroupBy(r => r.Application.Name)
            .Select(g => new
            {
                applicationName = g.Key,
                total = g.Count(),
                approved = g.Count(x => x.Status == RequestStatus.Approved),
                pending = g.Count(x => x.Status == RequestStatus.PendingManager || x.Status == RequestStatus.PendingOwner),
                rejected = g.Count(x => x.Status == RequestStatus.Rejected)
            })
            .ToListAsync(ct);

        // Average approval times in hours (manager & owner)
        var decidedRequests = await _db.AccessRequests
            .Where(r => r.ManagerDecisionAt != null || r.OwnerDecisionAt != null)
            .Select(r => new
            {
                r.CreatedAt,
                r.ManagerDecisionAt,
                r.OwnerDecisionAt
            })
            .ToListAsync(ct);

        var managerHours = decidedRequests
            .Where(r => r.ManagerDecisionAt.HasValue)
            .Select(r => (r.ManagerDecisionAt!.Value - r.CreatedAt).TotalHours)
            .ToList();

        var ownerHours = decidedRequests
            .Where(r => r.OwnerDecisionAt.HasValue && r.ManagerDecisionAt.HasValue)
            .Select(r => (r.OwnerDecisionAt!.Value - r.ManagerDecisionAt!.Value).TotalHours)
            .ToList();

        var approvalTimes = new[]
        {
            new { stage = "Manager Review", avgHours = managerHours.Count > 0 ? Math.Round(managerHours.Average(), 1) : 0 },
            new { stage = "Owner Review", avgHours = ownerHours.Count > 0 ? Math.Round(ownerHours.Average(), 1) : 0 }
        };

        int myActiveAccessCount = approvedRequests.Count(r => r.RequesterId == CurrentUserId);
        int myPendingRequestsCount = await _db.AccessRequests
            .CountAsync(r => r.RequesterId == CurrentUserId &&
                             (r.Status == RequestStatus.PendingManager || r.Status == RequestStatus.PendingOwner), ct);

        return Ok(new
        {
            pendingApprovals,
            expiringSoonCount = expiringSoonList.Count,
            expiringSoon = expiringSoonList,
            overdueReviews,
            myActiveAccessCount,
            myPendingRequestsCount,
            requestsPerApp,
            approvalTimes
        });
    }
}
