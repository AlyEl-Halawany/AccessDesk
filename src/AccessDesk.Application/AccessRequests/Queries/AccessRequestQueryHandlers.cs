using MediatR;
using AccessDesk.Domain.Enums;
using AccessDesk.Domain.Interfaces;

namespace AccessDesk.Application.AccessRequests.Queries;

public class AccessRequestQueryHandlers :
    IRequestHandler<GetMyRequestsQuery, PagedResult<AccessRequestDto>>,
    IRequestHandler<GetPendingApprovalsQuery, PagedResult<AccessRequestDto>>,
    IRequestHandler<GetRequestByIdQuery, AccessRequestDto?>
{
    private readonly IAccessRequestRepository _requests;

    public AccessRequestQueryHandlers(IAccessRequestRepository requests)
    {
        _requests = requests;
    }

    public async Task<PagedResult<AccessRequestDto>> Handle(GetMyRequestsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var requests = await _requests.GetByRequesterIdAsync(q.UserId, page, pageSize, q.Status, q.Search, ct);
        var total = await _requests.CountByRequesterIdAsync(q.UserId, q.Status, q.Search, ct);
        return new PagedResult<AccessRequestDto>(requests.Select(ToDto), total, page, pageSize);
    }

    public async Task<PagedResult<AccessRequestDto>> Handle(GetPendingApprovalsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        IEnumerable<Domain.Entities.AccessRequest> requests;
        int total;

        if (q.IsManager)
        {
            requests = await _requests.GetPendingForManagerAsync(q.ApproverId, page, pageSize, ct);
            total = await _requests.CountPendingForManagerAsync(q.ApproverId, ct);
        }
        else
        {
            requests = await _requests.GetPendingForOwnerAsync(q.ApproverId, page, pageSize, ct);
            total = await _requests.CountPendingForOwnerAsync(q.ApproverId, ct);
        }

        return new PagedResult<AccessRequestDto>(requests.Select(ToDto), total, page, pageSize);
    }

    public async Task<AccessRequestDto?> Handle(GetRequestByIdQuery q, CancellationToken ct)
    {
        var request = await _requests.GetByIdWithDetailsAsync(q.RequestId, ct);
        if (request is null) return null;

        // Non-admins can only see their own requests or requests they are approving
        if (!q.IsAdmin && request.RequesterId != q.RequestingUserId
            && request.Requester?.ManagerId != q.RequestingUserId
            && request.Application?.OwnerId != q.RequestingUserId)
            return null;

        return ToDto(request);
    }

    private static AccessRequestDto ToDto(Domain.Entities.AccessRequest r) => new(
        r.Id,
        r.RequesterId,
        r.Requester?.FullName ?? "Unknown",
        r.ApplicationId,
        r.Application?.Name ?? "Unknown",
        r.RequestedLevel.ToString(),
        r.Justification,
        r.DurationDays,
        r.Status.ToString(),
        r.CreatedAt,
        r.AccessGrantedAt,
        r.ExpiresAt,
        r.RejectionReason,
        r.ManagerApprover?.FullName,
        r.ManagerDecisionAt,
        r.OwnerApprover?.FullName,
        r.OwnerDecisionAt,
        r.HasSodConflict
    );
}
