using AccessDesk.Domain.Common;
using AccessDesk.Domain.Enums;

namespace AccessDesk.Domain.Entities;

/// <summary>
/// Represents a company application that employees can request access to.
/// Examples: Salesforce, GitHub, Jira, AWS Console.
/// </summary>
public class Application : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>The user who is responsible for approving/rejecting access to this app.</summary>
    public int OwnerId { get; set; }
    public ApplicationUser Owner { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<AccessRequest> AccessRequests { get; set; } = new List<AccessRequest>();
    public ICollection<SodConflict> SodConflictsAsFirst { get; set; } = new List<SodConflict>();
    public ICollection<SodConflict> SodConflictsAsSecond { get; set; } = new List<SodConflict>();
}
