namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// Immutable trie node that stores command descriptors for a specific byte prefix.
/// Used for efficient command matching in printer protocol parsers.
/// </summary>
/// <typeparam name="TState">The parser state type for this protocol.</typeparam>
public class CommandTrieNode<TState>
    where TState : class
{
    internal CommandTrieNode(
        IReadOnlyDictionary<byte, CommandTrieNode<TState>> children,
        ICommandDescriptor<TState>? descriptor,
        bool isLeaf)
    {
        Children = children;
        Descriptor = descriptor;
        IsLeaf = isLeaf;
    }

    /// <summary>
    /// Gets the child nodes for this trie node, keyed by the next byte in the command prefix.
    /// </summary>
    public IReadOnlyDictionary<byte, CommandTrieNode<TState>> Children { get; }

    /// <summary>
    /// The command descriptor associated with this node (only for leaf nodes).
    /// </summary>
    public ICommandDescriptor<TState>? Descriptor { get; }

    /// <summary>
    /// Gets a value indicating whether this node is a leaf (has a descriptor).
    /// </summary>
    public bool IsLeaf { get; }
}

/// <summary>
/// Extension methods for building command tries.
/// </summary>
public static class CommandTrieNodeExtensions
{
    /// <summary>
    /// Adds a command descriptor to the trie.
    /// </summary>
    public static void AddCommand<TState>(
        this CommandTrieNode<TState> root,
        ICommandDescriptor<TState> descriptor)
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (descriptor.Prefix.IsEmpty)
            throw new InvalidOperationException("Prefix must not be empty");

        // Note: This is a simplified version. The actual implementation
        // should use mutable nodes during building and freeze at the end.
        // See EplCommandTrieProvider or EscPosCommandTrieProvider for full implementation.
        throw new NotImplementedException("Use CommandTrieBuilder<TState> to build tries.");
    }
}
