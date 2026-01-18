using Mediator.Net.Contracts;
using Printify.Domain.Printers;
using DomainCanvas = Printify.Domain.Layout.Canvas;

namespace Printify.Domain.Documents;

/// <summary>
/// Document metadata combined with a rendered canvas.
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
    DomainCanvas Canvas,
    string[]? ErrorMessages) : IResponse
{
    public static RenderedDocument From(Document document, DomainCanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(canvas);

        // Preserve document metadata alongside the rendered canvas.
        return new RenderedDocument(
            document.Id,
            document.PrintJobId,
            document.PrinterId,
            document.Timestamp,
            document.Protocol,
            document.ClientAddress,
            document.BytesReceived,
            document.BytesSent,
            canvas,
            document.ErrorMessages);
    }
}
