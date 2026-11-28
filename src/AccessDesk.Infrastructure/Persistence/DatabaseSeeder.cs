using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;
using AccessDesk.Infrastructure.Persistence;

namespace AccessDesk.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with demo data on first run.
/// This runs at application startup and is idempotent (safe to run multiple times).
///
/// IMPORTANT: These are demo accounts with known passwords for development/demo use only.
/// The passwords are hashed before storage using ASP.NET Core Identity's PBKDF2 hasher.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessDeskDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await SeedRolesAsync(roleManager, logger);
        var users = await SeedUsersAsync(userManager, logger);
        await SeedApplicationsAsync(db, users, logger);
        await SeedSodConflictsAsync(db, logger);
        await SeedSampleRequestsAsync(db, users, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        string[] roles = ["Employee", "Manager", "AppOwner", "SecurityAdmin"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    private static async Task<Dictionary<string, ApplicationUser>> SeedUsersAsync(
        UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var users = new Dictionary<string, ApplicationUser>();

        // Define demo users. Passwords intentionally simple for demo — documented here.
        var demoUsers = new[]
        {
            new { Email = "admin@accessdesk.dev",   First = "Security", Last = "Admin",   Role = "SecurityAdmin", Manager = (string?)null, Password = "Admin@123!" },
            new { Email = "alice@accessdesk.dev",   First = "Alice",    Last = "Manager", Role = "Manager",       Manager = (string?)"admin@accessdesk.dev", Password = "Manager@123!" },
            new { Email = "bob@accessdesk.dev",     First = "Bob",      Last = "Owner",   Role = "AppOwner",      Manager = (string?)"admin@accessdesk.dev", Password = "Owner@123!" },
            new { Email = "charlie@accessdesk.dev", First = "Charlie",  Last = "Dev",     Role = "Employee",      Manager = (string?)"alice@accessdesk.dev", Password = "Employee@123!" },
            new { Email = "diana@accessdesk.dev",   First = "Diana",    Last = "Analyst", Role = "Employee",      Manager = (string?)"alice@accessdesk.dev", Password = "Employee@123!" },
            new { Email = "evan@accessdesk.dev",    First = "Evan",     Last = "Support", Role = "Employee",      Manager = (string?)"alice@accessdesk.dev", Password = "Employee@123!" },
        };

        // First pass: create all users without manager references
        foreach (var u in demoUsers)
        {
            var existing = await userManager.FindByEmailAsync(u.Email);
            if (existing is not null)
            {
                users[u.Email] = existing;
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = u.Email,
                Email = u.Email,
                FirstName = u.First,
                LastName = u.Last,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, u.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, u.Role);
                // All demo users are also Employees
                if (u.Role != "Employee")
                    await userManager.AddToRoleAsync(user, "Employee");

                users[u.Email] = user;
                logger.LogInformation("Created demo user: {Email} ({Role})", u.Email, u.Role);
            }
            else
            {
                logger.LogWarning("Failed to create user {Email}: {Errors}", u.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // Second pass: set manager references now that all users exist
        foreach (var u in demoUsers.Where(x => x.Manager != null))
        {
            if (users.TryGetValue(u.Email, out var user) && users.TryGetValue(u.Manager!, out var manager))
            {
                if (user.ManagerId != manager.Id)
                {
                    user.ManagerId = manager.Id;
                    await userManager.UpdateAsync(user);
                }
            }
        }

        return users;
    }

    private static async Task SeedApplicationsAsync(
        AccessDeskDbContext db, Dictionary<string, ApplicationUser> users, ILogger logger)
    {
        if (await db.Applications.AnyAsync()) return;

        var owner = users["bob@accessdesk.dev"];

        var apps = new[]
        {
            new Domain.Entities.Application { Name = "GitHub Enterprise", Description = "Source code management and CI/CD platform.", OwnerId = owner.Id },
            new Domain.Entities.Application { Name = "Jira", Description = "Project tracking and issue management.", OwnerId = owner.Id },
            new Domain.Entities.Application { Name = "AWS Console", Description = "Amazon Web Services cloud management console.", OwnerId = owner.Id },
            new Domain.Entities.Application { Name = "Salesforce CRM", Description = "Customer relationship management system.", OwnerId = owner.Id },
            new Domain.Entities.Application { Name = "Payroll System", Description = "HR payroll processing platform.", OwnerId = owner.Id },
            new Domain.Entities.Application { Name = "Financial Audit Tool", Description = "Internal financial auditing and reporting.", OwnerId = owner.Id },
        };

        db.Applications.AddRange(apps);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} applications", apps.Length);
    }

    private static async Task SeedSodConflictsAsync(AccessDeskDbContext db, ILogger logger)
    {
        if (await db.SodConflicts.AnyAsync()) return;

        var payroll = await db.Applications.FirstOrDefaultAsync(a => a.Name == "Payroll System");
        var audit = await db.Applications.FirstOrDefaultAsync(a => a.Name == "Financial Audit Tool");

        if (payroll is null || audit is null) return;

        // Classic SoD: you cannot both run payroll AND audit the financials
        db.SodConflicts.Add(new SodConflict
        {
            FirstApplicationId = payroll.Id,
            FirstLevel = AccessLevel.Admin,
            SecondApplicationId = audit.Id,
            SecondLevel = AccessLevel.Admin,
            Description = "Payroll Admin and Financial Audit Admin access cannot be held by the same person.",
            IsBlocking = false // Flag for review, not auto-block
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded SoD conflict rules");
    }

    private static async Task SeedSampleRequestsAsync(
        AccessDeskDbContext db, Dictionary<string, ApplicationUser> users, ILogger logger)
    {
        if (await db.AccessRequests.AnyAsync()) return;

        var github = await db.Applications.FirstOrDefaultAsync(a => a.Name == "GitHub Enterprise");
        var jira = await db.Applications.FirstOrDefaultAsync(a => a.Name == "Jira");
        if (github is null || jira is null) return;

        var charlie = users["charlie@accessdesk.dev"];
        var alice = users["alice@accessdesk.dev"];
        var bob = users["bob@accessdesk.dev"];

        // Sample: a pending request waiting for manager approval
        db.AccessRequests.Add(new AccessRequest
        {
            RequesterId = charlie.Id,
            ApplicationId = github.Id,
            RequestedLevel = AccessLevel.Write,
            Justification = "Need write access to contribute to the main repository for the Q4 feature branch.",
            DurationDays = 90,
            Status = RequestStatus.PendingManager
        });

        // Sample: an approved request
        var approved = new AccessRequest
        {
            RequesterId = charlie.Id,
            ApplicationId = jira.Id,
            RequestedLevel = AccessLevel.Read,
            Justification = "Need to track sprint progress for my team's tasks.",
            DurationDays = 365,
            Status = RequestStatus.Approved,
            ManagerApproverId = alice.Id,
            OwnerApproverId = bob.Id,
        };
        approved.ManagerDecisionAt = approved.CreatedAt.AddHours(2);
        approved.OwnerDecisionAt = approved.CreatedAt.AddDays(1);
        approved.AccessGrantedAt = approved.CreatedAt.AddDays(1);
        db.AccessRequests.Add(approved);

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded sample access requests");
    }
}
