namespace Printify.Infrastructure.Printing.EscPos;

using System.Collections.Generic;
using Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Exposes the immutable ESC/POS command trie so every parser session
/// can reuse the same structure without rebuilding it.
/// </summary>
public interface IEscPosCommandTrieProvider
{
    /// <summary>
    /// Gets the root node of the immutable trie that contains all registered descriptors.
    /// </summary>
    EscPosCommandTrieNode Root { get; }
}

/// <summary>
/// Immutable trie node that stores ESC/POS command descriptors for a specific byte prefix.
/// </summary>
public sealed class EscPosCommandTrieNode
{
    internal EscPosCommandTrieNode(
        IReadOnlyDictionary<byte, EscPosCommandTrieNode> children,
        IReadOnlyList<ICommandDescriptor> descriptors,
        bool hasDescriptorInSubtree)
    {
        Children = children;
        Descriptors = descriptors;
        HasDescriptorInSubtree = hasDescriptorInSubtree;
    }

    public IReadOnlyDictionary<byte, EscPosCommandTrieNode> Children { get; }

    public IReadOnlyList<ICommandDescriptor> Descriptors { get; }

    public bool HasDescriptorInSubtree { get; }

    public bool HasTerminalDescriptors => Descriptors.Count > 0;
}
