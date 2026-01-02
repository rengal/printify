namespace Printify.Tests.Shared.EscPos;

using Printify.Domain.Documents.Elements;
using Printify.Web.Contracts.Documents.Responses.View.Elements;

/// <summary>
/// Represents a deterministic ESC/POS parser scenario consisting of an input payload
/// and the elements that must be produced after parsing it.
/// </summary>
/// <param name="Input">Raw ESC/POS byte sequence to parse.</param>
/// <param name="ExpectedRequestElements">Expected elements containing upload-stage media (RasterImageUpload, MediaUpload) for internal service testing.</param>
/// <param name="ExpectedPersistedElements">Expected elements containing finalized, persisted media (RasterImage, Media) for public API testing.</param>
/// <param name="ExpectedViewElements">Expected view elements derived from persisted elements.</param>
public sealed record EscPosScenario
{
    public EscPosScenario(
        byte[] input,
        IReadOnlyList<Element> expectedRequestElements,
        IReadOnlyList<Element>? expectedPersistedElements = null,
        IReadOnlyList<ViewElementDto>? expectedViewElements = null)
    {
        Input = input;
        ExpectedRequestElements = expectedRequestElements;
        ExpectedPersistedElements = expectedPersistedElements;
        ExpectedViewElements = expectedViewElements ?? Array.Empty<ViewElementDto>();
    }

    public byte[] Input { get; }
    public IReadOnlyList<Element> ExpectedRequestElements { get; }
    public IReadOnlyList<Element>? ExpectedPersistedElements { get; }
    public IReadOnlyList<ViewElementDto> ExpectedViewElements { get; }
}
