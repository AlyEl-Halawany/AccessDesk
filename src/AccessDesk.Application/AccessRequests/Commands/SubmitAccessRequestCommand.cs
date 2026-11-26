using MediatR;
using AccessDesk.Domain.Enums;

namespace AccessDesk.Application.AccessRequests.Commands;

/// <summary>
/// Command to submit a new access request.
/// MediatR routes this to SubmitAccessRequestHandler.
/// </summary>
public record SubmitAccessRequestCommand(
    int RequesterId,
    int ApplicationId,
    AccessLevel RequestedLevel,
    string Justification,
    int DurationDays
) : IRequest<SubmitAccessRequestResult>;

public record SubmitAccessRequestResult(
    bool Success,
    int? RequestId,
    string? ErrorMessage,
    bool HasSodConflict = false
);
