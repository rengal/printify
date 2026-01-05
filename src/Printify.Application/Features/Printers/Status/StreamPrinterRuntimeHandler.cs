using System.Runtime.CompilerServices;
using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Status;

public sealed class StreamPrinterRuntimeHandler(
    IPrinterRepository printerRepository,
    IPrinterStatusStream statusStream)
    : IRequestHandler<StreamPrinterRuntimeQuery, PrinterRuntimeStreamResult>
{
    public async Task<PrinterRuntimeStreamResult> Handle(
        StreamPrinterRuntimeQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Context.WorkspaceId is null)
        {
            throw new BadRequestException("Workspace identifier must be provided.");
        }

        var printer = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);
        if (printer is null)
        {
            throw new PrinterNotFoundException(request.PrinterId);
        }

        var updates = ReadUpdatesAsync(
            request.Context.WorkspaceId.Value,
            request.PrinterId,
            cancellationToken);

        return new PrinterRuntimeStreamResult("status", updates);
    }

    private async IAsyncEnumerable<PrinterStatusUpdate> ReadUpdatesAsync(
        Guid workspaceId,
        Guid printerId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var update in statusStream.Subscribe(workspaceId, ct))
        {
            if (update.PrinterId != printerId)
            {
                continue;
            }

            if (update.RuntimeUpdate is null
                && update.OperationalFlagsUpdate is null
                && update.Settings is null
                && update.Printer is null)
            {
                continue;
            }

            yield return update;
        }
    }
}
