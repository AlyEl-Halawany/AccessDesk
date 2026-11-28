using AccessDesk.Domain.Entities;

namespace AccessDesk.Domain.Interfaces;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetUnreadByUserAsync(int userId, CancellationToken ct = default);
    Task<IEnumerable<Notification>> GetByUserAsync(int userId, int page = 1, int pageSize = 20, bool unreadOnly = false, CancellationToken ct = default);
    Task<int> CountByUserAsync(int userId, bool unreadOnly = false, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
