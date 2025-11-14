using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;

namespace Printify.Web.Tests.EscPos;

internal static class DocumentAssertions
{
    public static void Equal(Document? actual, Protocol expectedProtocol, IReadOnlyList<Element> expectedElements)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedProtocol, actual.Protocol);

        var actualElements = actual.Elements.ToList();
        try
        {
            Assert.Equal(expectedElements.Count, actualElements.Count);
        }
        catch (Exception e) //todo debugnow
        {
            Console.WriteLine(e.Message);
            Assert.Equal(expectedElements.Count, actualElements.Count);
        }

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
}