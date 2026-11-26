using AccessDesk.Domain.Common;
using AccessDesk.Domain.Enums;

namespace AccessDesk.Domain.Entities;

/// <summary>
/// An in-app notification sent to a user when something important happens.
/// E.g., "Your access request was approved" or "You have a pending approval".
/// </summary>
public class Notification : BaseEntity
{
    public int RecipientId { get; set; }
    public ApplicationUser Recipient { get; set; } = null!;

    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Link to the entity the notification is about (e.g., /requests/42).</summary>
    public string? Link { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    public int? AccessRequestId { get; set; }
    public AccessRequest? AccessRequest { get; set; }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        Touch();
    }
}
