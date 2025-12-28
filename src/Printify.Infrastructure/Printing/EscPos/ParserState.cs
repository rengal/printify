using System.Text;
using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos;

public enum ParserMode
{
    Text,
    Command,
    Error
}

public class CommandState
{
    public int? MinLength { get; set; }
    public int? ExactLength { get; set; }
    public (int length, Element element)? Pending { get; set; }
    public EscPosCommandTrieNode CurrentNode { get; set; }
    private readonly EscPosCommandTrieNode root;

    public CommandState(EscPosCommandTrieNode root)
    {
        this.root = root;
        CurrentNode = root;
        Reset();
    }

    public void Reset()
    {
        MinLength = null;
        ExactLength = null;
        Pending = null;
        CurrentNode = root;
    }
}

public class ParserState
{
    public CommandState CommandState { get; }
    public ParserMode Mode { get; set; }
    public List<byte> Buffer { get; } = new();
    public List<byte> PendingErrorBuffer { get; } = new();
    public Encoding Encoding { get; set; }

    public ParserState(EscPosCommandTrieNode root)
    {
        CommandState = new CommandState(root);
        Reset();
        Encoding = Encoding.GetEncoding("437");
    }

    public void Reset()
    {
        Mode = ParserMode.Text;
        CommandState.Reset();
        Buffer.Clear();
    }
}
