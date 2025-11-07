using Printify.Web.Contracts.Documents.Responses.Elements;

namespace Printify.Web.Contracts.Documents.Responses;

/// <summary>
/// Immutable representation of a document exposed by the public API.
/// </summary>
/// <param name="Id">Identifier of the document.</param>
/// <param name="PrintJobId">Print job that produced the document.</param>
/// <param name="PrinterId">Identifier of the printer associated with the document.</param>
/// <param name="Timestamp">Creation timestamp (UTC) when the document was finalized or captured.</param>
/// <param name="Protocol">Protocol the bytes were parsed with.</param>
/// <param name="ClientAddress">Address observed for the producer of the document.</param>
/// <param name="Elements">Ordered list of document elements.</param>
public sealed record DocumentDto(
    Guid Id,
    Guid PrintJobId,
    Guid PrinterId,
    DateTimeOffset Timestamp,
    string Protocol,
    string? ClientAddress,
    IReadOnlyList<ResponseElement> Elements);
