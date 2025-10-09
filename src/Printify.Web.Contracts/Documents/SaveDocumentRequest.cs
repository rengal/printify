using Printify.Web.Contracts.Documents.Elements;

namespace Printify.Web.Contracts.Documents;

/// <summary>
/// Request payload to persist a new document.
/// </summary>
/// <param name="PrinterId">Identifier of the printer producing the document.</param>
/// <param name="Protocol">Protocol the document was parsed with.</param>
/// <param name="Elements">Ordered list of document elements.</param>
public sealed record SaveDocumentRequest(
    long PrinterId,
    string Protocol,
    IReadOnlyList<Element> Elements);
