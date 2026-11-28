using Microsoft.EntityFrameworkCore;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;
using AccessDesk.Infrastructure.Persistence;

namespace AccessDesk.IntegrationTests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ACCESSDESK_SQLSERVER_CONNECTION")))
            Skip = "Set ACCESSDESK_SQLSERVER_CONNECTION to run SQL Server integration tests.";
    }
}

public class SqlServerConcurrencyTests
{
    [SqlServerFact]
    public async Task ConcurrentManagerApprovals_SecondSaveFailsWithConcurrencyException()
    {
        var connectionString = Environment.GetEnvironmentVariable("ACCESSDESK_SQLSERVER_CONNECTION")!;
        var options = new DbContextOptionsBuilder<AccessDeskDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var setup = new AccessDeskDbContext(options);
        await setup.Database.MigrateAsync();
        var template = await setup.AccessRequests
            .Include(r => r.Requester)
            .OrderBy(r => r.Id)
            .FirstAsync();
        var request = new AccessRequest
        {
            RequesterId = template.RequesterId,
            ApplicationId = template.ApplicationId,
            RequestedLevel = AccessLevel.Read,
            Justification = $"Concurrency test {Guid.NewGuid():N}",
            DurationDays = 3,
            Status = RequestStatus.PendingManager
        };
        setup.AccessRequests.Add(request);
        await setup.SaveChangesAsync();
        var requestId = request.Id;
        var managerId = template.Requester.ManagerId!.Value;

        try
        {
            using var first = new AccessDeskDbContext(options);
            using var second = new AccessDeskDbContext(options);
            var firstCopy = await first.AccessRequests.SingleAsync(r => r.Id == requestId);
            var secondCopy = await second.AccessRequests.SingleAsync(r => r.Id == requestId);

            firstCopy.ApproveByManager(managerId);
            secondCopy.ApproveByManager(managerId);
            await first.SaveChangesAsync();

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        }
        finally
        {
            using var cleanup = new AccessDeskDbContext(options);
            var testRequest = await cleanup.AccessRequests.FindAsync(requestId);
            if (testRequest is not null)
            {
                cleanup.AccessRequests.Remove(testRequest);
                await cleanup.SaveChangesAsync();
            }
        }
    }
}
