using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Pin;

public sealed class SetPrinterPinnedHandler(IPrinterRepository printerRepository) : IRequestHandler<SetPrinterPinnedCommand, Printer>
{
    public async Task<Printer> Handle(SetPrinterPinnedCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.UserId, request.Context.AnonymousSessionId, cancellationToken)
            .ConfigureAwait(false);

        if (printer is null)
        {
            throw new InvalidOperationException("Printer not found.");
        }

        await printerRepository
            .SetPinnedAsync(request.PrinterId, request.Context.UserId, request.Context.AnonymousSessionId, request.IsPinned, cancellationToken)
            .ConfigureAwait(false);

        return printer with { IsPinned = request.IsPinned };
    }
}
