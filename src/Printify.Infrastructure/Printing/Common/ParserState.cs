using System.Text;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// Common parser state shared by all protocol parsers.
/// Contains properties for mode management, buffering, and encoding.
/// </summary>
/// <typeparam name="TState">The specific parser state type.</typeparam>
public abstract class ParserState<TState> where TState : class
{
    /// <summary>
    /// Command-specific state for trie navigation and parsing.
    /// </summary>
    public abstract ICommandState<TState> CommandState { get; }

    /// <summary>
    /// Current parser mode.
    /// </summary>
    public ParserMode Mode { get; set; }

    /// <summary>
    /// Buffer for accumulating bytes during parsing.
    /// </summary>
    public List<byte> Buffer { get; } = new();

    /// <summary>
    /// Buffer for unrecognized/error bytes.
    /// </summary>
    public List<byte> UnrecognizedBuffer { get; } = new();

    /// <summary>
    /// Current encoding for text interpretation.
    /// </summary>
    public Encoding Encoding { get; set; }

    /// <summary>
    /// Initializes a new instance with the specified encoding.
    /// </summary>
    protected ParserState(Encoding encoding)
    {
        Encoding = encoding;
    }

    /// <summary>
    /// Resets the parser state to defaults.
    /// </summary>
    public virtual void Reset()
    {
        Mode = ParserMode.Command;
        CommandState.Reset();
        Buffer.Clear();
        UnrecognizedBuffer.Clear();
    }
}

/// <summary>
/// Interface for command state used by parsers.
/// </summary>
/// <typeparam name="TState">The parser state type.</typeparam>
public interface ICommandState<TState> where TState : class
{
    /// <summary>
    /// Minimum command length.
    /// </summary>
    int? MinLength { get; set; }

    /// <summary>
    /// Exact command length if known.
    /// </summary>
    int? ExactLength { get; set; }

    /// <summary>
    /// Current trie node during command parsing.
    /// </summary>
    CommandTrieNode<TState> CurrentNode { get; set; }

    /// <summary>
    /// Resets the command state.
    /// </summary>
    void Reset();
}
