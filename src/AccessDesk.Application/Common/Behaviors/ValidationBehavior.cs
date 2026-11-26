using FluentValidation;
using MediatR;

namespace AccessDesk.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs FluentValidation validators automatically
/// before every command handler. This keeps validation out of the handlers and
/// ensures it always runs regardless of which endpoint called the command.
///
/// If validation fails, a ValidationException is thrown. The API's global error
/// handler catches it and returns a 400 response with the validation errors.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        // Run all validators concurrently
        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
