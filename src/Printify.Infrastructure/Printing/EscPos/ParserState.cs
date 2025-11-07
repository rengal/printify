using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

namespace Printify.Infrastructure.Printing.EscPos;

public class ParserState
{
    public ICommandDescriptor? CommandDescriptor { get; set; }
    public int? MinLength { get; set; }
    public int? ExactLength { get; set; }
    public List<byte> TextBuffer { get; } = new();

    /// <summary>
    /// Injected by the session so descriptors can materialize domain elements with the correct sequence number.
    /// </summary>
    public Func<Func<int, Element>, Element>? ElementFactory { get; set; }

    /// <summary>
    /// Injected by the session so descriptors can flush buffered text prior to emitting control elements.
    /// </summary>
    public Action<bool>? FlushTextAction { get; set; }

    public Element CreateElement(Func<int, Element> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (ElementFactory is null)
        {
            throw new InvalidOperationException("ElementFactory delegate is not configured.");
        }

        return ElementFactory(builder);
    }

    public void FlushText(bool allowEmpty)
    {
        FlushTextAction?.Invoke(allowEmpty);
    }
}
