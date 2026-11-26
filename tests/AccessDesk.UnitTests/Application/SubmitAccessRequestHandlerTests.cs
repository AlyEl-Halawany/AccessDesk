using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using AccessDesk.Application.AccessRequests.Commands;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;
using AccessDesk.Domain.Interfaces;
using AppEntity = AccessDesk.Domain.Entities.Application;

namespace AccessDesk.UnitTests.Application;

public class SubmitAccessRequestHandlerTests
{
    private readonly Mock<IAccessRequestRepository> _requests = new();
    private readonly Mock<IApplicationRepository> _applications = new();
    private readonly Mock<ISodConflictRepository> _sodConflicts = new();
    private readonly Mock<IAuditLogRepository> _auditLogs = new();
    private readonly Mock<INotificationRepository> _notifications = new();

    private SubmitAccessRequestHandler CreateHandler() => new(
        _requests.Object,
        _applications.Object,
        _sodConflicts.Object,
        _auditLogs.Object,
        _notifications.Object,
        NullLogger<SubmitAccessRequestHandler>.Instance);

    [Fact]
    public async Task Handle_WhenApplicationNotFound_ReturnsFailure()
    {
        _applications.Setup(a => a.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppEntity?)null);

        var cmd = new SubmitAccessRequestCommand(1, 99, AccessLevel.Read, "Valid justification for testing.", 30);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenBlockingSodConflictExists_BlocksAndLogsAudit()
    {
        _applications.Setup(a => a.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppEntity { Name = "Payroll", IsActive = true });

        _sodConflicts.Setup(s => s.FindConflictsForRequestAsync(1, 1, AccessLevel.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new SodConflict
                {
                    Description = "Cannot hold Payroll Admin and Audit Admin simultaneously",
                    IsBlocking = true
                }
            });

        var cmd = new SubmitAccessRequestCommand(1, 1, AccessLevel.Admin, "Need admin access to payroll for Q4.", 30);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.HasSodConflict.Should().BeTrue();
        _requests.Verify(r => r.AddAsync(It.IsAny<AccessRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditLogs.Verify(a => a.AddAsync(It.Is<AuditLog>(l => l.Action == "AccessRequest.BlockedBySoD"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNonBlockingSodConflictExists_FlagsRequestForReview()
    {
        _applications.Setup(a => a.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppEntity { Name = "Payroll", IsActive = true });

        _sodConflicts.Setup(s => s.FindConflictsForRequestAsync(1, 1, AccessLevel.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new SodConflict { Description = "SoD warning", IsBlocking = false }
            });

        AccessRequest? saved = null;
        _requests.Setup(r => r.AddAsync(It.IsAny<AccessRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AccessRequest, CancellationToken>((req, _) => saved = req);

        var cmd = new SubmitAccessRequestCommand(1, 1, AccessLevel.Admin, "Need admin access to payroll for Q4.", 30);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.HasSodConflict.Should().BeTrue();
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(RequestStatus.FlaggedForReview);
        saved.HasSodConflict.Should().BeTrue();
    }

    [Theory]
    [InlineData("Too short", 30, false)]
    [InlineData("Valid justification with more than twenty characters.", 0, false)]
    [InlineData("Valid justification with more than twenty characters.", 400, false)]
    [InlineData("Valid justification with more than twenty characters.", 90, true)]
    public void Validator_EnforcesJustificationAndDurationRules(string justification, int days, bool expectedValid)
    {
        var validator = new SubmitAccessRequestValidator();
        var cmd = new SubmitAccessRequestCommand(1, 1, AccessLevel.Read, justification, days);

        var result = validator.Validate(cmd);

        result.IsValid.Should().Be(expectedValid);
    }
}
