using AccessDesk.Domain.Common;
using AccessDesk.Domain.Enums;

namespace AccessDesk.Domain.Entities;

/// <summary>
/// A conflict rule between two application/access-level combinations.
/// When an employee tries to get access B while already having access A,
/// the request is blocked or flagged, depending on configuration.
///
/// Example: Having "Payroll Admin" and "Financial Audit Admin" is a separation-of-duties violation
/// because the same person should not be able to both run payroll and audit it.
/// </summary>
public class SodConflict : BaseEntity
{
    public int FirstApplicationId { get; set; }
    public Application FirstApplication { get; set; } = null!;
    public AccessLevel FirstLevel { get; set; }

    public int SecondApplicationId { get; set; }
    public Application SecondApplication { get; set; } = null!;
    public AccessLevel SecondLevel { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// If true, the conflicting request is auto-rejected.
    /// If false, it is flagged for SecurityAdmin review.
    /// </summary>
    public bool IsBlocking { get; set; } = false;

    public bool IsActive { get; set; } = true;
}
