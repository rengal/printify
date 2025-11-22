namespace Printify.Web.Contracts.Documents.Requests;

/// <summary>
/// Carries the identifier of the last document viewed by the client.
/// </summary>
/// <param name="DocumentId">Identifier of the document that was last viewed.</param>
public sealed record SetLastViewedDocumentRequestDto(Guid DocumentId);
