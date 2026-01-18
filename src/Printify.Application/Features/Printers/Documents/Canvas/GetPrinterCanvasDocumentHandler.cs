using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;

namespace Printify.Application.Features.Printers.Documents.Canvas;

public sealed class GetPrinterCanvasDocumentHandler(
    IPrinterRepository printerRepository,
    IDocumentRepository documentRepository,
    IRendererFactory rendererFactory)
    : IRequestHandler<GetPrinterCanvasDocumentQuery, RenderedDocument?>
{
    public async Task<RenderedDocument?> Handle(
        IReceiveContext<GetPrinterCanvasDocumentQuery> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.WorkspaceId,
            cancellationToken).ConfigureAwait(false);

        if (printer is null)
        {
            // Guard: prevent leaking document data from non-existent printers.
            throw new PrinterNotFoundException(request.PrinterId);
        }

        var document = await documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            .ConfigureAwait(false);

        if (document is null || document.PrinterId != request.PrinterId)
        {
            // Ensure documents are only returned for the requested printer.
            return null;
        }

        var renderer = rendererFactory.GetRenderer(document.Protocol);
        var canvas = renderer.Render(document);
        return RenderedDocument.From(document, canvas);
    }
}
