using Printify.Application.Features.Workspaces.CreateWorkspace;
using Printify.Domain.Printers;
using Printify.Domain.Requests;
using Features = Printify.Application.Features;
using WebApi = Printify.Web.Contracts;

namespace Printify.Web.Mapping;

internal static class CommandMapper
{
    internal static Features.Printers.Create.CreatePrinterCommand ToCommand(this WebApi.Printers.Requests.CreatePrinterRequestDto request,  RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new Features.Printers.Create.CreatePrinterCommand(
            context,
            new Features.Printers.Create.CreatePrinterPayload(
                request.Printer.Id,
                request.Printer.DisplayName),
            new Features.Printers.Create.CreatePrinterSettingsPayload(
                ParseProtocol(request.Settings.Protocol),
                request.Settings.WidthInDots,
                request.Settings.HeightInDots,
                request.Settings.EmulateBufferCapacity,
                request.Settings.BufferDrainRate,
                request.Settings.BufferMaxCapacity));
    }

    internal static Features.Printers.Update.UpdatePrinterCommand ToCommand(this WebApi.Printers.Requests.UpdatePrinterRequestDto request, Guid printerId, RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new Features.Printers.Update.UpdatePrinterCommand(
            context,
            printerId,
            new Features.Printers.Update.UpdatePrinterPayload(
                request.Printer.DisplayName),
            new Features.Printers.Update.UpdatePrinterSettingsPayload(
                ParseProtocol(request.Settings.Protocol),
                request.Settings.WidthInDots,
                request.Settings.HeightInDots,
                request.Settings.EmulateBufferCapacity,
                request.Settings.BufferDrainRate,
                request.Settings.BufferMaxCapacity));
    }

    internal static Features.Printers.Pin.SetPrinterPinnedCommand ToCommand(this WebApi.Printers.Requests.PinPrinterRequestDto request, Guid printerId, RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new Features.Printers.Pin.SetPrinterPinnedCommand(context, printerId, request.IsPinned);
    }

    internal static Features.Auth.Login.LoginCommand ToCommand(this WebApi.Auth.Requests.LoginRequestDto request, RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new Features.Auth.Login.LoginCommand(context, request.Token);
    }

    internal static CreateWorkspaceCommand ToCommand(this WebApi.Workspaces.Requests.CreateWorkspaceRequestDto request, RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateWorkspaceCommand(context, request.Id, request.WorkspaceName);
    }

    private static Protocol ParseProtocol(string protocol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        return protocol.ToLowerInvariant() switch
        {
            ProtocolConstants.EscPos => Protocol.EscPos,
            ProtocolConstants.Epl => Protocol.Epl,
            _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Protocol is not supported.")
        };
    }
}
