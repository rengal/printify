using Printify.Domain.Documents.Elements;
using Printify.Domain.Mapping;
using Printify.Domain.Printers;
using Printify.Web.Contracts.Documents.Responses;
using Printify.Web.Contracts.Documents.Responses.Elements;
using Printify.Web.Mapping;
using Xunit;
using PrinterError = Printify.Domain.Documents.Elements.PrinterError;
using System.Linq;

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
                case PrintBarcodeDto expectedBarcode:
                    var actualBarcode = Assert.IsType<PrintBarcodeDto>(actualElement);
                    Assert.Equal(expectedBarcode.Symbology, actualBarcode.Symbology);
                    Assert.NotNull(actualBarcode.Media);
                    Assert.False(string.IsNullOrWhiteSpace(actualBarcode.Media.Sha256));
                    Assert.False(string.IsNullOrWhiteSpace(actualBarcode.Media.Url));
                    Assert.True(actualBarcode.Media.Length > 0);
                    break;
                case PrintQrCodeDto expectedQr:
                    var actualQr = Assert.IsType<PrintQrCodeDto>(actualElement);
                    Assert.Equal(expectedQr.Data, actualQr.Data);
                    Assert.NotNull(actualQr.Media);
                    Assert.False(string.IsNullOrWhiteSpace(actualQr.Media.Sha256));
                    Assert.False(string.IsNullOrWhiteSpace(actualQr.Media.Url));
                    Assert.True(actualQr.Media.Length > 0);
                    break;
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
                case PrintBarcode expectedBarcode:
                    var actualBarcode = Assert.IsType<PrintBarcode>(actualElement);
                    Assert.Equal(expectedBarcode.Symbology, actualBarcode.Symbology);
                    Assert.True(actualBarcode.Width > 0);
                    Assert.True(actualBarcode.Height > 0);
                    Assert.NotNull(actualBarcode.Media);
                    Assert.False(string.IsNullOrWhiteSpace(actualBarcode.Media.ContentType));
                    Assert.True(actualBarcode.Media.Length > 0);
                    Assert.False(string.IsNullOrWhiteSpace(actualBarcode.Media.Sha256Checksum));
                    Assert.False(string.IsNullOrWhiteSpace(actualBarcode.Media.Url));
                    break;
                case PrintQrCode expectedQr:
                    var actualQr = Assert.IsType<PrintQrCode>(actualElement);
                    Assert.Equal(expectedQr.Data, actualQr.Data);
                    Assert.True(actualQr.Width > 0);
                    Assert.True(actualQr.Height > 0);
                    Assert.NotNull(actualQr.Media);
                    Assert.False(string.IsNullOrWhiteSpace(actualQr.Media.ContentType));
                    Assert.True(actualQr.Media.Length > 0);
                    Assert.False(string.IsNullOrWhiteSpace(actualQr.Media.Sha256Checksum));
                    Assert.False(string.IsNullOrWhiteSpace(actualQr.Media.Url));
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
