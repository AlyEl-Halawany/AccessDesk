using AccessDesk.Domain.Entities;

namespace AccessDesk.Domain.Interfaces;

public interface IAuditLogRepository
{
    /// <summary>Add a new audit log entry. This is the ONLY write operation allowed.</summary>
    Task AddAsync(AuditLog log, CancellationToken ct = default);

    Task<IEnumerable<AuditLog>> SearchAsync(
        string? action = null,
        string? entityType = null,
        int? entityId = null,
        int? actorId = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<int> CountAsync(
        string? action = null,
        string? entityType = null,
        int? entityId = null,
        int? actorId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
