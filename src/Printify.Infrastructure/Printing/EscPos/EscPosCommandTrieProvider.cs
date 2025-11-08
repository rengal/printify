namespace Printify.Infrastructure.Printing.EscPos;

using System;
using System.Collections.Generic;
using Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Builds the ESC/POS command trie once and keeps it immutable for reuse.
/// </summary>
public sealed class EscPosCommandTrieProvider : IEscPosCommandTrieProvider
{
    public EscPosCommandTrieProvider(IEnumerable<ICommandDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        Root = Build(descriptors);
    }

    public EscPosCommandTrieNode Root { get; }

    private static EscPosCommandTrieNode Build(IEnumerable<ICommandDescriptor> descriptors)
    {
        var root = new MutableNode();
        // Preload every descriptor so the trie snapshot is complete before first use.
        foreach (var descriptor in descriptors)
        {
            AddDescriptor(root, descriptor);
        }

        return Freeze(root);
    }

    private static void AddDescriptor(MutableNode root, ICommandDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (descriptor.Prefix.IsEmpty)
        {
            throw new InvalidOperationException("Descriptor prefix cannot be empty.");
        }

        var current = root;
        var visited = new List<MutableNode> { current };

        foreach (var value in descriptor.Prefix.Span)
        {
            // Ensure every byte along the prefix path has a node so commands can share prefixes.
            if (!current.Children.TryGetValue(value, out var next))
            {
                next = new MutableNode();
                current.Children[value] = next;
            }

            current = next;
            visited.Add(current);
        }

        current.Descriptors.Add(descriptor);

        foreach (var node in visited)
        {
            // Mark all ancestors so the parser knows more data may match deeper in the tree.
            node.HasDescriptorInSubtree = true;
        }
    }

    private static EscPosCommandTrieNode Freeze(MutableNode node)
    {
        var frozenChildren = new Dictionary<byte, EscPosCommandTrieNode>(node.Children.Count);
        foreach (var child in node.Children)
        {
            // Recursively convert mutable nodes into immutable ones.
            frozenChildren[child.Key] = Freeze(child.Value);
        }

        // Share empty descriptor arrays to avoid unnecessary allocations per node.
        var descriptors = node.Descriptors.Count == 0
            ? Array.Empty<ICommandDescriptor>()
            : node.Descriptors.ToArray();

        return new EscPosCommandTrieNode(frozenChildren, descriptors, node.HasDescriptorInSubtree);
    }

    private sealed class MutableNode
    {
        public Dictionary<byte, MutableNode> Children { get; } = new();
        public List<ICommandDescriptor> Descriptors { get; } = new();
        public bool HasDescriptorInSubtree { get; set; }
    }
}
