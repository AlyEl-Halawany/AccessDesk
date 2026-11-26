using Microsoft.AspNetCore.Identity;

namespace AccessDesk.Infrastructure.Persistence;

/// <summary>
/// Our custom role type. We extend IdentityRole so we can add extra fields
/// in the future (e.g., description) without a migration headache.
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
