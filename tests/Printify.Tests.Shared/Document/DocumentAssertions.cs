using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Xunit;

namespace Printify.Tests.Shared.Document;

public static class DocumentAssertions
{
    public static void Equal(IReadOnlyList<Element> expectedElements, IReadOnlyList<Element> actualElements)
    {
        Assert.NotNull(expectedElements);
        Assert.Equal(expectedElements.Count, actualElements.Count);

        for (var index = 0; index < expectedElements.Count; index++)
        {
            var expected = expectedElements[index];
            var actualElement = actualElements[index];

            Assert.Equal(expected.GetType(), actualElement.GetType());

            switch (expected)
            {
                case PrinterError expectedError:
                    var actualError = Assert.IsType<PrinterError>(actualElement);
                    break;
                default:
                    Assert.Equal(expected, actualElement);
                    break;
            }
        }
    }

    public static void Equal(Domain.Documents.Document? actual, Protocol expectedProtocol, IReadOnlyList<Element> expectedElements)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedProtocol, actual.Protocol);

        Equal(expectedElements, actual.Elements.ToList());
    }
}