namespace Printify.Tests.Shared.EscPos;

using Printify.Domain.Documents.Elements;

/// <summary>
/// Represents a deterministic ESC/POS parser scenario consisting of an input payload
/// and the elements that must be produced after parsing it.
/// </summary>
public sealed record EscPosScenario(byte[] Input, IReadOnlyList<Element> ExpectedElements);
