using AccessDesk.Domain.Common;

namespace AccessDesk.Domain.Entities;

/// <summary>
/// A periodic review campaign where managers confirm whether their team members
/// still need their current access. If a manager does not respond by the deadline,
/// the access is flagged as overdue.
/// </summary>
public class AccessReview : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>The SecurityAdmin who started this review campaign.</summary>
    public int InitiatedById { get; set; }
    public ApplicationUser InitiatedBy { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    public bool IsCompleted => CompletedAt is not null;
    public bool IsOverdue => !IsCompleted && DateTime.UtcNow > DueDate;

    public ICollection<AccessReviewItem> Items { get; set; } = new List<AccessReviewItem>();
}

/// <summary>
/// One line item in an access review: a manager decides whether an employee
/// should keep a specific approved access request.
/// </summary>
public class AccessReviewItem : BaseEntity
{
    public int AccessReviewId { get; set; }
    public AccessReview AccessReview { get; set; } = null!;

    /// <summary>The approved access request under review.</summary>
    public int AccessRequestId { get; set; }
    public AccessRequest AccessRequest { get; set; } = null!;

    /// <summary>The manager who must respond to this item.</summary>
    public int ReviewerId { get; set; }
    public ApplicationUser Reviewer { get; set; } = null!;

    public ReviewDecision? Decision { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? DecidedAt { get; set; }

    public bool IsOverdue(DateTime dueDate) => Decision is null && DateTime.UtcNow > dueDate;

    public void Confirm(string? note = null)
    {
        Decision = ReviewDecision.Confirmed;
        ReviewNote = note;
        DecidedAt = DateTime.UtcNow;
        Touch();
    }

    public void Revoke(string? note = null)
    {
        Decision = ReviewDecision.Revoked;
        ReviewNote = note;
        DecidedAt = DateTime.UtcNow;
        Touch();
    }
}

public enum ReviewDecision
{
    Confirmed, // Manager says: yes, this person still needs this access
    Revoked    // Manager says: revoke this access
}
