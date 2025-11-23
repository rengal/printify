using MediatR;
using Printify.Application.Exceptions;
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

        if (context.WorkspaceId is null)
            throw new BadRequestException("Workspace identifier must be provided.");

        return await printerRepository
            .ListOwnedAsync(context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);
    }
}
