using Microsoft.AspNetCore.Identity;
using AccessDesk.Domain.Common;
using AccessDesk.Domain.Enums;

namespace AccessDesk.Domain.Entities;

/// <summary>
/// Extends ASP.NET Core Identity's IdentityUser with our application-specific fields.
/// We use Identity for password hashing and authentication plumbing,
/// then add our own fields on top.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>The employee's direct manager. Null for top-level managers.</summary>
    public int? ManagerId { get; set; }
    public ApplicationUser? Manager { get; set; }

    /// <summary>When the user was deactivated (leaver event). Null = active.</summary>
    public DateTime? DeactivatedAt { get; set; }
    public bool IsActive => DeactivatedAt is null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ApplicationUser> DirectReports { get; set; } = new List<ApplicationUser>();
    public ICollection<AccessRequest> SubmittedRequests { get; set; } = new List<AccessRequest>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public string FullName => $"{FirstName} {LastName}";
}
