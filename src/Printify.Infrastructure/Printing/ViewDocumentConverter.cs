using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;
using Printify.Domain.Documents.View;
using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.Epl;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Service that delegates view document conversion to protocol-specific converters.
/// </summary>
public sealed class ViewDocumentConversionService : IViewDocumentConverter
{
    private readonly Dictionary<Protocol, IViewDocumentConverter> converters;

    public ViewDocumentConversionService(
        EscPosViewDocumentConverter escPosConverter,
        EplViewDocumentConverter eplConverter)
    {
        ArgumentNullException.ThrowIfNull(escPosConverter);
        ArgumentNullException.ThrowIfNull(eplConverter);

        converters = new Dictionary<Protocol, IViewDocumentConverter>
        {
            [Protocol.EscPos] = escPosConverter,
            [Protocol.Epl] = eplConverter
        };
    }

    public ViewDocument ToViewDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (converters.TryGetValue(document.Protocol, out var converter))
        {
            return converter.ToViewDocument(document);
        }

        throw new BadRequestException(
            $"View conversion is not supported for protocol {document.Protocol}.");
    }
}
