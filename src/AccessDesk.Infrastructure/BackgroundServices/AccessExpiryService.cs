using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Enums;
using AccessDesk.Domain.Interfaces;

namespace AccessDesk.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that runs periodically to expire access requests that have
/// passed their duration. It uses IHostedService (registered as a hosted service
/// in Program.cs) which keeps running for the lifetime of the application.
///
/// Why IHostedService instead of a cron job?
/// For simple periodic tasks in a .NET app, a hosted service is the idiomatic choice.
/// It stays in-process (no extra infrastructure), is easy to test, and ties neatly
/// into the application's dependency injection and cancellation tokens.
/// </summary>
public class AccessExpiryService : BackgroundService
{
    // IServiceScopeFactory lets us create a new DI scope each time we run,
    // so we don't hold a long-lived DbContext (which would cause stale data).
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccessExpiryService> _logger;

    // How often to check. 1 hour is a sensible default; can be configurable.
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public AccessExpiryService(IServiceScopeFactory scopeFactory, ILogger<AccessExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AccessExpiryService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await ExpireAccessAsync(stoppingToken);

            // Wait until the next check interval, but stop waiting if the app is shutting down
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ExpireAccessAsync(CancellationToken ct)
    {
        // Create a fresh scope so each run gets a fresh DbContext
        using var scope = _scopeFactory.CreateScope();
        var requestRepo = scope.ServiceProvider.GetRequiredService<IAccessRequestRepository>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var notifRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        try
        {
            var expiredRequests = (await requestRepo.GetExpiredApprovedRequestsAsync(ct)).ToList();

            foreach (var request in expiredRequests)
            {
                _logger.LogInformation("Expiring access request {RequestId} for user {UserId}", request.Id, request.RequesterId);

                request.Expire();
                await requestRepo.SaveChangesAsync(ct);

                await auditRepo.AddAsync(AuditLog.Create(
                    "AccessRequest.Expired", "AccessRequest", request.Id,
                    actorId: null, // null = system action
                    details: $"Access expired after {request.DurationDays} days.",
                    accessRequestId: request.Id), ct);
                await auditRepo.SaveChangesAsync(ct);

                // Notify the user their access was revoked
                await notifRepo.AddAsync(new Notification
                {
                    RecipientId = request.RequesterId,
                    Type = NotificationType.AccessExpired,
                    Message = $"Your access to {request.Application?.Name ?? "an application"} has expired and been revoked.",
                    Link = $"/requests/{request.Id}",
                    AccessRequestId = request.Id
                }, ct);
                await notifRepo.SaveChangesAsync(ct);
            }

            if (expiredRequests.Count > 0)
                _logger.LogInformation("Expired {Count} access requests", expiredRequests.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during access expiry check");
        }
    }
}
