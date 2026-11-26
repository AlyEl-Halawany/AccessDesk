using AccessDesk.Domain.Entities;

namespace AccessDesk.Domain.Interfaces;

public interface IApplicationRepository
{
    Task<Application?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Application>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IEnumerable<Application>> GetByOwnerIdAsync(int ownerId, CancellationToken ct = default);
    Task AddAsync(Application application, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
