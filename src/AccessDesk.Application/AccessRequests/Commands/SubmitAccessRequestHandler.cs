using MediatR;
using Microsoft.Extensions.Logging;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;
using AccessDesk.Domain.Interfaces;

namespace AccessDesk.Application.AccessRequests.Commands;

/// <summary>
/// Handles SubmitAccessRequestCommand.
/// 
/// Steps:
///   1. Validate the application exists and is active.
///   2. Check for active SoD conflicts.
///   3. If blocking conflict → reject immediately.
///   4. Create the request and persist it.
///   5. If non-blocking conflict → flag for SecurityAdmin review.
///   6. Write an audit log entry.
///   7. Send a notification to the requester's manager.
/// </summary>
public class SubmitAccessRequestHandler : IRequestHandler<SubmitAccessRequestCommand, SubmitAccessRequestResult>
{
    private readonly IAccessRequestRepository _requests;
    private readonly IApplicationRepository _applications;
    private readonly ISodConflictRepository _sodConflicts;
    private readonly IAuditLogRepository _auditLogs;
    private readonly INotificationRepository _notifications;
    private readonly ILogger<SubmitAccessRequestHandler> _logger;

    public SubmitAccessRequestHandler(
        IAccessRequestRepository requests,
        IApplicationRepository applications,
        ISodConflictRepository sodConflicts,
        IAuditLogRepository auditLogs,
        INotificationRepository notifications,
        ILogger<SubmitAccessRequestHandler> logger)
    {
        _requests = requests;
        _applications = applications;
        _sodConflicts = sodConflicts;
        _auditLogs = auditLogs;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<SubmitAccessRequestResult> Handle(
        SubmitAccessRequestCommand cmd, CancellationToken ct)
    {
        // 1. Verify the application exists
        var app = await _applications.GetByIdAsync(cmd.ApplicationId, ct);
        if (app is null || !app.IsActive)
            return new SubmitAccessRequestResult(false, null, "Application not found or inactive.");

        // 2. Check for SoD conflicts
        var conflicts = (await _sodConflicts.FindConflictsForRequestAsync(
            cmd.RequesterId, cmd.ApplicationId, cmd.RequestedLevel, ct)).ToList();

        // 3. If any conflict is blocking, reject immediately
        if (conflicts.Any(c => c.IsBlocking))
        {
            var conflictNames = string.Join(", ", conflicts.Select(c => c.Description));
            _logger.LogWarning("SoD blocking conflict for user {UserId}: {Conflicts}", cmd.RequesterId, conflictNames);

            await _auditLogs.AddAsync(AuditLog.Create(
                "AccessRequest.BlockedBySoD", "AccessRequest", 0, cmd.RequesterId,
                details: $"Blocked by SoD: {conflictNames}"), ct);
            await _auditLogs.SaveChangesAsync(ct);

            return new SubmitAccessRequestResult(false, null,
                $"This request violates separation of duties rules: {conflictNames}", HasSodConflict: true);
        }

        // 4. Create and persist the request
        var request = new AccessRequest
        {
            RequesterId = cmd.RequesterId,
            ApplicationId = cmd.ApplicationId,
            RequestedLevel = cmd.RequestedLevel,
            Justification = cmd.Justification,
            DurationDays = cmd.DurationDays,
            Status = RequestStatus.PendingManager
        };

        // 5. If non-blocking conflict, flag after creating
        if (conflicts.Any())
        {
            request.FlagForSodReview();
        }

        await _requests.AddAsync(request, ct);
        await _requests.SaveChangesAsync(ct);

        // 6. Audit log
        await _auditLogs.AddAsync(AuditLog.Create(
            "AccessRequest.Submitted", "AccessRequest", request.Id,
            actorId: cmd.RequesterId,
            details: $"Requested {cmd.RequestedLevel} access to '{app.Name}' for {cmd.DurationDays} days.",
            accessRequestId: request.Id), ct);
        await _auditLogs.SaveChangesAsync(ct);

        _logger.LogInformation("Access request {RequestId} submitted by user {UserId}", request.Id, cmd.RequesterId);

        return new SubmitAccessRequestResult(true, request.Id, null, HasSodConflict: conflicts.Any());
    }
}
