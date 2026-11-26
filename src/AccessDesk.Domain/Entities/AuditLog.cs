using AccessDesk.Domain.Common;

namespace AccessDesk.Domain.Entities;

/// <summary>
/// An append-only record of every important action in the system.
/// We NEVER update or delete audit log entries — that would defeat the purpose of auditing.
///
/// This is enforced at two levels:
///   1. No Update/Delete endpoints in the API.
///   2. The EF Core configuration marks this entity as owned (insert-only); 
///      the repository only exposes an Add method.
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>Who performed the action. Null means a system/background process did it.</summary>
    public int? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }

    /// <summary>The name of the action, e.g. "AccessRequest.Approved", "User.Deactivated".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>The type of entity that was changed, e.g. "AccessRequest".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>The ID of the entity that was changed.</summary>
    public int EntityId { get; set; }

    /// <summary>JSON snapshot of the entity state before the change.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of the entity state after the change.</summary>
    public string? NewValues { get; set; }

    /// <summary>Extra context, e.g. a rejection reason or IP address.</summary>
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Optional: link to the specific access request being acted on
    public int? AccessRequestId { get; set; }
    public AccessRequest? AccessRequest { get; set; }

    /// <summary>Factory method so callers don't forget any required fields.</summary>
    public static AuditLog Create(
        string action,
        string entityType,
        int entityId,
        int? actorId = null,
        string? details = null,
        string? oldValues = null,
        string? newValues = null,
        int? accessRequestId = null)
    {
        return new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ActorId = actorId,
            Details = details,
            OldValues = oldValues,
            NewValues = newValues,
            AccessRequestId = accessRequestId,
            Timestamp = DateTime.UtcNow
        };
    }
}
