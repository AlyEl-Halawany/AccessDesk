using MediatR;
using AccessDesk.Domain.Enums;
using AccessDesk.Domain.Interfaces;

namespace AccessDesk.Application.AccessRequests.Queries;

public class AccessRequestQueryHandlers :
    IRequestHandler<GetMyRequestsQuery, IEnumerable<AccessRequestDto>>,
    IRequestHandler<GetPendingApprovalsQuery, IEnumerable<AccessRequestDto>>,
    IRequestHandler<GetRequestByIdQuery, AccessRequestDto?>
{
    private readonly IAccessRequestRepository _requests;

    public AccessRequestQueryHandlers(IAccessRequestRepository requests)
    {
        _requests = requests;
    }

    public async Task<IEnumerable<AccessRequestDto>> Handle(GetMyRequestsQuery q, CancellationToken ct)
    {
        var requests = await _requests.GetByRequesterIdAsync(q.UserId, ct);
        return requests.Select(ToDto);
    }

    public async Task<IEnumerable<AccessRequestDto>> Handle(GetPendingApprovalsQuery q, CancellationToken ct)
    {
        IEnumerable<Domain.Entities.AccessRequest> requests;

        if (q.IsManager)
            requests = await _requests.GetPendingForManagerAsync(q.ApproverId, ct);
        else
            requests = await _requests.GetPendingForOwnerAsync(q.ApproverId, ct);

        return requests.Select(ToDto);
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
