using Printify.Web.Contracts.Documents.Responses.View.Elements;

namespace Printify.Web.Contracts.Documents.Responses.View;

/// <summary>
/// View-oriented representation of a document, using absolute positioning for rendering.
/// </summary>
/// <param name="Id">Identifier of the document.</param>
/// <param name="PrintJobId">Print job that produced the document.</param>
/// <param name="PrinterId">Identifier of the printer associated with the document.</param>
/// <param name="Timestamp">Creation timestamp (UTC) when the document was finalized or captured.</param>
/// <param name="Protocol">Protocol the bytes were parsed with.</param>
/// <param name="WidthInDots">Printer width in dots at the time of capture.</param>
/// <param name="HeightInDots">Optional printer height in dots at the time of capture.</param>
/// <param name="ClientAddress">Address observed for the producer of the document.</param>
/// <param name="Elements">Ordered list of view elements, including non-visual state changes.</param>
/// <param name="ErrorMessages">Collection of error messages from Error or PrinterError elements. Null if no errors present.</param>
public sealed record ViewDocumentDto(
    Guid Id,
    Guid PrintJobId,
    Guid PrinterId,
    DateTimeOffset Timestamp,
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    string? ClientAddress,
    IReadOnlyList<ViewElementDto> Elements,
    string[]? ErrorMessages);
