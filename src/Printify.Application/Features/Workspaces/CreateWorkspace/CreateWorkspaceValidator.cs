using FluentValidation;

namespace Printify.Application.Features.Workspaces.CreateWorkspace;

public sealed class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceCommand>
{
    private const int MaxDisplayNameLength = 128;

    public CreateWorkspaceValidator()
    {
        RuleFor(command => command.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace id must be supplied.");

        RuleFor(command => command.WorkspaceName)
            .MaximumLength(MaxDisplayNameLength)
            .WithMessage($"Workspace name cannot exceed {MaxDisplayNameLength} characters.");
    }
}
