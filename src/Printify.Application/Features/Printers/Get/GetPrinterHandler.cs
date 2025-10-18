using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Get;

public sealed class GetPrinterHandler(IPrinterRepository printerRepository)
    : IRequestHandler<GetPrinterQuery, Printer?>
{
    public Task<Printer?> Handle(GetPrinterQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.UserId,
            request.Context.AnonymousSessionId,
            cancellationToken).AsTask();
    }
}
