using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Printify.Application.Interfaces;

namespace Printify.Application.Features.Printers.Delete;

public sealed class DeletePrinterHandler(IPrinterRepository printerRepository)
    : IRequestHandler<DeletePrinterCommand, Unit>
{
    public async Task<Unit> Handle(DeletePrinterCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.UserId, request.Context.AnonymousSessionId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            throw new InvalidOperationException("Printer not found.");
        }

        await printerRepository.DeleteAsync(existing, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
