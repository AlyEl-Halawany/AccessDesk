using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using AccessDesk.Domain.Entities;
using AccessDesk.Infrastructure.Persistence;

namespace AccessDesk.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<AccessDeskWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AccessDeskWebApplicationFactory _factory;

    public ApiIntegrationTests(AccessDeskWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("accessToken").GetString()!;
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task Login_WithValidDemoCredentials_ReturnsJwtAndUserInfo()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "charlie@accessdesk.dev",
            password = "Employee@123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("user").GetProperty("email").GetString().Should().Be("charlie@accessdesk.dev");
    }

    [Fact]
    public async Task ProtectedEndpoints_WithoutToken_Return401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/requests/my");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuditLogs_AsEmployee_Returns403Forbidden()
    {
        var token = await LoginAsync("charlie@accessdesk.dev", "Employee@123!");
        SetBearer(token);

        var response = await _client.GetAsync("/api/audit-logs");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FullAccessRequestWorkflow_SubmitManagerApproveOwnerApprove_SucceedsAndAudits()
    {
        // 1. Employee submits request
        var empToken = await LoginAsync("charlie@accessdesk.dev", "Employee@123!");
        SetBearer(empToken);

        var submitResp = await _client.PostAsJsonAsync("/api/requests", new
        {
            applicationId = 3, // AWS Console
            requestedLevel = "Read",
            justification = "Need read access to AWS Console to inspect CloudWatch logs.",
            durationDays = 30
        });
        submitResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var submitJson = await submitResp.Content.ReadFromJsonAsync<JsonElement>();
        int requestId = submitJson.GetProperty("requestId").GetInt32();

        // 2. Manager approves
        var mgrToken = await LoginAsync("alice@accessdesk.dev", "Manager@123!");
        SetBearer(mgrToken);

        var mgrApprove = await _client.PostAsJsonAsync($"/api/requests/{requestId}/approve", new { note = "Approved" });
        mgrApprove.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. AppOwner approves
        var ownerToken = await LoginAsync("bob@accessdesk.dev", "Owner@123!");
        SetBearer(ownerToken);

        var ownerApprove = await _client.PostAsJsonAsync($"/api/requests/{requestId}/approve", new { note = "Granted" });
        ownerApprove.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Verify SecurityAdmin sees audit trail
        var adminToken = await LoginAsync("admin@accessdesk.dev", "Admin@123!");
        SetBearer(adminToken);

        var auditResp = await _client.GetAsync($"/api/audit-logs?entityId={requestId}");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var auditJson = await auditResp.Content.ReadFromJsonAsync<JsonElement>();
        auditJson.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Dashboard_AsAuthenticatedUser_ReturnsMetrics()
    {
        var token = await LoginAsync("admin@accessdesk.dev", "Admin@123!");
        SetBearer(token);

        var resp = await _client.GetAsync("/api/dashboard");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        json.TryGetProperty("requestsPerApp", out _).Should().BeTrue();
        json.TryGetProperty("approvalTimes", out _).Should().BeTrue();
    }

    [Fact]
    public async Task AuditLog_SaveChangesRejectsModificationAndDeletion()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessDeskDbContext>();
        var entry = AuditLog.Create("Test.AppendOnly", "Test", 1, details: "original");
        db.AuditLogs.Add(entry);
        await db.SaveChangesAsync();

        entry.Details = "modified";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());

        db.Entry(entry).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
        db.AuditLogs.Remove(entry);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task RefreshTokens_AreHashedAndReuseRevokesActiveTokens()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "charlie@accessdesk.dev",
            password = "Employee@123!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var rawRefreshToken = login.GetProperty("refreshToken").GetString()!;

        int userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AccessDeskDbContext>();
            userId = (await db.Users.SingleAsync(u => u.Email == "charlie@accessdesk.dev")).Id;
            var stored = await db.RefreshTokens.Where(t => t.UserId == userId).OrderByDescending(t => t.Id).FirstAsync();
            stored.TokenHash.Should().Be(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken))));
            stored.TokenHash.Should().NotBe(rawRefreshToken);
        }

        var rotatedResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = rawRefreshToken });
        rotatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await rotatedResponse.Content.ReadFromJsonAsync<JsonElement>();
        var rotatedRefreshToken = rotated.GetProperty("refreshToken").GetString()!;
        rotatedRefreshToken.Should().NotBe(rawRefreshToken);

        var replayResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = rawRefreshToken });
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AccessDeskDbContext>();
        (await verificationDb.RefreshTokens.CountAsync(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow))
            .Should().Be(0);
    }

    [Fact]
    public async Task HealthAndValidationResponses_UseSecurityHeadersAndProblemDetails()
    {
        var health = await _client.GetAsync("/health");
        health.StatusCode.Should().Be(HttpStatusCode.OK);
        health.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        health.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");

        var token = await LoginAsync("charlie@accessdesk.dev", "Employee@123!");
        SetBearer(token);
        var invalid = await _client.PostAsJsonAsync("/api/requests", new
        {
            applicationId = 3,
            requestedLevel = "Read",
            justification = "invalid duration test",
            durationDays = 0
        });
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        invalid.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await invalid.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("errors").TryGetProperty("DurationDays", out _).Should().BeTrue();
    }

    [Fact]
    public async Task RequestAndNotificationLists_ClampPageSizeAndSupportFilters()
    {
        var token = await LoginAsync("charlie@accessdesk.dev", "Employee@123!");
        SetBearer(token);
        var requests = await _client.GetFromJsonAsync<JsonElement>("/api/requests/my?page=0&pageSize=999&status=Approved&search=Jira");
        requests.GetProperty("page").GetInt32().Should().Be(1);
        requests.GetProperty("pageSize").GetInt32().Should().Be(100);
        requests.GetProperty("items").EnumerateArray().All(item => item.GetProperty("status").GetString() == "Approved")
            .Should().BeTrue();

        var notifications = await _client.GetFromJsonAsync<JsonElement>("/api/notifications?page=0&pageSize=999&unreadOnly=true");
        notifications.GetProperty("page").GetInt32().Should().Be(1);
        notifications.GetProperty("pageSize").GetInt32().Should().Be(100);
        notifications.GetProperty("items").EnumerateArray().All(item => !item.GetProperty("isRead").GetBoolean())
            .Should().BeTrue();
    }
}
