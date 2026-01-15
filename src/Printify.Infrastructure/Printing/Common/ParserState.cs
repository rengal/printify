namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// Unified parser state that contains all parsing state for a printer protocol.
/// This includes mode, buffers, trie navigation, and protocol-specific device context.
/// </summary>
/// <typeparam name="TDeviceContext">The device context type for this protocol.</typeparam>
public sealed class ParserState<TDeviceContext>
    where TDeviceContext : IDeviceContext
{
    /// <summary>
    /// Current parser mode (Text, Command, or Error).
    /// </summary>
    public ParserMode Mode { get; set; }

    /// <summary>
    /// Buffer for accumulating bytes during parsing.
    /// The semantic meaning depends on the current mode:
    /// - Text mode: accumulated text bytes
    /// - Command mode: command bytes being parsed
    /// - Error mode: unused (errors go to UnrecognizedBuffer)
    /// </summary>
    public List<byte> Buffer { get; } = new();

    /// <summary>
    /// Buffer for unrecognized/error bytes that couldn't be parsed.
    /// </summary>
    public List<byte> UnrecognizedBuffer { get; } = new();

    /// <summary>
    /// Trie navigation state for tracking position in the command trie.
    /// This represents WHERE we are in the command tree, not WHAT mode we're in.
    /// </summary>
    public TrieNavigationState TrieNavigation { get; }

    /// <summary>
    /// Device context containing protocol-specific state like encoding,
    /// label dimensions, print settings, etc.
    /// </summary>
    public TDeviceContext DeviceContext { get; }

    /// <summary>
    /// Initializes a new parser state with the specified device context and trie root.
    /// </summary>
    /// <param name="deviceContext">The device context for this protocol.</param>
    /// <param name="trieRoot">The root node of the command trie.</param>
    public ParserState(TDeviceContext deviceContext, CommandTrieNode trieRoot)
    {
        DeviceContext = deviceContext;
        TrieNavigation = new TrieNavigationState(trieRoot);
    }

    /// <summary>
    /// Resets the parser state to initial conditions.
    /// </summary>
    /// <param name="defaultMode">The default mode for this protocol (Command for EPL, Text for ESC/POS).</param>
    public void Reset(ParserMode defaultMode)
    {
        Mode = defaultMode;
        Buffer.Clear();
        UnrecognizedBuffer.Clear();
        TrieNavigation.Reset();
    }
}
