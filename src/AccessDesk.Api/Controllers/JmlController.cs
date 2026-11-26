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

/// <summary>
/// Simulates HR Joiner / Mover / Leaver lifecycle events.
/// In production, these endpoints would be triggered by an HRIS webhook (Workday, BambooHR).
/// </summary>
[ApiController]
[Route("api/jml")]
[Authorize(Roles = "SecurityAdmin")]
public class JmlController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AccessDeskDbContext _db;
    private readonly IAuditLogRepository _auditLogs;
    private readonly INotificationRepository _notifications;

    public JmlController(
        UserManager<ApplicationUser> userManager,
        AccessDeskDbContext db,
        IAuditLogRepository auditLogs,
        INotificationRepository notifications)
    {
        _userManager = userManager;
        _db = db;
        _auditLogs = auditLogs;
        _notifications = notifications;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Simulate a Joiner event: onboard a new employee with a manager and role.</summary>
    [HttpPost("joiner")]
    public async Task<IActionResult> Joiner([FromBody] JoinerBody body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.FirstName) || string.IsNullOrWhiteSpace(body.LastName))
            return BadRequest(new { message = "Email, FirstName, and LastName are required." });

        var existing = await _userManager.FindByEmailAsync(body.Email);
        if (existing is not null)
            return BadRequest(new { message = "A user with this email already exists." });

        var user = new ApplicationUser
        {
            UserName = body.Email.Trim(),
            Email = body.Email.Trim(),
            FirstName = body.FirstName.Trim(),
            LastName = body.LastName.Trim(),
            ManagerId = body.ManagerId,
            EmailConfirmed = true
        };

        var password = string.IsNullOrWhiteSpace(body.InitialPassword) ? "Welcome@123!" : body.InitialPassword;
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        await _userManager.AddToRoleAsync(user, "Employee");
        if (!string.IsNullOrWhiteSpace(body.Role) && body.Role != "Employee")
            await _userManager.AddToRoleAsync(user, body.Role);

        await _auditLogs.AddAsync(AuditLog.Create(
            "JML.Joiner",
            "ApplicationUser",
            user.Id,
            CurrentUserId,
            details: $"Onboarded {user.FullName} ({user.Email}) with ManagerId={body.ManagerId}"), ct);
        await _auditLogs.SaveChangesAsync(ct);

        return Ok(new { userId = user.Id, user.Email, user.FullName, message = "Joiner onboarded." });
    }

    /// <summary>Simulate a Mover event: change manager and trigger a review of all existing access.</summary>
    [HttpPost("mover")]
    public async Task<IActionResult> Mover([FromBody] MoverBody body, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(body.UserId.ToString());
        if (user is null || !user.IsActive)
            return NotFound(new { message = "Active user not found." });

        var newManager = await _userManager.FindByIdAsync(body.NewManagerId.ToString());
        if (newManager is null || !newManager.IsActive)
            return BadRequest(new { message = "New manager not found or inactive." });

        var oldManagerId = user.ManagerId;
        user.ManagerId = body.NewManagerId;
        await _userManager.UpdateAsync(user);

        // Trigger a targeted access review for the mover's existing approved access
        var activeRequests = await _db.AccessRequests
            .Where(r => r.RequesterId == user.Id && r.Status == RequestStatus.Approved)
            .ToListAsync(ct);

        int reviewId = 0;
        if (activeRequests.Count > 0)
        {
            var review = new AccessReview
            {
                Name = $"Mover Review: {user.FullName} ({DateTime.UtcNow:yyyy-MM-dd})",
                InitiatedById = CurrentUserId,
                StartDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            foreach (var req in activeRequests)
            {
                review.Items.Add(new AccessReviewItem
                {
                    AccessRequestId = req.Id,
                    ReviewerId = newManager.Id
                });
            }

            _db.AccessReviews.Add(review);
            await _db.SaveChangesAsync(ct);
            reviewId = review.Id;

            await _notifications.AddAsync(new Notification
            {
                RecipientId = newManager.Id,
                Type = NotificationType.ReviewRequired,
                Message = $"{user.FullName} transferred to your team. Please review their {activeRequests.Count} existing access grant(s).",
                Link = "/reviews"
            }, ct);
            await _notifications.SaveChangesAsync(ct);
        }

        await _auditLogs.AddAsync(AuditLog.Create(
            "JML.Mover",
            "ApplicationUser",
            user.Id,
            CurrentUserId,
            oldValues: $"ManagerId={oldManagerId}",
            newValues: $"ManagerId={body.NewManagerId}",
            details: $"Triggered mover review (ReviewId={reviewId}, Items={activeRequests.Count})."), ct);
        await _auditLogs.SaveChangesAsync(ct);

        return Ok(new
        {
            userId = user.Id,
            newManagerId = body.NewManagerId,
            reviewId,
            itemsFlagged = activeRequests.Count,
            message = "Mover processed and access review triggered."
        });
    }

    /// <summary>Simulate a Leaver event: deactivate user, revoke all active access and refresh tokens immediately.</summary>
    [HttpPost("leaver")]
    public async Task<IActionResult> Leaver([FromBody] LeaverBody body, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(body.UserId.ToString());
        if (user is null)
            return NotFound(new { message = "User not found." });

        if (!user.IsActive)
            return BadRequest(new { message = "User is already deactivated." });

        user.DeactivatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Immediately expire all active approved access and cancel pending requests
        var requests = await _db.AccessRequests
            .Where(r => r.RequesterId == user.Id &&
                        (r.Status == RequestStatus.Approved ||
                         r.Status == RequestStatus.PendingManager ||
                         r.Status == RequestStatus.PendingOwner ||
                         r.Status == RequestStatus.FlaggedForReview))
            .ToListAsync(ct);

        int revokedCount = 0;
        foreach (var req in requests)
        {
            if (req.Status == RequestStatus.Approved)
            {
                req.Expire();
                revokedCount++;
            }
            else
            {
                req.Cancel();
            }
        }

        // Revoke all active refresh tokens
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _auditLogs.AddAsync(AuditLog.Create(
            "JML.Leaver",
            "ApplicationUser",
            user.Id,
            CurrentUserId,
            oldValues: "IsActive=true",
            newValues: "IsActive=false",
            details: $"Offboarded {user.FullName}. Revoked {revokedCount} active access grant(s) and {tokens.Count} refresh token(s)."), ct);
        await _auditLogs.SaveChangesAsync(ct);

        return Ok(new
        {
            userId = user.Id,
            revokedAccessCount = revokedCount,
            revokedTokensCount = tokens.Count,
            message = "Leaver processed; all access and tokens revoked immediately."
        });
    }

    /// <summary>List all users for SecurityAdmin JML management.</summary>
    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(CancellationToken ct)
    {
        var users = await _db.Users
            .Include(u => u.Manager)
            .OrderBy(u => u.FirstName)
            .ToListAsync(ct);

        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.ManagerId,
                ManagerName = u.Manager?.FullName,
                u.IsActive,
                u.DeactivatedAt,
                Roles = roles
            });
        }

        return Ok(result);
    }
}

public record JoinerBody(string FirstName, string LastName, string Email, int? ManagerId, string? Role, string? InitialPassword);
public record MoverBody(int UserId, int NewManagerId);
public record LeaverBody(int UserId);
