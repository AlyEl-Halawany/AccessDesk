using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using AccessDesk.Domain.Entities;
using AccessDesk.Domain.Interfaces;
using AccessDesk.Infrastructure.Persistence;
using AccessDesk.Infrastructure.Persistence.Repositories;
using AccessDesk.Infrastructure.Services;

namespace AccessDesk.Infrastructure;

/// <summary>
/// Extension method that wires up all Infrastructure services into the DI container.
/// The API project calls this once in Program.cs. This keeps Program.cs clean and
/// Infrastructure self-contained.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- Database ---
        services.AddDbContext<AccessDeskDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AccessDeskDbContext).Assembly.FullName)));

        // --- ASP.NET Core Identity ---
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Reasonable password policy — not too strict to block demo, not too weak
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;

            // Lock account after 5 failed attempts for 5 minutes
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<AccessDeskDbContext>()
        .AddDefaultTokenProviders();

        // --- JWT Authentication ---
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(jwtSection);
        var jwtSettings = jwtSection.Get<JwtSettings>()!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No grace period — tokens expire exactly when they say
            };
        });

        // --- Repositories ---
        services.AddScoped<IAccessRequestRepository, AccessRequestRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<ISodConflictRepository, SodConflictRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAccessReviewRepository, AccessReviewRepository>();

        // --- Application Services ---
        services.AddScoped<TokenService>();

        // --- Email ---
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}
