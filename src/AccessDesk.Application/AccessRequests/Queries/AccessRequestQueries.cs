using MediatR;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;

namespace AccessDesk.Application.AccessRequests.Queries;

public record GetMyRequestsQuery(int UserId, int Page = 1, int PageSize = 20, RequestStatus? Status = null, string? Search = null) : IRequest<PagedResult<AccessRequestDto>>;
public record GetPendingApprovalsQuery(int ApproverId, bool IsManager, int Page = 1, int PageSize = 20) : IRequest<PagedResult<AccessRequestDto>>;
public record GetRequestByIdQuery(int RequestId, int RequestingUserId, bool IsAdmin = false) : IRequest<AccessRequestDto?>;
public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);

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
