using Mediator.Net.Contracts;
using Printify.Domain.Printers;
using DomainCanvas = Printify.Domain.Layout.Canvas;

namespace Printify.Domain.Documents;

/// <summary>
/// Document metadata combined with rendered canvases.
/// Multiple canvases represent pages/labels within a single document.
/// </summary>
public sealed record RenderedDocument(
    Guid Id,
    Guid PrintJobId,
    Guid PrinterId,
    DateTimeOffset Timestamp,
    Protocol Protocol,
    string? ClientAddress,
    int BytesReceived,
    int BytesSent,
    DomainCanvas[] Canvases,
    string[]? ErrorMessages) : IResponse
{
    public static RenderedDocument From(Document document, DomainCanvas[] canvases)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(canvases);

        // Preserve document metadata alongside the rendered canvases.
        return new RenderedDocument(
            document.Id,
            document.PrintJobId,
            document.PrinterId,
            document.Timestamp,
            document.Protocol,
            document.ClientAddress,
            document.BytesReceived,
            document.BytesSent,
            canvases,
            document.ErrorMessages);
    }
}
