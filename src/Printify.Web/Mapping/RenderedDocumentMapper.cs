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

        // Convert all canvases to DTOs
        var canvasDtos = document.Canvases
            .Select(canvas =>
            {
                var items = canvas.Items
                    .Select(ToCanvasElementDto)
                    .ToList();

                return new CanvasDto(
                    canvas.WidthInDots,
                    canvas.HeightInDots,
                    items.AsReadOnly());
            })
            .ToArray();

        // Extract error messages from all canvases
        var allItems = document.Canvases
            .SelectMany(c => c.Items)
            .Select(ToCanvasElementDto)
            .ToList();
        var errorMessages = ExtractErrorMessages(allItems, document.ErrorMessages);

        return new RenderedDocumentDto(
            document.Id,
            document.PrintJobId,
            document.PrinterId,
            document.Timestamp,
            EnumMapper.ToString(document.Protocol),
            canvasDtos,
            document.ClientAddress,
            document.BytesReceived,
            document.BytesSent,
            errorMessages);
    }

    private static string[]? ExtractErrorMessages(List<CanvasElementDto> items, string[]? documentErrorMessages)
    {
        var errorMessages = new List<string>();

        // Add document-level error messages if any
        if (documentErrorMessages is { Length: > 0 })
        {
            errorMessages.AddRange(documentErrorMessages);
        }

        // Extract error messages from debug elements
        foreach (var item in items)
        {
            if (item is CanvasDebugElementDto debug &&
                (debug.DebugType == "error" || debug.DebugType == "printerError") &&
                debug.Parameters is not null &&
                debug.Parameters.TryGetValue("Message", out var message))
            {
                errorMessages.Add(message);
            }
        }

        return errorMessages.Count > 0 ? errorMessages.ToArray() : null;
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
