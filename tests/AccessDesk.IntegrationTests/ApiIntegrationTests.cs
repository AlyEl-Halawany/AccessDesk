using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace AccessDesk.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<AccessDeskWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(AccessDeskWebApplicationFactory factory)
    {
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
}
