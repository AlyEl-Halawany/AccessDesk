using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using AccessDesk.Application.Common.Behaviors;

namespace AccessDesk.Application;

/// <summary>
/// Registers all Application layer services into the DI container.
/// Called from the API project's Program.cs.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register all MediatR handlers from this assembly
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly));

        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceExtensions).Assembly);

        // Add the validation pipeline behavior — it runs validators before every handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
