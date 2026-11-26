using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;
using AccessDesk.Domain.Interfaces;

namespace AccessDesk.Api.Controllers;

[ApiController]
[Route("api/sod-conflicts")]
[Authorize(Roles = "SecurityAdmin")]
public class SodConflictsController : ControllerBase
{
    private readonly ISodConflictRepository _conflicts;
    private readonly IApplicationRepository _applications;
    private readonly IAuditLogRepository _auditLogs;

    public SodConflictsController(
        ISodConflictRepository conflicts,
        IApplicationRepository applications,
        IAuditLogRepository auditLogs)
    {
        _conflicts = conflicts;
        _applications = applications;
        _auditLogs = auditLogs;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>List all active Separation of Duties conflict rules.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var rules = await _conflicts.GetActiveConflictsAsync(ct);
        var result = rules.Select(r => new SodConflictDto(
            r.Id,
            r.FirstApplicationId,
            r.FirstApplication?.Name ?? "Unknown",
            r.FirstLevel.ToString(),
            r.SecondApplicationId,
            r.SecondApplication?.Name ?? "Unknown",
            r.SecondLevel.ToString(),
            r.Description,
            r.IsBlocking,
            r.IsActive));

        return Ok(result);
    }

    /// <summary>Create a new Separation of Duties conflict rule.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSodConflictBody body, CancellationToken ct)
    {
        var app1 = await _applications.GetByIdAsync(body.FirstApplicationId, ct);
        var app2 = await _applications.GetByIdAsync(body.SecondApplicationId, ct);

        if (app1 is null || app2 is null)
            return BadRequest(new { message = "Both applications must exist." });

        if (body.FirstApplicationId == body.SecondApplicationId && body.FirstLevel == body.SecondLevel)
            return BadRequest(new { message = "A conflict rule must pair two distinct application/level combinations." });

        if (string.IsNullOrWhiteSpace(body.Description))
            return BadRequest(new { message = "Description is required." });

        var rule = new SodConflict
        {
            FirstApplicationId = body.FirstApplicationId,
            FirstLevel = body.FirstLevel,
            SecondApplicationId = body.SecondApplicationId,
            SecondLevel = body.SecondLevel,
            Description = body.Description.Trim(),
            IsBlocking = body.IsBlocking,
            IsActive = true
        };

        await _conflicts.AddAsync(rule, ct);
        await _conflicts.SaveChangesAsync(ct);

        await _auditLogs.AddAsync(AuditLog.Create(
            "SodConflict.Created",
            "SodConflict",
            rule.Id,
            CurrentUserId,
            details: $"{app1.Name} ({rule.FirstLevel}) <-> {app2.Name} ({rule.SecondLevel}), Blocking={rule.IsBlocking}"), ct);
        await _auditLogs.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), new { id = rule.Id }, new { id = rule.Id });
    }
}

public record SodConflictDto(
    int Id,
    int FirstApplicationId,
    string FirstApplicationName,
    string FirstLevel,
    int SecondApplicationId,
    string SecondApplicationName,
    string SecondLevel,
    string Description,
    bool IsBlocking,
    bool IsActive);

public record CreateSodConflictBody(
    int FirstApplicationId,
    AccessLevel FirstLevel,
    int SecondApplicationId,
    AccessLevel SecondLevel,
    string Description,
    bool IsBlocking);
