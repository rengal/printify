using FluentValidation;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Create;

public class CreatePrinterValidator : AbstractValidator<CreatePrinterCommand>
{
    public CreatePrinterValidator()
    {
        RuleFor(x => x.Printer.Id)
            .NotEmpty()
            .WithMessage("Printer id must be supplied.");

        RuleFor(x => x.Printer.DisplayName)
            .NotEmpty()
            .WithMessage("Printer name must not be empty.")
            .MaximumLength(PrinterConstants.MaxNameLength).WithMessage(x =>
                $"Printer name must not exceed {PrinterConstants.MaxNameLength} characters. Length: {x.Printer.DisplayName.Length}");

        RuleFor(x => x.Settings.Protocol)
            .IsInEnum().WithMessage(x => $"Invalid printer protocol: {x.Settings.Protocol}");

        RuleFor(x => x.Settings.WidthInDots)
            .GreaterThanOrEqualTo(PrinterConstants.MinWidthInDots)
            .WithMessage($"Width in dots must be greater than or equal to {PrinterConstants.MinWidthInDots}")
            .LessThanOrEqualTo(PrinterConstants.MaxWidthInDots)
            .WithMessage($"Width cannot exceed {PrinterConstants.MaxWidthInDots} dots");

        RuleFor(x => x.Settings.HeightInDots)
            .Must(height => height is null or >= PrinterConstants.MaxHeightInDots)
            .WithMessage(
                $"Height in dots must be greater than or equal to {PrinterConstants.MaxHeightInDots}, if specified");
    }
}
