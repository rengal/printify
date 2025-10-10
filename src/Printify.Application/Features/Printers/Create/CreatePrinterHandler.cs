using Printify.Application.Interfaces;
using Printify.Domain.Printers;
using MediatR;

namespace Printify.Application.Features.Printers.Create;

public sealed class CreatePrinterHandler(IPrinterRepository printerRepository, IUnitOfWork uow)
    : IRequestHandler<CreatePrinterCommand, long>
{
    public async Task<long> Handle(
        CreatePrinterCommand request,
        CancellationToken ct)
    {
        await uow.BeginTransactionAsync(ct);
        try
        {
            var listenTcpPortNumber = await printerRepository.GetFreeTcpPortNumber(ct);

            var printer = new Printer(Id: 0, //todo ?
                request.Context.UserId,
                request.Context.SessionId,
                request.DisplayName,
                request.Protocol.ToString(), //todo enum to string
                request.WidthInDots,
                request.HeightInDots,
                DateTimeOffset.Now,
                request.Context.IpAddress,
                listenTcpPortNumber);

            await printerRepository.AddAsync(printer, ct);

            return printer.Id;
        }
        finally
        {
            await uow.CommitAsync(ct);
        }
    }
}
