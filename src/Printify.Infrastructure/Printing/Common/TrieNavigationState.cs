namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// State for trie navigation during command parsing.
/// Tracks the current position in the command trie and parsing metadata.
/// This is NOT the parser mode - it's specifically the navigation state within the command trie.
/// </summary>
public sealed class TrieNavigationState
{
    /// <summary>
    /// Minimum command length (from descriptor).
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Exact command length if determinable from parsed bytes.
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
