using FluentValidation;

namespace Printify.Application.Features.Users.CreateUser;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    private const int MaxDisplayNameLength = 128;

    public CreateUserValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("User id must be supplied.");

        RuleFor(command => command.DisplayName)
            .NotEmpty()
            .WithMessage("Display name must not be empty.")
            .MaximumLength(MaxDisplayNameLength)
            .WithMessage($"Display name cannot exceed {MaxDisplayNameLength} characters.");
    }
}
