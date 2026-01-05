using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Get;

public sealed class GetPrinterHandler(
    IPrinterRepository printerRepository,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IRequestHandler<GetPrinterQuery, PrinterDetailsSnapshot?>
{
    public async Task<PrinterDetailsSnapshot?> Handle(GetPrinterQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Context.WorkspaceId is null)
        {
            throw new BadRequestException("Workspace identifier must be provided.");
        }

        var printer = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.WorkspaceId,
            cancellationToken);

        if (printer is null)
        {
            return null;
        }

        var flags = await printerRepository.GetOperationalFlagsAsync(printer.Id, cancellationToken)
            .ConfigureAwait(false);
        var settings = await printerRepository.GetSettingsAsync(printer.Id, cancellationToken)
            .ConfigureAwait(false);
        // Settings are persisted separately; missing settings indicate a data integrity issue.
        if (settings is null)
        {
            throw new InvalidOperationException($"Settings for printer {printer.Id} are missing.");
        }
        var runtimeStatus = runtimeStatusStore.Get(printer.Id);

        return new PrinterDetailsSnapshot(printer, settings, flags, runtimeStatus);
    }
}
