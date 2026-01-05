using FluentValidation;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Update;

public sealed class UpdatePrinterValidator : AbstractValidator<UpdatePrinterCommand>
{
    public UpdatePrinterValidator()
    {
        RuleFor(command => command.PrinterId)
            .NotEmpty();

        RuleFor(command => command.Printer.DisplayName)
            .NotEmpty()
            .MaximumLength(PrinterConstants.MaxNameLength);

        RuleFor(command => command.Settings.WidthInDots)
            .InclusiveBetween(PrinterConstants.MinWidthInDots, PrinterConstants.MaxWidthInDots);

        RuleFor(command => command.Settings.HeightInDots)
            .Must(height => height is null || height >= PrinterConstants.MinHeightInDots)
            .WithMessage($"{nameof(UpdatePrinterSettingsPayload.HeightInDots)} must be null or >= {PrinterConstants.MinHeightInDots}");
    }
}
