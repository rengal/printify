using FluentValidation;

namespace Printify.Application.Features.Auth.CreateAnonymousSession;

public sealed class CreateAnonymousSessionValidator : AbstractValidator<CreateAnonymousSessionCommand>
{
    private const int MaxIpLength = 64;

    public CreateAnonymousSessionValidator()
    {
        RuleFor(x => x.Context)
            .NotNull()
            .WithMessage("Request context must be provided to create an anonymous session.");

        RuleFor(x => x.Context.IpAddress)
            .NotEmpty()
            .WithMessage("Client IP address is required to establish an anonymous session.")
            .MaximumLength(MaxIpLength)
            .WithMessage($"Client IP address must not exceed {MaxIpLength} characters.");
    }
}
