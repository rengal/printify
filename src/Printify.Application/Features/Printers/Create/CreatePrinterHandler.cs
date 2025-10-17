using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Create;

public sealed class CreatePrinterHandler(IPrinterRepository printerRepository)
    : IRequestHandler<CreatePrinterCommand, Printer>
{
    public async Task<Printer> Handle(
        CreatePrinterCommand request,
        CancellationToken ct)
    {
        var existing = await printerRepository.GetByIdAsync(request.PrinterId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            // Simplified idempotency: return existing printer without reapplying side effects.
            return existing;
        }

        var listenTcpPortNumber = request.TcpListenPort
            ?? await printerRepository.GetFreeTcpPortNumber(ct).ConfigureAwait(false);

        // NOTE: Simplified idempotency – only the identifier is reused to detect duplicates.
        // We do not persist the original response payload, so subsequent retries could observe changes.

        var printer = new Printer(
            request.PrinterId,
            request.Context.AnonymousSessionId,
            request.Context.UserId,
            request.DisplayName,
            request.Protocol.ToString(), //todo enum to string
            request.WidthInDots,
            request.HeightInDots,
            DateTimeOffset.UtcNow,
            request.Context.IpAddress,
            listenTcpPortNumber,
            false);

        await printerRepository.AddAsync(printer, ct).ConfigureAwait(false);

        return printer;
    }
}
