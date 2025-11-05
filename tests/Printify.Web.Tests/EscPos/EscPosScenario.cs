using Printify.Domain.Documents.Elements;

namespace Printify.Web.Tests.EscPos;

public sealed record EscPosScenario(byte[] Input, IReadOnlyList<Element> ExpectedElements);
