using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos;

public class ParserState
{
    public int? MinLength { get; set; }
    public int? ExactLength { get; set; }
    public List<byte> TextBuffer { get; } = new();
}
