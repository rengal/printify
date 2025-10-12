using FluentValidation;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Create;

public class CreatePrinterValidator : AbstractValidator<CreatePrinterCommand>
{
    public CreatePrinterValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Printer name must not be empty.")
            .MaximumLength(PrinterConstants.MaxNameLength).WithMessage(x =>
                $"Printer name must not exceed {PrinterConstants.MaxNameLength} characters. Length: {x.DisplayName.Length}");

        RuleFor(x => x.Protocol)
            .IsInEnum().WithMessage(x => $"Invalid printer protocol: {x.Protocol}");

        RuleFor(x => x.WidthInDots)
            .GreaterThanOrEqualTo(PrinterConstants.MinWidthInDots)
            .WithMessage($"Width in dots must be greater than or equal to {PrinterConstants.MinWidthInDots}")
            .LessThanOrEqualTo(PrinterConstants.MaxWidthInDots)
            .WithMessage($"Width cannot exceed {PrinterConstants.MaxWidthInDots} dots");

        RuleFor(x => x.HeightInDots)
            .Must(height => height is null or >= PrinterConstants.MaxHeightInDots)
            .WithMessage(
                $"Height in dots must be greater than or equal to {PrinterConstants.MaxHeightInDots}, if specified");

        RuleFor(x => x.TcpListenPort)
            .Must(tcpListenPort => tcpListenPort is null or >= PrinterConstants.MinTcpListenerPort and <= PrinterConstants.MaxTcpListenerPort)
            .WithMessage(x => $"{nameof(x.TcpListenPort)} must be in range {PrinterConstants.MinTcpListenerPort}-{PrinterConstants.MaxTcpListenerPort}");
    }
}
