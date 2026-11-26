using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;
using AccessDesk.Domain.Interfaces;
using AccessDesk.Infrastructure.Persistence;

namespace AccessDesk.Api.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly AccessDeskDbContext _db;
    private readonly IAccessReviewRepository _reviews;
    private readonly IAuditLogRepository _auditLogs;
    private readonly INotificationRepository _notifications;
    private readonly IEmailService _email;

    public ReviewsController(
        AccessDeskDbContext db,
        IAccessReviewRepository reviews,
        IAuditLogRepository auditLogs,
        INotificationRepository notifications,
        IEmailService email)
    {
        _db = db;
        _reviews = reviews;
        _auditLogs = auditLogs;
        _notifications = notifications;
        _email = email;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List all access review campaigns (SecurityAdmin).</summary>
    [HttpGet]
    [Authorize(Roles = "SecurityAdmin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var campaigns = await _db.AccessReviews
            .Include(r => r.InitiatedBy)
            .Include(r => r.Items)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(ct);

        var result = campaigns.Select(c => new
        {
            c.Id,
            c.Name,
            InitiatedByName = c.InitiatedBy?.FullName ?? "System",
            c.StartDate,
            c.DueDate,
            c.CompletedAt,
            c.IsCompleted,
            c.IsOverdue,
            TotalItems = c.Items.Count,
            PendingItems = c.Items.Count(i => i.Decision is null),
            ConfirmedItems = c.Items.Count(i => i.Decision == ReviewDecision.Confirmed),
            RevokedItems = c.Items.Count(i => i.Decision == ReviewDecision.Revoked)
        });

        return Ok(result);
    }

    /// <summary>Get pending review items assigned to the current manager.</summary>
    [HttpGet("my-pending")]
    [Authorize(Roles = "Manager,SecurityAdmin")]
    public async Task<IActionResult> GetMyPending(CancellationToken ct)
    {
        var items = await _reviews.GetPendingItemsForReviewerAsync(CurrentUserId, ct);

        var result = items.Select(i => new
        {
            i.Id,
            i.AccessReviewId,
            CampaignName = i.AccessReview.Name,
            DueDate = i.AccessReview.DueDate,
            IsOverdue = i.IsOverdue(i.AccessReview.DueDate),
            i.AccessRequestId,
            EmployeeName = i.AccessRequest.Requester.FullName,
            EmployeeEmail = i.AccessRequest.Requester.Email,
            ApplicationName = i.AccessRequest.Application.Name,
            RequestedLevel = i.AccessRequest.RequestedLevel.ToString(),
            AccessGrantedAt = i.AccessRequest.AccessGrantedAt,
            ExpiresAt = i.AccessRequest.ExpiresAt
        });

        return Ok(result);
    }

    /// <summary>Start a new access review campaign for all currently active approved access (SecurityAdmin).</summary>
    [HttpPost]
    [Authorize(Roles = "SecurityAdmin")]
    public async Task<IActionResult> StartCampaign([FromBody] StartReviewBody body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { message = "Campaign name is required." });

        int dueInDays = body.DueInDays is > 0 and <= 90 ? body.DueInDays : 14;

        var activeAccess = await _db.AccessRequests
            .Include(r => r.Requester)
            .Include(r => r.Application)
            .Where(r => r.Status == RequestStatus.Approved && r.Requester.ManagerId != null)
            .ToListAsync(ct);

        var review = new AccessReview
        {
            Name = body.Name.Trim(),
            InitiatedById = CurrentUserId,
            StartDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(dueInDays)
        };

        foreach (var req in activeAccess)
        {
            review.Items.Add(new AccessReviewItem
            {
                AccessRequestId = req.Id,
                ReviewerId = req.Requester.ManagerId!.Value
            });
        }

        await _reviews.AddAsync(review, ct);
        await _reviews.SaveChangesAsync(ct);

        await _auditLogs.AddAsync(AuditLog.Create(
            "AccessReview.Started",
            "AccessReview",
            review.Id,
            CurrentUserId,
            details: $"Started campaign '{review.Name}' with {review.Items.Count} review items."), ct);
        await _auditLogs.SaveChangesAsync(ct);

        // Notify each distinct reviewer
        var reviewerIds = review.Items.Select(i => i.ReviewerId).Distinct().ToList();
        var reviewers = await _db.Users.Where(u => reviewerIds.Contains(u.Id)).ToListAsync(ct);

        foreach (var reviewer in reviewers)
        {
            int count = review.Items.Count(i => i.ReviewerId == reviewer.Id);
            await _notifications.AddAsync(new Notification
            {
                RecipientId = reviewer.Id,
                Type = NotificationType.ReviewRequired,
                Message = $"Access review '{review.Name}' requires your decision on {count} team access item(s) by {review.DueDate:yyyy-MM-dd}.",
                Link = "/reviews"
            }, ct);

            if (!string.IsNullOrEmpty(reviewer.Email))
            {
                await _email.SendAsync(
                    reviewer.Email,
                    reviewer.FullName,
                    $"Access Review Required: {review.Name}",
                    $"<p>Hello {reviewer.FirstName},</p><p>You have {count} access item(s) to review in <strong>{review.Name}</strong> by {review.DueDate:yyyy-MM-dd}.</p>",
                    ct);
            }
        }
        await _notifications.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), new { id = review.Id }, new
        {
            review.Id,
            review.Name,
            ItemsCreated = review.Items.Count,
            review.DueDate
        });
    }

    /// <summary>Manager confirms or revokes a team member's access item.</summary>
    [HttpPost("items/{itemId:int}/decide")]
    [Authorize(Roles = "Manager,SecurityAdmin")]
    public async Task<IActionResult> DecideItem(int itemId, [FromBody] DecideReviewItemBody body, CancellationToken ct)
    {
        var item = await _db.AccessReviewItems
            .Include(i => i.AccessReview)
            .Include(i => i.AccessRequest).ThenInclude(r => r.Application)
            .Include(i => i.AccessRequest).ThenInclude(r => r.Requester)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item is null) return NotFound();

        if (item.ReviewerId != CurrentUserId && !User.IsInRole("SecurityAdmin"))
            return Forbid();

        if (item.Decision is not null)
            return BadRequest(new { message = "This review item has already been decided." });

        if (body.Confirm)
        {
            item.Confirm(body.Note);
        }
        else
        {
            item.Revoke(body.Note);
            // Immediately revoke the underlying approved access
            if (item.AccessRequest.Status == RequestStatus.Approved)
                item.AccessRequest.Expire();

            await _notifications.AddAsync(new Notification
            {
                RecipientId = item.AccessRequest.RequesterId,
                Type = NotificationType.AccessExpired,
                Message = $"Your access to {item.AccessRequest.Application.Name} was revoked during access review '{item.AccessReview.Name}'.",
                Link = "/my-access",
                AccessRequestId = item.AccessRequestId
            }, ct);
        }

        // If all items in the campaign are now decided, mark campaign completed
        var remaining = await _db.AccessReviewItems
            .CountAsync(i => i.AccessReviewId == item.AccessReviewId && i.Id != itemId && i.Decision == null, ct);

        if (remaining == 0)
            item.AccessReview.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _auditLogs.AddAsync(AuditLog.Create(
            body.Confirm ? "AccessReviewItem.Confirmed" : "AccessReviewItem.Revoked",
            "AccessReviewItem",
            item.Id,
            CurrentUserId,
            details: $"Review '{item.AccessReview.Name}' for {item.AccessRequest.Requester.FullName} on {item.AccessRequest.Application.Name}. Note: {body.Note}",
            accessRequestId: item.AccessRequestId), ct);
        await _auditLogs.SaveChangesAsync(ct);

        return Ok(new { message = body.Confirm ? "Access confirmed." : "Access revoked." });
    }
}

public record StartReviewBody(string Name, int DueInDays = 14);
public record DecideReviewItemBody(bool Confirm, string? Note);
