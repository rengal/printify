using Printify.Domain.Documents;
using Printify.Domain.Layout.Primitives;
using Printify.Infrastructure.Mapping;
using Printify.Web.Contracts.Documents.Responses.Canvas;
using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;

namespace Printify.Web.Mapping;

internal static class RenderedDocumentMapper
{
    internal static RenderedDocumentDto ToCanvasResponseDto(RenderedDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var items = document.Canvas.Items
            .Select(ToCanvasElementDto)
            .ToList();

        var canvasDto = new CanvasDto(
            document.Canvas.WidthInDots,
            document.Canvas.HeightInDots,
            items.AsReadOnly());

        return new RenderedDocumentDto(
            document.Id,
            document.PrintJobId,
            document.PrinterId,
            document.Timestamp,
            DomainMapper.ToString(document.Protocol),
            canvasDto,
            document.ClientAddress,
            document.BytesReceived,
            document.BytesSent,
            document.ErrorMessages);
    }

    private static CanvasElementDto ToCanvasElementDto(BaseElement element)
    {
        return element switch
        {
            TextElement text => WithCommandMetadata(
                new CanvasTextElementDto(
                    text.Text,
                    text.X,
                    text.Y,
                    text.Width,
                    text.Height,
                    text.FontName,
                    text.CharSpacing,
                    text.IsBold,
                    text.IsUnderline,
                    text.IsReverse,
                    text.CharScaleX,
                    text.CharScaleY,
                    RotationMapper.ToDto(text.Rotation)),
                element),
            ImageElement image => WithCommandMetadata(
                new CanvasImageElementDto(
                    ToMediaDto(image.Media),
                    image.X,
                    image.Y,
                    image.Width,
                    image.Height,
                    RotationMapper.ToDto(image.Rotation)),
                element),
            LineElement line => WithCommandMetadata(
                new CanvasLineElementDto(
                    line.X1,
                    line.Y1,
                    line.X2,
                    line.Y2,
                    line.Thickness),
                element),
            BoxElement box => WithCommandMetadata(
                new CanvasBoxElementDto(
                    box.X,
                    box.Y,
                    box.Width,
                    box.Height,
                    box.Thickness),
                element),
            DebugInfo debug => WithCommandMetadata(
                new CanvasDebugElementDto(
                    debug.DebugType,
                    debug.Parameters),
                element),
            _ => throw new NotSupportedException(
                $"Canvas element type '{element.GetType().FullName}' is not supported in responses.")
        };
    }

    private static CanvasMediaDto ToMediaDto(Media media)
    {
        ArgumentNullException.ThrowIfNull(media);

        return new CanvasMediaDto(
            media.MimeType,
            media.Size,
            media.Url,
            media.StorageKey);
    }

    private static T WithCommandMetadata<T>(T dto, BaseElement element)
        where T : CanvasElementDto
    {
        if (element is not DebugInfo debugInfo)
        {
            return dto;
        }

        // Only debug entries carry command raw bytes and descriptions for diagnostics.
        return dto with
        {
            CommandRaw = debugInfo.CommandRaw.Length == 0
                ? string.Empty
                : Convert.ToHexString(debugInfo.CommandRaw),
            LengthInBytes = debugInfo.LengthInBytes,
            CommandDescription = debugInfo.CommandDescription
        };
    }
}
