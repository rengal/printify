using System.Text;
using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.EscPos;

/// <summary>
/// Command state for ESC/POS protocol parsing.
/// </summary>
public sealed class EscPosCommandState : ICommandState<ParserState>
{
    public int? MinLength { get; set; }
    public int? ExactLength { get; set; }
    public (int length, Element element)? Pending { get; set; }
    public CommandTrieNode<ParserState> CurrentNode { get; set; }
    private readonly CommandTrieNode<ParserState> root;

    public EscPosCommandState(CommandTrieNode<ParserState> root)
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

/// <summary>
/// Parser state for ESC/POS protocol.
/// </summary>
public sealed class ParserState : ParserState<ParserState>
{
    /// <summary>
    /// Command state for trie navigation.
    /// </summary>
    public EscPosCommandState EscPosCommandState { get; }

    /// <summary>
    /// Overrides the CommandState property to return ESC/POS-specific command state.
    /// </summary>
    public override ICommandState<ParserState> CommandState => EscPosCommandState;

    public ParserState(CommandTrieNode<ParserState> root)
        : base(Encoding.GetEncoding("437"))
    {
        EscPosCommandState = new EscPosCommandState(root);
        Mode = ParserMode.Text; // ESC/POS starts in Text mode
    }

    /// <summary>
    /// Resets the state to defaults.
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        Mode = ParserMode.Text;
    }
}
