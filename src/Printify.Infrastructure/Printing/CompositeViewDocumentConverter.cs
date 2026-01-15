using Printify.Application.Exceptions;
using Printify.Application.Features.Printers.Documents.View;
using Printify.Domain.Documents;
using Printify.Domain.Documents.View;
using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.Epl;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Composite view document converter that delegates to protocol-specific converters.
/// </summary>
public sealed class CompositeViewDocumentConverter : IViewDocumentConverter
{
    private readonly Dictionary<Protocol, IViewDocumentConverter> _converters;

    public CompositeViewDocumentConverter(
        EscPosViewDocumentConverter escPosConverter,
        EplViewDocumentConverter eplConverter)
    {
        ArgumentNullException.ThrowIfNull(escPosConverter);
        ArgumentNullException.ThrowIfNull(eplConverter);

        _converters = new Dictionary<Protocol, IViewDocumentConverter>
        {
            [Protocol.EscPos] = escPosConverter,
            [Protocol.Epl] = eplConverter
        };
    }

    public ViewDocument ToViewDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_converters.TryGetValue(document.Protocol, out var converter))
        {
            return converter.ToViewDocument(document);
        }

        throw new BadRequestException(
            $"View conversion is not supported for protocol {document.Protocol}.");
    }
}
