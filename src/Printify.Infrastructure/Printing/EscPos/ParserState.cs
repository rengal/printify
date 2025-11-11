using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos;

public class ParserState
{
    private EscPosCommandTrieNode RootNode { get; }

    public int? MinLength { get; set; }
    public int? ExactLength { get; set; }
    public List<byte> Buffer { get; } = new();

    public (int length, Element element)? Pending { get; set; }
    public EscPosCommandTrieNode CurrentNode { get; set; }

    public ParserState(EscPosCommandTrieNode root)
    {
        Reset();
        RootNode = root;
        CurrentNode = RootNode;
    }

    public void Reset()
    {
        MinLength = null;
        ExactLength = null;
        Buffer.Clear();
        Pending = null;
        CurrentNode = RootNode;
    }
}
