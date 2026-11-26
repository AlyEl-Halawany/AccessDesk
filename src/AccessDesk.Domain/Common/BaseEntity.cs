namespace AccessDesk.Domain.Common;

/// <summary>
/// Base class for all domain entities. Every entity has an integer primary key
/// and tracks when it was created and last updated — useful for auditing and debugging.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>Call this whenever you change something on the entity so the timestamp stays accurate.</summary>
    protected void Touch() => UpdatedAt = DateTime.UtcNow;
}
