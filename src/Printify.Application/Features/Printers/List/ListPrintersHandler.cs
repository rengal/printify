using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.List;

public sealed class ListPrintersHandler(IPrinterRepository printerRepository)
    : IRequestHandler<ListPrintersQuery, IReadOnlyList<Printer>>
{
    public async Task<IReadOnlyList<Printer>> Handle(ListPrintersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = request.Context;

        if (context.UserId is null && context.AnonymousSessionId is null)
        {
            throw new InvalidOperationException("At least one owner identifier must be provided.");
        }

        if (context.UserId is not null)
        {
            return await printerRepository
                .ListOwnedAsync(context.UserId, null, cancellationToken)
                .ConfigureAwait(false);
        }

        return await printerRepository
            .ListOwnedAsync(null, context.AnonymousSessionId, cancellationToken)
            .ConfigureAwait(false);
    }
}
