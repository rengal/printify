using Printify.Domain.Documents.View;
using Printify.Domain.Mapping;
using Printify.Web.Contracts.Documents.Responses.View;
using ViewElements = Printify.Web.Contracts.Documents.Responses.View.Elements;

namespace Printify.Web.Mapping;

internal static class ViewDocumentMapper
{
    internal static ViewDocumentDto ToViewResponseDto(ViewDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var viewElements = document.Elements?
            .Select(ToViewElementDto)
            .ToList()
            ?? new List<ViewElements.ViewElementDto>();

        return new ViewDocumentDto(
            document.Id,
            document.PrintJobId,
            document.PrinterId,
            document.Timestamp,
            DomainMapper.ToString(document.Protocol),
            document.WidthInDots,
            document.HeightInDots,
            document.ClientAddress,
            viewElements.AsReadOnly());
    }

    private static ViewElements.ViewElementDto ToViewElementDto(ViewElement element)
    {
        return element switch
        {
            ViewTextElement text => WithCommandMetadata(
                new ViewElements.ViewTextElementDto(
                    text.Text,
                    text.X,
                    text.Y,
                    text.Width,
                    text.Height,
                    text.Font,
                    text.CharSpacing,
                    text.IsBold,
                    text.IsUnderline,
                    text.IsReverse)
                {
                    CharScaleX = text.CharScaleX == 1 ? null : text.CharScaleX,
                    CharScaleY = text.CharScaleY == 1 ? null : text.CharScaleY
                },
                text),
            ViewImageElement image => WithCommandMetadata(
                new ViewElements.ViewImageElementDto(
                    ToMediaDto(image.Media),
                    image.X,
                    image.Y,
                    image.Width,
                    image.Height),
                image),
            ViewDebugElement debug => WithCommandMetadata(
                new ViewElements.ViewDebugElementDto(
                    debug.DebugType,
                    debug.Parameters),
                debug),
            _ => throw new NotSupportedException(
                $"View element type '{element.GetType().FullName}' is not supported in responses.")
        };
    }

    private static ViewElements.ViewMediaDto ToMediaDto(ViewMedia media)
    {
        ArgumentNullException.ThrowIfNull(media);

        return new ViewElements.ViewMediaDto(
            media.ContentType,
            media.Length,
            media.Sha256Checksum,
            media.Url);
    }

    private static T WithCommandMetadata<T>(T dto, ViewElement element)
        where T : ViewElements.ViewElementDto
    {
        return dto with
        {
            CommandRaw = element.CommandRaw,
            CommandDescription = element.CommandDescription,
            LengthInBytes = element.LengthInBytes,
            ZIndex = element.ZIndex
        };
    }

}
