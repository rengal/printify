using Printify.Application.Interfaces;
using Printify.Domain.Printers;
using MediatR;

namespace Printify.Application.Features.Printers.Create;

public sealed class CreatePrinterHandler(IPrinterRepository printerRepository, IUnitOfWork uow)
    : IRequestHandler<CreatePrinterCommand, Guid>
{
    public async Task<Guid> Handle(
        CreatePrinterCommand request,
        CancellationToken ct)
    {
        await uow.BeginTransactionAsync(ct);
        try
        {
            var listenTcpPortNumber = await printerRepository.GetFreeTcpPortNumber(ct);

            var printer = new Printer(Guid.NewGuid(),
                request.Context.AnonymousSessionId,
                request.Context.UserId,
                request.DisplayName,
                request.Protocol.ToString(), //todo enum to string
                request.WidthInDots,
                request.HeightInDots,
                DateTimeOffset.Now,
                request.Context.IpAddress,
                listenTcpPortNumber);

            var printerId = await printerRepository.AddAsync(printer, ct);

            return printerId;
        }
        finally
        {
            await uow.CommitAsync(ct);
        }
    }
}
