using MediatR;

namespace AccessDesk.Application.AccessRequests.Commands;

public record ApproveRequestCommand(
    int RequestId,
    int ApproverId,
    bool IsManager   // true = manager step, false = owner step
) : IRequest<ApproveRequestResult>;

public record ApproveRequestResult(bool Success, string? ErrorMessage);

public record RejectRequestCommand(
    int RequestId,
    int ApproverId,
    string Reason,
    bool IsManager
) : IRequest<RejectRequestResult>;

public record RejectRequestResult(bool Success, string? ErrorMessage);

public record CancelRequestCommand(
    int RequestId,
    int RequesterId  // The employee cancelling their own request
) : IRequest<CancelRequestResult>;

public record CancelRequestResult(bool Success, string? ErrorMessage);
