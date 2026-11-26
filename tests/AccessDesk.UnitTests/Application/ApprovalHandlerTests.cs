using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using AccessDesk.Application.AccessRequests.Commands;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;
using AccessDesk.Domain.Interfaces;
using AppEntity = AccessDesk.Domain.Entities.Application;

namespace AccessDesk.UnitTests.Application;

public class ApprovalHandlerTests
{
    private readonly Mock<IAccessRequestRepository> _requests = new();
    private readonly Mock<IAuditLogRepository> _auditLogs = new();
    private readonly Mock<INotificationRepository> _notifications = new();

    private static AccessRequest BuildRequest(int requesterId = 10, int managerId = 20, int ownerId = 30) => new()
    {
        RequesterId = requesterId,
        Requester = new ApplicationUser { Id = requesterId, ManagerId = managerId, FirstName = "Charlie", LastName = "Dev" },
        ApplicationId = 5,
        Application = new AppEntity { Name = "GitHub", OwnerId = ownerId },
        RequestedLevel = AccessLevel.Write,
        Justification = "Need write access for sprint tasks.",
        DurationDays = 30,
        Status = RequestStatus.PendingManager
    };

    [Fact]
    public async Task Approve_WhenCallerIsNotRequesterManager_ReturnsFailure()
    {
        var req = BuildRequest(requesterId: 10, managerId: 20);
        _requests.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(req);

        var handler = new ApproveRequestHandler(_requests.Object, _auditLogs.Object, _notifications.Object, NullLogger<ApproveRequestHandler>.Instance);
        var result = await handler.Handle(new ApproveRequestCommand(1, ApproverId: 999, IsManager: true), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not the manager");
    }

    [Fact]
    public async Task Approve_WhenManagerApproves_TransitionsAndNotifiesRequester()
    {
        var req = BuildRequest(requesterId: 10, managerId: 20);
        _requests.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(req);

        var handler = new ApproveRequestHandler(_requests.Object, _auditLogs.Object, _notifications.Object, NullLogger<ApproveRequestHandler>.Instance);
        var result = await handler.Handle(new ApproveRequestCommand(1, ApproverId: 20, IsManager: true), CancellationToken.None);

        result.Success.Should().BeTrue();
        req.Status.Should().Be(RequestStatus.PendingOwner);
        _auditLogs.Verify(a => a.AddAsync(It.Is<AuditLog>(l => l.Action == "AccessRequest.ApprovedByManager"), It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Verify(n => n.AddAsync(It.Is<Notification>(x => x.RecipientId == 10), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_WhenOwnerRejectsWithReason_TransitionsAndNotifies()
    {
        var req = BuildRequest(requesterId: 10, managerId: 20, ownerId: 30);
        req.ApproveByManager(20);
        _requests.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(req);

        var handler = new RejectRequestHandler(_requests.Object, _auditLogs.Object, _notifications.Object, NullLogger<RejectRequestHandler>.Instance);
        var result = await handler.Handle(new RejectRequestCommand(1, ApproverId: 30, Reason: "Access not needed", IsManager: false), CancellationToken.None);

        result.Success.Should().BeTrue();
        req.Status.Should().Be(RequestStatus.Rejected);
        _auditLogs.Verify(a => a.AddAsync(It.Is<AuditLog>(l => l.Action == "AccessRequest.Rejected"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenCalledByDifferentUser_ReturnsFailure()
    {
        var req = BuildRequest(requesterId: 10);
        _requests.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(req);

        var handler = new CancelRequestHandler(_requests.Object, _auditLogs.Object, NullLogger<CancelRequestHandler>.Instance);
        var result = await handler.Handle(new CancelRequestCommand(1, RequesterId: 999), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("only cancel your own");
    }
}
