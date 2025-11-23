namespace Printify.Tests.Shared.EscPos;

using Printify.Domain.Documents.Elements;

/// <summary>
/// Represents a deterministic ESC/POS parser scenario consisting of an input payload
/// and the elements that must be produced after parsing it.
/// </summary>
/// <param name="Input">Raw ESC/POS byte sequence to parse.</param>
/// <param name="ExpectedElements">Expected elements containing upload-stage media (RasterImageUpload, MediaUpload) for internal service testing.</param>
/// <param name="ExpectedFinalizedElements">Expected elements containing finalized, persisted media (RasterImage, Media) for public API testing. If null, uses ExpectedElements.</param>
public sealed record EscPosScenario(
    byte[] Input,
    IReadOnlyList<Element> ExpectedElements,
    IReadOnlyList<Element>? ExpectedFinalizedElements = null);
