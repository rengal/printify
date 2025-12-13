using Printify.Domain.Documents.Elements;
using Printify.Domain.Mapping;
using Printify.Domain.Printers;
using Printify.Web.Contracts.Documents.Responses;
using Printify.Web.Contracts.Documents.Responses.Elements;
using Printify.Web.Mapping;
using Xunit;
using PrinterError = Printify.Domain.Documents.Elements.PrinterError;

namespace Printify.Tests.Shared.Document;

public static class DocumentAssertions
{
    public static void Equal(IReadOnlyList<ResponseElementDto> expectedElements, IReadOnlyList<ResponseElementDto> actualElements)
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
                case RasterImageDto expectedRasterImage:
                    var actualRasterImage = Assert.IsType<RasterImageDto>(actualElement);
                    Assert.Equal(expectedRasterImage.Width, actualRasterImage.Width);
                    Assert.Equal(expectedRasterImage.Height, actualRasterImage.Height);
                    Assert.Equal(expectedRasterImage.Media.ContentType, actualRasterImage.Media.ContentType);
                    Assert.True(!string.IsNullOrEmpty(actualRasterImage.Media.Sha256));
                    Assert.True(!string.IsNullOrEmpty(actualRasterImage.Media.Url));
                    Assert.Equal(expectedRasterImage.Media.Length, actualRasterImage.Media.Length);
                    break;
                default:
                    Assert.Equal(expected, actualElement);
                    break;
            }
        }
    }

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

    public static void Equal(IReadOnlyList<Element> expectedElements, Protocol expectedProtocol, Domain.Documents.Document? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedProtocol, actual.Protocol);

        Equal(expectedElements, actual.Elements.ToList());
    }

    public static void Equal(IReadOnlyList<Element> expectedElements, Protocol expectedProtocol, DocumentDto? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(DomainMapper.ToString(expectedProtocol), actual.Protocol);

        Equal(expectedElements.Select(DocumentMapper.ToResponseElement).ToList(),
            actual.Elements.ToList());
    }
    /*
    public static void Equal(Domain.Documents.Document expected, DocumentDto actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        var expectedDocumentDto = DocumentMapper.ToResponseDto(expected);

        Equal(expectedDocumentDto, actual);
    }*/
}
