using FluentValidation;

namespace AccessDesk.Application.AccessRequests.Commands;

public class SubmitAccessRequestValidator : AbstractValidator<SubmitAccessRequestCommand>
{
    public SubmitAccessRequestValidator()
    {
        RuleFor(x => x.RequesterId).GreaterThan(0);
        RuleFor(x => x.ApplicationId).GreaterThan(0);

        RuleFor(x => x.Justification)
            .NotEmpty().WithMessage("Justification is required.")
            .MinimumLength(20).WithMessage("Justification must be at least 20 characters. Be specific about why you need this access.")
            .MaximumLength(2000);

        RuleFor(x => x.DurationDays)
            .InclusiveBetween(1, 365).WithMessage("Duration must be between 1 and 365 days.");
    }
}
