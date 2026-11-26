using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Interfaces;

namespace AccessDesk.Api.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationRepository _applications;
    private readonly IAuditLogRepository _auditLogs;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationsController(
        IApplicationRepository applications,
        IAuditLogRepository auditLogs,
        UserManager<ApplicationUser> userManager)
    {
        _applications = applications;
        _auditLogs = auditLogs;
        _userManager = userManager;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List all active applications in the catalog.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var apps = await _applications.GetAllActiveAsync(ct);
        var result = apps.Select(a => new ApplicationDto(
            a.Id,
            a.Name,
            a.Description,
            a.OwnerId,
            a.Owner?.FullName ?? "Unknown",
            a.IsActive,
            a.CreatedAt));

        return Ok(result);
    }

    /// <summary>Get details for a single application.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var app = await _applications.GetByIdAsync(id, ct);
        if (app is null) return NotFound();

        return Ok(new ApplicationDto(
            app.Id,
            app.Name,
            app.Description,
            app.OwnerId,
            app.Owner?.FullName ?? "Unknown",
            app.IsActive,
            app.CreatedAt));
    }

    /// <summary>Create a new application in the catalog (SecurityAdmin only).</summary>
    [HttpPost]
    [Authorize(Roles = "SecurityAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateApplicationBody body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { message = "Application name is required." });

        var owner = await _userManager.FindByIdAsync(body.OwnerId.ToString());
        if (owner is null || !owner.IsActive)
            return BadRequest(new { message = "Assigned owner not found or inactive." });

        var app = new Domain.Entities.Application
        {
            Name = body.Name.Trim(),
            Description = body.Description?.Trim() ?? string.Empty,
            OwnerId = body.OwnerId,
            IsActive = true
        };

        await _applications.AddAsync(app, ct);
        await _applications.SaveChangesAsync(ct);

        await _auditLogs.AddAsync(AuditLog.Create(
            "Application.Created",
            "Application",
            app.Id,
            CurrentUserId,
            details: $"Created application '{app.Name}' with owner {owner.FullName}."), ct);
        await _auditLogs.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = app.Id },
            new ApplicationDto(app.Id, app.Name, app.Description, app.OwnerId, owner.FullName, app.IsActive, app.CreatedAt));
    }

    /// <summary>Update an existing application's metadata or owner (SecurityAdmin only).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SecurityAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateApplicationBody body, CancellationToken ct)
    {
        var app = await _applications.GetByIdAsync(id, ct);
        if (app is null) return NotFound();

        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { message = "Application name is required." });

        var owner = await _userManager.FindByIdAsync(body.OwnerId.ToString());
        if (owner is null || !owner.IsActive)
            return BadRequest(new { message = "Assigned owner not found or inactive." });

        var oldSummary = $"{app.Name} (OwnerId={app.OwnerId}, Active={app.IsActive})";

        app.Name = body.Name.Trim();
        app.Description = body.Description?.Trim() ?? string.Empty;
        app.OwnerId = body.OwnerId;
        app.IsActive = body.IsActive;

        await _applications.SaveChangesAsync(ct);

        var newSummary = $"{app.Name} (OwnerId={app.OwnerId}, Active={app.IsActive})";
        await _auditLogs.AddAsync(AuditLog.Create(
            "Application.Updated",
            "Application",
            app.Id,
            CurrentUserId,
            oldValues: oldSummary,
            newValues: newSummary), ct);
        await _auditLogs.SaveChangesAsync(ct);

        return Ok(new ApplicationDto(app.Id, app.Name, app.Description, app.OwnerId, owner.FullName, app.IsActive, app.CreatedAt));
    }
}

public record ApplicationDto(
    int Id,
    string Name,
    string Description,
    int OwnerId,
    string OwnerName,
    bool IsActive,
    DateTime CreatedAt);

public record CreateApplicationBody(string Name, string? Description, int OwnerId);
public record UpdateApplicationBody(string Name, string? Description, int OwnerId, bool IsActive);
