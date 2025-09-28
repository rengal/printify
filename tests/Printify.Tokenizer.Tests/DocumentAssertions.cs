namespace Printify.Tokenizer.Tests;

using System.Collections.Generic;
using Contracts;
using Contracts.Elements;
using Xunit;

internal static class DocumentAssertions
{
    public static void Equal(Document? actual, Protocol expectedProtocol, IReadOnlyList<Element> expectedElements)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedProtocol, actual!.Protocol);
        Assert.Equal(expectedElements.Count, actual.Elements.Count);

        for (var index = 0; index < expectedElements.Count; index++)
        {
            Assert.Equal(expectedElements[index], actual.Elements[index]);
        }
    }
}
