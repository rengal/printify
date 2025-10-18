using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Update;

public sealed class UpdatePrinterHandler(IPrinterRepository printerRepository)
    : IRequestHandler<UpdatePrinterCommand, Printer>
{
    public async Task<Printer> Handle(UpdatePrinterCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.UserId,
            request.Context.AnonymousSessionId,
            cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            throw new InvalidOperationException("Printer not found.");
        }

        var updated = existing with
        {
            DisplayName = request.DisplayName,
            Protocol = request.Protocol.ToString(),
            WidthInDots = request.WidthInDots,
            HeightInDots = request.HeightInDots,
            ListenTcpPortNumber = request.TcpListenPort ?? existing.ListenTcpPortNumber
        };

        await printerRepository.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);

        return updated;
    }
}
