using FluentValidation;

namespace Printify.Application.Features.Printers.Pin;

public sealed class SetPrinterPinnedValidator : AbstractValidator<SetPrinterPinnedCommand>
{
    public SetPrinterPinnedValidator()
    {
        RuleFor(command => command.PrinterId)
            .NotEmpty();
    }
}
