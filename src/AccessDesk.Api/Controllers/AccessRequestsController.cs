using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AccessDesk.Application.AccessRequests.Commands;
using AccessDesk.Application.AccessRequests.Queries;
using AccessDesk.Domain.Enums;

namespace AccessDesk.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Authorize]
public class AccessRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccessRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get all access requests submitted by the current user.</summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] RequestStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMyRequestsQuery(CurrentUserId, page, pageSize, status, search), ct);
        return Ok(result);
    }

    /// <summary>Get currently active (Approved) access grants for the current user.</summary>
    [HttpGet("my-access")]
    public async Task<IActionResult> GetMyActiveAccess([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var active = await _mediator.Send(new GetMyRequestsQuery(CurrentUserId, page, pageSize, RequestStatus.Approved), ct);
        return Ok(active);
    }

    /// <summary>Get a specific access request by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        bool isAdmin = User.IsInRole("SecurityAdmin");
        var result = await _mediator.Send(new GetRequestByIdQuery(id, CurrentUserId, isAdmin));
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>Submit a new access request.</summary>
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitRequestBody body)
    {
        var cmd = new SubmitAccessRequestCommand(
            CurrentUserId,
            body.ApplicationId,
            body.RequestedLevel,
            body.Justification,
            body.DurationDays);

        var result = await _mediator.Send(cmd);

        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage, hasSodConflict = result.HasSodConflict });

        return CreatedAtAction(nameof(GetById), new { id = result.RequestId },
            new { requestId = result.RequestId, hasSodConflict = result.HasSodConflict });
    }

    /// <summary>Approve a request (manager or owner step).</summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Manager,AppOwner")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveBody body)
    {
        bool isManager = User.IsInRole("Manager");
        var result = await _mediator.Send(new ApproveRequestCommand(id, CurrentUserId, isManager));

        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = "Request approved successfully." });
    }

    /// <summary>Reject a request with a mandatory reason.</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Manager,AppOwner")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectBody body)
    {
        bool isManager = User.IsInRole("Manager");
        var result = await _mediator.Send(new RejectRequestCommand(id, CurrentUserId, body.Reason, isManager));

        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = "Request rejected." });
    }

    /// <summary>Cancel your own pending request.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _mediator.Send(new CancelRequestCommand(id, CurrentUserId));
        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { message = "Request cancelled." });
    }

    /// <summary>Get requests pending approval (for managers and owners).</summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Manager,AppOwner")]
    public async Task<IActionResult> GetPendingApprovals([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        bool isManager = User.IsInRole("Manager");
        var result = await _mediator.Send(new GetPendingApprovalsQuery(CurrentUserId, isManager, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>SecurityAdmin resolves a request flagged for Separation of Duties review.</summary>
    [HttpPost("{id:int}/sod-resolve")]
    [Authorize(Roles = "SecurityAdmin")]
    public async Task<IActionResult> ResolveSodConflict(
        int id,
        [FromBody] ResolveSodBody body,
        [FromServices] AccessDesk.Domain.Interfaces.IAccessRequestRepository requests,
        [FromServices] AccessDesk.Domain.Interfaces.IAuditLogRepository auditLogs,
        [FromServices] AccessDesk.Domain.Interfaces.INotificationRepository notifications,
        CancellationToken ct)
    {
        var request = await requests.GetByIdWithDetailsAsync(id, ct);
        if (request is null) return NotFound();

        if (request.Status != RequestStatus.FlaggedForReview)
            return BadRequest(new { message = "Only requests flagged for SoD review can be resolved here." });

        if (body.Allow)
        {
            request.Status = RequestStatus.PendingManager;
            await requests.SaveChangesAsync(ct);

            await auditLogs.AddAsync(AccessDesk.Domain.Entities.AuditLog.Create(
                "AccessRequest.SodCleared", "AccessRequest", request.Id, CurrentUserId,
                details: $"Cleared by SecurityAdmin. Note: {body.Note}",
                oldValues: "FlaggedForReview", newValues: "PendingManager",
                accessRequestId: request.Id), ct);
        }
        else
        {
            request.Status = RequestStatus.Rejected;
            request.RejectionReason = string.IsNullOrWhiteSpace(body.Note)
                ? "Rejected by SecurityAdmin due to Separation of Duties conflict."
                : body.Note;
            await requests.SaveChangesAsync(ct);

            await auditLogs.AddAsync(AccessDesk.Domain.Entities.AuditLog.Create(
                "AccessRequest.SodRejected", "AccessRequest", request.Id, CurrentUserId,
                details: $"Rejected by SecurityAdmin: {request.RejectionReason}",
                oldValues: "FlaggedForReview", newValues: "Rejected",
                accessRequestId: request.Id), ct);

            await notifications.AddAsync(new AccessDesk.Domain.Entities.Notification
            {
                RecipientId = request.RequesterId,
                Type = NotificationType.RequestRejected,
                Message = $"Your access request for {request.Application.Name} was rejected due to a Separation of Duties conflict.",
                Link = $"/requests/{request.Id}",
                AccessRequestId = request.Id
            }, ct);
            await notifications.SaveChangesAsync(ct);
        }

        await auditLogs.SaveChangesAsync(ct);
        return Ok(new { message = body.Allow ? "Request cleared for manager approval." : "Request rejected." });
    }
}

public record SubmitRequestBody(int ApplicationId, AccessLevel RequestedLevel, string Justification, int DurationDays);
public record ApproveBody(string? Note);
public record RejectBody(string Reason);
public record ResolveSodBody(bool Allow, string? Note);
