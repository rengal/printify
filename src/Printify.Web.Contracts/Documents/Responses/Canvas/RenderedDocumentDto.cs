namespace Printify.Web.Contracts.Documents.Responses.Canvas;

/// <summary>
/// Rendered document representation with canvas primitives for preview.
/// </summary>
/// <param name="Id">Identifier of the document.</param>
/// <param name="PrintJobId">Print job that produced the document.</param>
/// <param name="PrinterId">Identifier of the printer associated with the document.</param>
/// <param name="Timestamp">Creation timestamp (UTC) when the document was finalized or captured.</param>
/// <param name="Protocol">Protocol the bytes were parsed with.</param>
/// <param name="Canvases">Array of canvases with dimensions and layout items. Multiple canvases represent pages/labels within the document.</param>
/// <param name="ClientAddress">Address observed for the producer of the document.</param>
/// <param name="BytesReceived">Total bytes received from the client during the session.</param>
/// <param name="BytesSent">Total bytes sent to the client during the session.</param>
/// <param name="ErrorMessages">Collection of error messages from parsing or device responses.</param>
public sealed record RenderedDocumentDto(
    Guid Id,
    Guid PrintJobId,
    Guid PrinterId,
    DateTimeOffset Timestamp,
    string Protocol,
    CanvasDto[] Canvases,
    string? ClientAddress,
    int BytesReceived,
    int BytesSent,
    string[]? ErrorMessages);
