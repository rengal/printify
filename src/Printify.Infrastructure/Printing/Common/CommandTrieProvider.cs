namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// Base class for command trie providers that build and maintain
/// immutable command tries for protocol parsers.
/// </summary>
/// <typeparam name="TState">The parser state type.</typeparam>
/// <typeparam name="TDescriptor">The command descriptor type.</typeparam>
public abstract class CommandTrieProvider<TState, TDescriptor>
    where TState : class
    where TDescriptor : ICommandDescriptor<TState>
{
    /// <summary>
    /// Gets all registered command descriptors.
    /// </summary>
    protected abstract IEnumerable<TDescriptor> AllDescriptors { get; }

    /// <summary>
    /// Gets the root node of the immutable trie that contains all registered descriptors.
    /// </summary>
    public CommandTrieNode<TState> Root { get; }

    /// <summary>
    /// Initializes a new instance and builds the command trie.
    /// </summary>
    protected CommandTrieProvider()
    {
        Root = Build(AllDescriptors);
    }

    /// <summary>
    /// Builds the immutable command trie from the provided descriptors.
    /// </summary>
    private CommandTrieNode<TState> Build(IEnumerable<TDescriptor> descriptors)
    {
        var root = new MutableNode();

        foreach (var descriptor in descriptors)
        {
            if (descriptor.Prefix.IsEmpty)
                throw new InvalidOperationException("Prefix must not be empty");
            AddDescriptor(root, descriptor);
        }

        return Freeze(root);
    }

    private static void AddDescriptor(MutableNode root, TDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var current = root;

        foreach (var value in descriptor.Prefix.Span)
        {
            if (!current.Children.TryGetValue(value, out var next))
            {
                next = new MutableNode();
                current.Children[value] = next;
            }

            current = next;
        }

        // Check for contradiction: node has children (not a leaf) but also has a descriptor
        // OR node already has a descriptor
        if (current.Descriptor != null)
        {
            throw new InvalidOperationException(
                $"Two descriptors have the same prefix: " +
                $"{current.Descriptor.GetType().Name} and {descriptor.GetType().Name}");
        }

        if (current.Children.Count > 0)
        {
            throw new InvalidOperationException(
                $"Descriptor {descriptor.GetType().Name} creates a contradiction: " +
                $"node at prefix {Convert.ToHexString(descriptor.Prefix.Span)} has children but also needs a descriptor. " +
                $"This means another descriptor has a longer prefix that extends this one.");
        }

        current.Descriptor = descriptor;
    }

    private static CommandTrieNode<TState> Freeze(MutableNode node)
    {
        var frozenChildren = new Dictionary<byte, CommandTrieNode<TState>>(node.Children.Count);
        var isLeaf = node.Children.Count == 0;

        // Validate trie invariant: leaf nodes must have a descriptor, non-leaf nodes must not
        if (isLeaf && node.Descriptor == null)
        {
            throw new InvalidOperationException(
                "Internal error: leaf node without descriptor found during trie construction");
        }

        if (!isLeaf && node.Descriptor != null)
        {
            throw new InvalidOperationException(
                $"Contradiction in trie: non-leaf node has descriptor {node.Descriptor.GetType().Name}. " +
                $"This means another command extends this command's prefix.");
        }

        foreach (var child in node.Children)
        {
            var frozenChild = Freeze(child.Value);
            frozenChildren[child.Key] = frozenChild;
        }

        return new CommandTrieNode<TState>(frozenChildren, node.Descriptor, isLeaf);
    }

    private sealed class MutableNode
    {
        public Dictionary<byte, MutableNode> Children { get; } = new();
        public TDescriptor? Descriptor { get; set; }
    }
}
