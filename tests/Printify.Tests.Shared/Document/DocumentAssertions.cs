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
                case PrinterError:
                    _ = Assert.IsType<PrinterError>(actualElement);
                    break;
                case RasterImageUpload expectedRaster:
                    var actualRaster = Assert.IsType<RasterImageUpload>(actualElement);
                    Assert.Equal(expectedRaster.Width, actualRaster.Width);
                    Assert.Equal(expectedRaster.Height, actualRaster.Height);
                    Assert.Equal(expectedRaster.Media.ContentType, actualRaster.Media.ContentType);
                    Assert.True(actualRaster.Media.Content.Length > 0);
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
