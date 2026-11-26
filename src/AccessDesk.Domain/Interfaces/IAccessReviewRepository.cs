using AccessDesk.Domain.Entities;

namespace AccessDesk.Domain.Interfaces;

public interface IAccessReviewRepository
{
    Task<AccessReview?> GetByIdWithItemsAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<AccessReview>> GetActiveReviewsAsync(CancellationToken ct = default);
    Task<IEnumerable<AccessReviewItem>> GetPendingItemsForReviewerAsync(int reviewerId, CancellationToken ct = default);
    Task AddAsync(AccessReview review, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
