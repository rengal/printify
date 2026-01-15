namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// Immutable trie node that stores command descriptors for a specific byte prefix.
/// Used for efficient command matching in printer protocol parsers.
/// </summary>
public class CommandTrieNode
{
    internal CommandTrieNode(
        IReadOnlyDictionary<byte, CommandTrieNode> children,
        ICommandDescriptor? descriptor,
        bool isLeaf)
    {
        Children = children;
        Descriptor = descriptor;
        IsLeaf = isLeaf;
    }

    /// <summary>
    /// Gets the child nodes for this trie node, keyed by the next byte in the command prefix.
    /// </summary>
    public IReadOnlyDictionary<byte, CommandTrieNode> Children { get; }

    /// <summary>
    /// The command descriptor associated with this node (only for leaf nodes).
    /// </summary>
    public ICommandDescriptor? Descriptor { get; }

    /// <summary>
    /// Gets a value indicating whether this node is a leaf (has a descriptor).
    /// </summary>
    public bool IsLeaf { get; }
}
