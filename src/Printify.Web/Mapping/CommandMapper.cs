using Printify.Domain.Printers;
using Printify.Domain.Requests;
using Features = Printify.Application.Features;
using WebApi = Printify.Web.Contracts;

namespace Printify.Web.Mapping;

internal static class CommandMapper
{
    // internal static CreateDocumentCommand ToCommand(CreateDocumentRequest request, string? sourceIp)
    // {
    //     ArgumentNullException.ThrowIfNull(request);
    //
    //     var protocol = ParseProtocol(request.Protocol);
    //     var elements = request.Elements?.Select(ToDomainElement).ToList() ?? new List<Element>();
    //
    //     return new CreateDocumentCommand
    //         {
    //         request.PrinterId,
    //         protocol,
    //         sourceIp,
    //         elements
    //         }
    // }

    internal static Features.Printers.Create.CreatePrinterCommand ToCommand(this WebApi.Printers.Requests.CreatePrinterRequestDto request,  RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new Features.Printers.Create.CreatePrinterCommand(
            context,
            request.DisplayName,
            ParseProtocol(request.Protocol),
            request.WidthInDots,
            request.HeightInDots,
            request.TcpListenPort);
    }

    internal static Features.Auth.Login.LoginCommand ToCommand(this WebApi.Auth.Requests.LoginRequestDto request, RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new Features.Auth.Login.LoginCommand(context, request.DisplayName);
    }

    private static Protocol ParseProtocol(string protocol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);

        if (Enum.TryParse(protocol, true, out Protocol parsed))
        {
            return parsed;
        }

        throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Protocol is not supported.");
    }
}
