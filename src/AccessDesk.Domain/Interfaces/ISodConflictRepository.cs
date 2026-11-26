using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;

namespace AccessDesk.Domain.Interfaces;

public interface ISodConflictRepository
{
    Task<IEnumerable<SodConflict>> GetActiveConflictsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Check if granting the given application/level to the user would violate
    /// any active SoD rule, given the user's currently approved accesses.
    /// </summary>
    Task<IEnumerable<SodConflict>> FindConflictsForRequestAsync(
        int userId,
        int applicationId,
        AccessLevel level,
        CancellationToken ct = default);

    Task AddAsync(SodConflict conflict, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
