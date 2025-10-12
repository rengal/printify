using MediatR;
using Printify.Domain.Printers;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Printers.Create;

public record CreatePrinterCommand(
    RequestContext Context,
    string DisplayName,
    Protocol Protocol,
    int WidthInDots,
    int? HeightInDots,
    int? TcpListenPort)
    : IRequest<Guid>;
