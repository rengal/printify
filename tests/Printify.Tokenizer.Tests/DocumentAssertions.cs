namespace Printify.Tokenizer.Tests;

using System.Collections.Generic;
using Contracts;
using Contracts.Elements;
using Xunit;

internal static class DocumentAssertions
{
    public static void Equal_legacy(Document? actual, Protocol expectedProtocol, IReadOnlyList<Element> expectedElements)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedProtocol, actual!.Protocol);
        Assert.Equal(expectedElements.Count, actual.Elements.Count);

        for (var index = 0; index < expectedElements.Count; index++)
        {
            Assert.Equal(expectedElements[index], actual.Elements[index]);
        }
    }

    public static void Equal(Document? actual, Protocol expectedProtocol, IReadOnlyList<Element> expectedElements)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedProtocol, actual!.Protocol);
        Assert.Equal(expectedElements.Count, actual.Elements.Count);

        for (var index = 0; index < expectedElements.Count; index++)
        {
            var expected = expectedElements[index];
            var actualElement = actual.Elements[index];

            Assert.Equal(expected.GetType(), actualElement.GetType());

            switch (expected)
            {
                case PrinterError expectedError:
                    var actualError = Assert.IsType<PrinterError>(actualElement);
                    Assert.Equal(expectedError.Sequence, actualError.Sequence);
                    // Ignore expectedError.Message
                    break;

                default:
                    // Fall back to full equality
                    Assert.Equal(expected, actualElement);
                    break;
            }
        }
    }

}
