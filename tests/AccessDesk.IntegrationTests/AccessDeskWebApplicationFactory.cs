using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AccessDesk.Infrastructure.Persistence;

namespace AccessDesk.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory that swaps SQL Server for an isolated EF Core InMemory database
/// so integration tests run fast without needing a live SQL Server instance.
/// </summary>
public class AccessDeskWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"AccessDeskTest_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Secret", "IntegrationTest-Super-Secret-Key-32Chars!");
        builder.UseSetting("Jwt:Issuer", "AccessDesk");
        builder.UseSetting("Jwt:Audience", "AccessDesk");

        builder.ConfigureServices(services =>
        {
            // Remove the real SQL Server DbContext and EF Core 9 provider configuration
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AccessDeskDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<AccessDeskDbContext>))
                .ToList();

            foreach (var d in descriptors)
                services.Remove(d);

            // Register InMemory database for isolated test runs
            services.AddDbContext<AccessDeskDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Seed demo users, roles, apps, and sample requests
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TestSeeder");
            DatabaseSeeder.SeedAsync(scope.ServiceProvider, logger).GetAwaiter().GetResult();
        });
    }
}
