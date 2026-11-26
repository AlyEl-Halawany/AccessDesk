using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccessDesk.Domain.Interfaces;
using AccessDesk.Infrastructure.Persistence;

namespace AccessDesk.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notifications;
    private readonly AccessDeskDbContext _db;

    public NotificationsController(INotificationRepository notifications, AccessDeskDbContext db)
    {
        _notifications = notifications;
        _db = db;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var items = await _notifications.GetByUserAsync(CurrentUserId, page, pageSize, ct);
        var unreadCount = await _notifications.GetUnreadCountAsync(CurrentUserId, ct);

        return Ok(new
        {
            unreadCount,
            items = items.Select(n => new
            {
                n.Id,
                Type = n.Type.ToString(),
                n.Message,
                n.Link,
                n.IsRead,
                n.CreatedAt,
                n.ReadAt
            })
        });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var count = await _notifications.GetUnreadCountAsync(CurrentUserId, ct);
        return Ok(new { unreadCount = count });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken ct)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientId == CurrentUserId, ct);

        if (notification is null) return NotFound();

        notification.MarkAsRead();
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Marked as read." });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var unread = await _db.Notifications
            .Where(n => n.RecipientId == CurrentUserId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread)
            n.MarkAsRead();

        await _db.SaveChangesAsync(ct);
        return Ok(new { markedCount = unread.Count });
    }
}
