using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;

namespace AccessDesk.Domain.Interfaces;

/// <summary>
/// Repository contract for AccessRequest entities.
/// These interfaces live in Domain so Application layer can depend on them
/// without knowing about EF Core or SQL Server.
/// </summary>
public interface IAccessRequestRepository
{
    Task<AccessRequest?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<AccessRequest?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<AccessRequest>> GetByRequesterIdAsync(int requesterId, int page = 1, int pageSize = 20, RequestStatus? status = null, string? search = null, CancellationToken ct = default);
    Task<int> CountByRequesterIdAsync(int requesterId, RequestStatus? status = null, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<AccessRequest>> GetPendingForManagerAsync(int managerId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<int> CountPendingForManagerAsync(int managerId, CancellationToken ct = default);
    Task<IEnumerable<AccessRequest>> GetPendingForOwnerAsync(int ownerId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<int> CountPendingForOwnerAsync(int ownerId, CancellationToken ct = default);
    Task<IEnumerable<AccessRequest>> GetApprovedByUserAndAppAsync(int userId, int applicationId, CancellationToken ct = default);
    Task<IEnumerable<AccessRequest>> GetExpiredApprovedRequestsAsync(CancellationToken ct = default);
    Task<IEnumerable<AccessRequest>> GetExpiringSoonAsync(int withinDays, CancellationToken ct = default);
    Task AddAsync(AccessRequest request, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
