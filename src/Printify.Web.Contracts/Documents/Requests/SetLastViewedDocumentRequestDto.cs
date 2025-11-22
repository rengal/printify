namespace Printify.Web.Contracts.Documents.Requests;

/// <summary>
/// Carries the identifier of a document that was last viewed by the client.
/// </summary>
/// <param name="DocumentId">Identifier of the document that was last viewed.</param>
public sealed record SetLastViewedDocumentRequestDto(Guid DocumentId);
