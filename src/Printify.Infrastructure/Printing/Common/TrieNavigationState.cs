namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// State for trie navigation during command parsing.
/// Tracks the current position in the command trie and parsing metadata.
/// This is NOT the parser mode - it's specifically the navigation state within the command trie.
/// </summary>
public sealed class TrieNavigationState
{
    /// <summary>
    /// Minimum command length from the descriptor.
    /// The parser will not attempt to parse until at least this many bytes have been received.
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Exact command length if determinable from parsed bytes (e.g., from length fields in the command).
    /// When set, the parser knows exactly how many bytes constitute the complete command.
    /// </summary>
    public int? ExactLength { get; set; }

    /// <summary>
    /// Current trie node during command parsing.
    /// </summary>
    public CommandTrieNode CurrentNode { get; set; }

    private readonly CommandTrieNode root;

    public TrieNavigationState(CommandTrieNode root)
    {
        this.root = root;
        CurrentNode = root;
    }

    /// <summary>
    /// Resets the navigation state to the root node.
    /// </summary>
    public void Reset()
    {
        MinLength = null;
        ExactLength = null;
        CurrentNode = root;
    }
}
