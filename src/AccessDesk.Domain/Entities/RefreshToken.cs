using AccessDesk.Domain.Common;

namespace AccessDesk.Domain.Entities;

/// <summary>
/// Stores a refresh token issued to a user. Refresh tokens are long-lived,
/// stored in the database (so we can revoke them), and each one maps to exactly one user.
/// When a refresh token is used, it is replaced by a new one (rotation).
/// </summary>
public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>SHA-256 hash of the opaque token sent to the client.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
