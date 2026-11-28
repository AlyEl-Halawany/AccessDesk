using AccessDesk.Domain.Common;
using AccessDesk.Domain.Enums;

namespace AccessDesk.Domain.Entities;

/// <summary>
/// An employee's request to gain access to a specific application at a specific level.
///
/// The approval workflow is:
///   1. Employee submits → status = PendingManager
///   2. Manager approves → status = PendingOwner
///   3. App owner approves → status = Approved, access is granted
///   4. Either rejects → status = Rejected
///   5. Employee cancels → status = Cancelled
///   6. Background service detects expiry → status = Expired
/// </summary>
public class AccessRequest : BaseEntity
{
    /// <summary>SQL Server-generated optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public int RequesterId { get; set; }
    public ApplicationUser Requester { get; set; } = null!;

    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public AccessLevel RequestedLevel { get; set; }

    /// <summary>Why the employee needs this access. Required and must be meaningful.</summary>
    public string Justification { get; set; } = string.Empty;

    /// <summary>How many days the access should remain active after being approved.</summary>
    public int DurationDays { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.PendingManager;

    /// <summary>Set when the owner approves. Access expires after DurationDays from this date.</summary>
    public DateTime? AccessGrantedAt { get; set; }

    /// <summary>Computed expiry date. Only meaningful when status is Approved.</summary>
    public DateTime? ExpiresAt => AccessGrantedAt?.AddDays(DurationDays);

    // Approval tracking
    public int? ManagerApproverId { get; set; }
    public ApplicationUser? ManagerApprover { get; set; }
    public DateTime? ManagerDecisionAt { get; set; }

    public int? OwnerApproverId { get; set; }
    public ApplicationUser? OwnerApprover { get; set; }
    public DateTime? OwnerDecisionAt { get; set; }

    /// <summary>The reason given when rejecting a request. Required on rejection.</summary>
    public string? RejectionReason { get; set; }

    public bool HasSodConflict { get; set; } = false;

    // Navigation
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // --- Domain methods (business logic lives here, not in services) ---

    /// <summary>
    /// Manager approves the request. Moves it to PendingOwner state.
    /// Throws if the request is not in the right state.
    /// </summary>
    public void ApproveByManager(int managerId)
    {
        if (Status != RequestStatus.PendingManager)
            throw new InvalidOperationException($"Cannot approve a request with status '{Status}'.");

        ManagerApproverId = managerId;
        ManagerDecisionAt = DateTime.UtcNow;
        Status = RequestStatus.PendingOwner;
        Touch();
    }

    /// <summary>
    /// App owner approves the request. Moves it to Approved and sets the access start time.
    /// </summary>
    public void ApproveByOwner(int ownerId)
    {
        if (Status != RequestStatus.PendingOwner)
            throw new InvalidOperationException($"Cannot approve a request with status '{Status}'.");

        OwnerApproverId = ownerId;
        OwnerDecisionAt = DateTime.UtcNow;
        AccessGrantedAt = DateTime.UtcNow;
        Status = RequestStatus.Approved;
        Touch();
    }

    /// <summary>Reject at any active approval step. A reason is mandatory.</summary>
    public void Reject(int approverId, string reason, bool isManager)
    {
        if (Status != RequestStatus.PendingManager && Status != RequestStatus.PendingOwner)
            throw new InvalidOperationException($"Cannot reject a request with status '{Status}'.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A rejection reason is required.", nameof(reason));

        RejectionReason = reason;
        Status = RequestStatus.Rejected;

        if (isManager)
        {
            ManagerApproverId = approverId;
            ManagerDecisionAt = DateTime.UtcNow;
        }
        else
        {
            OwnerApproverId = approverId;
            OwnerDecisionAt = DateTime.UtcNow;
        }

        Touch();
    }

    /// <summary>Employee cancels their own request.</summary>
    public void Cancel()
    {
        if (Status == RequestStatus.Approved || Status == RequestStatus.Rejected
            || Status == RequestStatus.Cancelled || Status == RequestStatus.Expired)
            throw new InvalidOperationException($"Cannot cancel a request with status '{Status}'.");

        Status = RequestStatus.Cancelled;
        Touch();
    }

    /// <summary>Background service calls this when the access duration elapses.</summary>
    public void Expire()
    {
        if (Status != RequestStatus.Approved)
            throw new InvalidOperationException("Only approved requests can expire.");

        Status = RequestStatus.Expired;
        Touch();
    }

    /// <summary>Flag the request because a separation-of-duties conflict was detected.</summary>
    public void FlagForSodReview()
    {
        HasSodConflict = true;
        Status = RequestStatus.FlaggedForReview;
        Touch();
    }
}
