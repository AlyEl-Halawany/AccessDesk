using FluentAssertions;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;

namespace AccessDesk.UnitTests.Domain;

public class AccessRequestStateMachineTests
{
    private static AccessRequest CreatePendingRequest() => new()
    {
        RequesterId = 1,
        ApplicationId = 10,
        RequestedLevel = AccessLevel.Write,
        Justification = "Need write access to deploy hotfixes to the production repository.",
        DurationDays = 30,
        Status = RequestStatus.PendingManager
    };

    [Fact]
    public void ApproveByManager_WhenPendingManager_TransitionsToPendingOwner()
    {
        var request = CreatePendingRequest();

        request.ApproveByManager(managerId: 2);

        request.Status.Should().Be(RequestStatus.PendingOwner);
        request.ManagerApproverId.Should().Be(2);
        request.ManagerDecisionAt.Should().NotBeNull();
    }

    [Fact]
    public void ApproveByManager_WhenAlreadyApproved_ThrowsInvalidOperationException()
    {
        var request = CreatePendingRequest();
        request.ApproveByManager(2);
        request.ApproveByOwner(3);

        var act = () => request.ApproveByManager(2);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApproveByOwner_WhenPendingOwner_TransitionsToApprovedAndSetsDates()
    {
        var request = CreatePendingRequest();
        request.ApproveByManager(2);

        request.ApproveByOwner(ownerId: 3);

        request.Status.Should().Be(RequestStatus.Approved);
        request.OwnerApproverId.Should().Be(3);
        request.AccessGrantedAt.Should().NotBeNull();
        request.ExpiresAt.Should().BeCloseTo(
            request.AccessGrantedAt!.Value.AddDays(30),
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ApproveByOwner_WhenStillPendingManager_ThrowsInvalidOperationException()
    {
        var request = CreatePendingRequest();

        var act = () => request.ApproveByOwner(ownerId: 3);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_WithoutReason_ThrowsArgumentException()
    {
        var request = CreatePendingRequest();

        var act = () => request.Reject(approverId: 2, reason: "   ", isManager: true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reject_WithValidReason_TransitionsToRejected()
    {
        var request = CreatePendingRequest();

        request.Reject(approverId: 2, reason: "Insufficient business justification", isManager: true);

        request.Status.Should().Be(RequestStatus.Rejected);
        request.RejectionReason.Should().Be("Insufficient business justification");
        request.ManagerApproverId.Should().Be(2);
    }

    [Fact]
    public void Cancel_WhenPending_TransitionsToCancelled()
    {
        var request = CreatePendingRequest();

        request.Cancel();

        request.Status.Should().Be(RequestStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenApproved_ThrowsInvalidOperationException()
    {
        var request = CreatePendingRequest();
        request.ApproveByManager(2);
        request.ApproveByOwner(3);

        var act = () => request.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Expire_WhenApproved_TransitionsToExpired()
    {
        var request = CreatePendingRequest();
        request.ApproveByManager(2);
        request.ApproveByOwner(3);

        request.Expire();

        request.Status.Should().Be(RequestStatus.Expired);
    }

    [Fact]
    public void Expire_WhenPending_ThrowsInvalidOperationException()
    {
        var request = CreatePendingRequest();

        var act = () => request.Expire();

        act.Should().Throw<InvalidOperationException>();
    }
}
