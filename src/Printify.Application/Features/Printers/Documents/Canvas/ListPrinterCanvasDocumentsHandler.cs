using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;

namespace Printify.Application.Features.Printers.Documents.Canvas;

public sealed class ListPrinterCanvasDocumentsHandler(
    IPrinterRepository printerRepository,
    IDocumentRepository documentRepository,
    IRendererFactory rendererFactory)
    : IRequestHandler<ListPrinterCanvasDocumentsQuery, PrinterCanvasDocumentListResponse>
{
    public async Task<PrinterCanvasDocumentListResponse> Handle(
        IReceiveContext<ListPrinterCanvasDocumentsQuery> context,
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
            // Guard: list only for printers that exist in the workspace.
            throw new PrinterNotFoundException(request.PrinterId);
        }

        var documents = await documentRepository.ListByPrinterIdAsync(
            request.PrinterId,
            request.BeforeId,
            request.Limit,
            cancellationToken).ConfigureAwait(false);

        var renderedDocuments = new List<RenderedDocument>(documents.Count);
        foreach (var document in documents)
        {
            // Render each document using its protocol-specific renderer.
            var renderer = rendererFactory.GetRenderer(document.Protocol);
            var canvas = renderer.Render(document);
            renderedDocuments.Add(RenderedDocument.From(document, canvas));
        }

        return new PrinterCanvasDocumentListResponse(renderedDocuments.AsReadOnly());
    }
}
