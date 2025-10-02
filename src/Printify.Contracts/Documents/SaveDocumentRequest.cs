using System.Collections.Generic;
using Printify.Contracts.Documents.Elements;

namespace Printify.Contracts.Documents;

/// <summary>
/// Request payload to persist a new document.
/// </summary>
/// <param name="PrinterId">Identifier of the printer producing the document.</param>
/// <param name="Protocol">Protocol the document was parsed with.</param>
/// <param name="SourceIp">Optional source IP associated with the document.</param>
/// <param name="Elements">Ordered list of document elements.</param>
public sealed record SaveDocumentRequest(
    long PrinterId,
    Protocol Protocol,
    string? SourceIp,
    IReadOnlyList<Element> Elements);
