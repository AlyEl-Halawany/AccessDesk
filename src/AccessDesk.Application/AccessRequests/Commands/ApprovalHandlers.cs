using MediatR;
using Microsoft.Extensions.Logging;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;
using AccessDesk.Domain.Interfaces;

namespace AccessDesk.Application.AccessRequests.Commands;

public class ApproveRequestHandler : IRequestHandler<ApproveRequestCommand, ApproveRequestResult>
{
    private readonly IAccessRequestRepository _requests;
    private readonly IAuditLogRepository _auditLogs;
    private readonly INotificationRepository _notifications;
    private readonly ILogger<ApproveRequestHandler> _logger;

    public ApproveRequestHandler(
        IAccessRequestRepository requests,
        IAuditLogRepository auditLogs,
        INotificationRepository notifications,
        ILogger<ApproveRequestHandler> logger)
    {
        _requests = requests;
        _auditLogs = auditLogs;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ApproveRequestResult> Handle(ApproveRequestCommand cmd, CancellationToken ct)
    {
        var request = await _requests.GetByIdWithDetailsAsync(cmd.RequestId, ct);
        if (request is null)
            return new ApproveRequestResult(false, "Request not found.");

        // Authorization check: manager must be the requester's actual manager
        if (cmd.IsManager && request.Requester.ManagerId != cmd.ApproverId)
            return new ApproveRequestResult(false, "You are not the manager of this employee.");

        // Authorization check: owner must be the application's actual owner
        if (!cmd.IsManager && request.Application.OwnerId != cmd.ApproverId)
            return new ApproveRequestResult(false, "You are not the owner of this application.");

        string oldStatus = request.Status.ToString();

        try
        {
            if (cmd.IsManager)
                request.ApproveByManager(cmd.ApproverId);
            else
                request.ApproveByOwner(cmd.ApproverId);
        }
        catch (InvalidOperationException ex)
        {
            return new ApproveRequestResult(false, ex.Message);
        }

        await _requests.SaveChangesAsync(ct);

        string action = cmd.IsManager ? "AccessRequest.ApprovedByManager" : "AccessRequest.ApprovedByOwner";
        await _auditLogs.AddAsync(AuditLog.Create(
            action, "AccessRequest", request.Id, cmd.ApproverId,
            oldValues: oldStatus, newValues: request.Status.ToString(),
            accessRequestId: request.Id), ct);
        await _auditLogs.SaveChangesAsync(ct);

        // Notify the requester
        string notifMsg = cmd.IsManager
            ? $"Your access request for {request.Application.Name} has been approved by your manager and is now pending owner review."
            : $"Your access request for {request.Application.Name} has been fully approved. Access is now active.";

        await _notifications.AddAsync(new Notification
        {
            RecipientId = request.RequesterId,
            Type = cmd.IsManager ? NotificationType.RequestApprovedByManager : NotificationType.RequestApprovedByOwner,
            Message = notifMsg,
            Link = $"/requests/{request.Id}",
            AccessRequestId = request.Id
        }, ct);
        await _notifications.SaveChangesAsync(ct);

        _logger.LogInformation("Request {RequestId} approved by {ApproverId} (IsManager={IsManager})",
            cmd.RequestId, cmd.ApproverId, cmd.IsManager);

        return new ApproveRequestResult(true, null);
    }
}

public class RejectRequestHandler : IRequestHandler<RejectRequestCommand, RejectRequestResult>
{
    private readonly IAccessRequestRepository _requests;
    private readonly IAuditLogRepository _auditLogs;
    private readonly INotificationRepository _notifications;
    private readonly ILogger<RejectRequestHandler> _logger;

    public RejectRequestHandler(
        IAccessRequestRepository requests,
        IAuditLogRepository auditLogs,
        INotificationRepository notifications,
        ILogger<RejectRequestHandler> logger)
    {
        _requests = requests;
        _auditLogs = auditLogs;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<RejectRequestResult> Handle(RejectRequestCommand cmd, CancellationToken ct)
    {
        var request = await _requests.GetByIdWithDetailsAsync(cmd.RequestId, ct);
        if (request is null)
            return new RejectRequestResult(false, "Request not found.");

        if (cmd.IsManager && request.Requester.ManagerId != cmd.ApproverId)
            return new RejectRequestResult(false, "You are not the manager of this employee.");

        if (!cmd.IsManager && request.Application.OwnerId != cmd.ApproverId)
            return new RejectRequestResult(false, "You are not the owner of this application.");

        string oldStatus = request.Status.ToString();

        try
        {
            request.Reject(cmd.ApproverId, cmd.Reason, cmd.IsManager);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return new RejectRequestResult(false, ex.Message);
        }

        await _requests.SaveChangesAsync(ct);

        await _auditLogs.AddAsync(AuditLog.Create(
            "AccessRequest.Rejected", "AccessRequest", request.Id, cmd.ApproverId,
            details: $"Reason: {cmd.Reason}",
            oldValues: oldStatus, newValues: "Rejected",
            accessRequestId: request.Id), ct);
        await _auditLogs.SaveChangesAsync(ct);

        await _notifications.AddAsync(new Notification
        {
            RecipientId = request.RequesterId,
            Type = NotificationType.RequestRejected,
            Message = $"Your access request for {request.Application.Name} was rejected. Reason: {cmd.Reason}",
            Link = $"/requests/{request.Id}",
            AccessRequestId = request.Id
        }, ct);
        await _notifications.SaveChangesAsync(ct);

        return new RejectRequestResult(true, null);
    }
}

public class CancelRequestHandler : IRequestHandler<CancelRequestCommand, CancelRequestResult>
{
    private readonly IAccessRequestRepository _requests;
    private readonly IAuditLogRepository _auditLogs;
    private readonly ILogger<CancelRequestHandler> _logger;

    public CancelRequestHandler(IAccessRequestRepository requests, IAuditLogRepository auditLogs, ILogger<CancelRequestHandler> logger)
    {
        _requests = requests;
        _auditLogs = auditLogs;
        _logger = logger;
    }

    public async Task<CancelRequestResult> Handle(CancelRequestCommand cmd, CancellationToken ct)
    {
        var request = await _requests.GetByIdAsync(cmd.RequestId, ct);
        if (request is null)
            return new CancelRequestResult(false, "Request not found.");

        if (request.RequesterId != cmd.RequesterId)
            return new CancelRequestResult(false, "You can only cancel your own requests.");

        try { request.Cancel(); }
        catch (InvalidOperationException ex) { return new CancelRequestResult(false, ex.Message); }

        await _requests.SaveChangesAsync(ct);

        await _auditLogs.AddAsync(AuditLog.Create(
            "AccessRequest.Cancelled", "AccessRequest", request.Id, cmd.RequesterId,
            accessRequestId: request.Id), ct);
        await _auditLogs.SaveChangesAsync(ct);

        return new CancelRequestResult(true, null);
    }
}
