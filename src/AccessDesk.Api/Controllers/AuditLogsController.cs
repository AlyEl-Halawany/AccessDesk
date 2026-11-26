using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AccessDesk.Domain.Interfaces;

namespace AccessDesk.Api.Controllers;

/// <summary>
/// Read-only audit log search endpoint for SecurityAdmin.
/// Intentionally has no Create, Update, or Delete endpoints to preserve
/// append-only audit integrity.
/// </summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "SecurityAdmin")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _auditLogs;

    public AuditLogsController(IAuditLogRepository auditLogs)
    {
        _auditLogs = auditLogs;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] int? entityId,
        [FromQuery] int? actorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var items = await _auditLogs.SearchAsync(action, entityType, entityId, actorId, from, to, page, pageSize, ct);
        var total = await _auditLogs.CountAsync(action, entityType, entityId, actorId, from, to, ct);

        var dtos = items.Select(l => new AuditLogDto(
            l.Id,
            l.ActorId,
            l.Actor?.FullName ?? "System",
            l.Action,
            l.EntityType,
            l.EntityId,
            l.OldValues,
            l.NewValues,
            l.Details,
            l.Timestamp,
            l.AccessRequestId));

        return Ok(new
        {
            items = dtos,
            totalCount = total,
            page,
            pageSize
        });
    }
}

public record AuditLogDto(
    int Id,
    int? ActorId,
    string ActorName,
    string Action,
    string EntityType,
    int EntityId,
    string? OldValues,
    string? NewValues,
    string? Details,
    DateTime Timestamp,
    int? AccessRequestId);
