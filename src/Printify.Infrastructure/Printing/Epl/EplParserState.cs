using System.Text;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.Epl;

/// <summary>
/// Command state for EPL protocol parsing.
/// </summary>
public sealed class EplCommandState : ICommandState<EplParserState>
{
    public int? MinLength { get; set; }
    public int? ExactLength { get; set; }
    public CommandTrieNode<EplParserState> CurrentNode { get; set; }
    private readonly CommandTrieNode<EplParserState> root;

    public EplCommandState(CommandTrieNode<EplParserState> root)
    {
        this.root = root;
        CurrentNode = root;
        Reset();
    }

    public void Reset()
    {
        MinLength = null;
        ExactLength = null;
        CurrentNode = root;
    }
}

/// <summary>
/// Parser state for EPL protocol.
/// </summary>
public sealed class EplParserState : ParserState<EplParserState>
{
    /// <summary>
    /// Label width in dots (set by q command).
    /// </summary>
    public int LabelWidth { get; set; } = 500;

    /// <summary>
    /// Label height in dots (set by Q command).
    /// </summary>
    public int LabelHeight { get; set; } = 300;

    /// <summary>
    /// Print speed (set by R command).
    /// </summary>
    public int PrintSpeed { get; set; } = 2;

    /// <summary>
    /// Print darkness (set by S command).
    /// </summary>
    public int PrintDarkness { get; set; } = 10;

    /// <summary>
    /// Command state for trie navigation.
    /// </summary>
    public EplCommandState EplCommandState { get; }

    /// <summary>
    /// Overrides the CommandState property to return EPL-specific command state.
    /// </summary>
    public override ICommandState<EplParserState> CommandState => EplCommandState;

    public EplParserState(CommandTrieNode<EplParserState> root)
        : base(Encoding.GetEncoding(437))
    {
        EplCommandState = new EplCommandState(root);
        Mode = ParserMode.Command; // EPL starts in Command mode (no Text mode)
    }

    /// <summary>
    /// Resets the state to defaults.
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        LabelWidth = 500;
        LabelHeight = 300;
        PrintSpeed = 2;
        PrintDarkness = 10;
        Mode = ParserMode.Command;
    }
}
