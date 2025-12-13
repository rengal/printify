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
                case RasterImageUpload expectedRasterImageUpload:
                    var actualRasterImageUpload = Assert.IsType<RasterImageUpload>(actualElement);
                    Assert.Equal(expectedRasterImageUpload.Width, actualRasterImageUpload.Width);
                    Assert.Equal(expectedRasterImageUpload.Height, actualRasterImageUpload.Height);
                    Assert.Equal(expectedRasterImageUpload.Media.ContentType, actualRasterImageUpload.Media.ContentType);
                    Assert.True(actualRasterImageUpload.Media.Content.Length > 0);
                    break;
                case RasterImage expectedRasterImage:
                    var actualRasterImage = Assert.IsType<RasterImage>(actualElement);
                    Assert.Equal(expectedRasterImage.Width, actualRasterImage.Width);
                    Assert.Equal(expectedRasterImage.Height, actualRasterImage.Height);
                    Assert.Equal(expectedRasterImage.Media.ContentType, actualRasterImage.Media.ContentType);
                    Assert.NotEmpty(actualRasterImage.Media.Sha256Checksum);
                    Assert.NotEmpty(actualRasterImage.Media.Url);
                    Assert.Equal(expectedRasterImage.Media.Length, actualRasterImage.Media.Length);
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
