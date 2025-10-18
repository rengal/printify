using FluentValidation;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Update;

public sealed class UpdatePrinterValidator : AbstractValidator<UpdatePrinterCommand>
{
    public UpdatePrinterValidator()
    {
        RuleFor(command => command.PrinterId)
            .NotEmpty();

        RuleFor(command => command.DisplayName)
            .NotEmpty()
            .MaximumLength(PrinterConstants.MaxNameLength);

        RuleFor(command => command.WidthInDots)
            .InclusiveBetween(PrinterConstants.MinWidthInDots, PrinterConstants.MaxWidthInDots);

        RuleFor(command => command.HeightInDots)
            .Must(height => height is null || height >= PrinterConstants.MinHeightInDots)
            .WithMessage($"{nameof(UpdatePrinterCommand.HeightInDots)} must be null or >= {PrinterConstants.MinHeightInDots}");

        RuleFor(command => command.TcpListenPort)
            .Must(port => port is null || port >= PrinterConstants.MinTcpListenerPort && port <= PrinterConstants.MaxTcpListenerPort)
            .WithMessage($"{nameof(UpdatePrinterCommand.TcpListenPort)} must be within {PrinterConstants.MinTcpListenerPort}-{PrinterConstants.MaxTcpListenerPort} when provided.");
    }
}
