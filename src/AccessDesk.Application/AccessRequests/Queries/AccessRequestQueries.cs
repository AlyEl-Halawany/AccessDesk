using MediatR;
using AccessDesk.Domain.Entities;

namespace AccessDesk.Application.AccessRequests.Queries;

public record GetMyRequestsQuery(int UserId) : IRequest<IEnumerable<AccessRequestDto>>;
public record GetPendingApprovalsQuery(int ApproverId, bool IsManager) : IRequest<IEnumerable<AccessRequestDto>>;
public record GetRequestByIdQuery(int RequestId, int RequestingUserId, bool IsAdmin = false) : IRequest<AccessRequestDto?>;

/// <summary>Data transfer object for access request data sent to the client.</summary>
public record AccessRequestDto(
    int Id,
    int RequesterId,
    string RequesterName,
    int ApplicationId,
    string ApplicationName,
    string RequestedLevel,
    string Justification,
    int DurationDays,
    string Status,
    DateTime CreatedAt,
    DateTime? AccessGrantedAt,
    DateTime? ExpiresAt,
    string? RejectionReason,
    string? ManagerApproverName,
    DateTime? ManagerDecisionAt,
    string? OwnerApproverName,
    DateTime? OwnerDecisionAt,
    bool HasSodConflict
);
