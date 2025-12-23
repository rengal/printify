namespace Printify.Infrastructure.Printing.EscPos;

using System.Collections.Generic;
using CommandDescriptors;

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
/// Immutable trie node that stores ESC/POS command descriptor for a specific byte prefix.
/// <para>
/// The trie is built from the fixed-length prefixes of ESC/POS commands. Each path from root
/// to a node with a non-empty <see cref="Descriptors"/> collection represents one complete command prefix.
/// </para>
/// <para>
/// Example trie structure for common ESC/POS commands:
/// <code>
/// Root
/// ├─ ESC (0x1B)
/// │  ├─ @ (0x40) [Initialize printer]
/// │  ├─ ! (0x21) [Select print mode]
/// │  ├─ d (0x64) [Print and feed n lines]
/// │  └─ J (0x4A) [Print and feed paper]
/// ├─ GS (0x1D)
/// │  ├─ V (0x56) [Cut paper - handles variable args internally]
/// │  ├─ ! (0x21) [Select character size]
/// │  └─ ( (0x28)
/// │     ├─ k (0x6B) [Graphics commands with length fields]
/// │     └─ L (0x4C) [More graphics commands]
/// └─ DLE (0x10)
///    └─ EOT (0x04)
///       └─ n (0x01-0x04) [Real-time status request]
/// </code>
/// </para>
/// <para>
/// Note: Commands with variable-length arguments (e.g., GS V can be followed by mode byte m,
/// or mode byte m and additional parameter n) are represented by a single prefix node (GS V).
/// The associated <see cref="ICommandDescriptor"/> is responsible for parsing all argument
/// variations via its <see cref="ICommandDescriptor.TryGetExactLength"/> and 
/// <see cref="ICommandDescriptor.TryParse"/> methods.
/// </para>
/// <para>
/// Intermediate nodes (e.g., ESC, GS, DLE, GS (0x28)) have empty <see cref="Descriptors"/>
/// because they represent incomplete prefixes, not valid commands on their own.
/// Only nodes at the end of a valid command prefix path contain a descriptor.
/// </para>
/// </summary>
public sealed class EscPosCommandTrieNode
{
    internal EscPosCommandTrieNode(
        IReadOnlyDictionary<byte, EscPosCommandTrieNode> children,
        IReadOnlyList<ICommandDescriptor> descriptors,
        bool isLeaf)
    {
        Children = children;
        Descriptors = descriptors;
        IsLeaf = isLeaf;
    }

    /// <summary>
    /// Gets the child nodes for this trie node, keyed by the next byte in the ESC/POS command prefix.
    /// Each level in the trie represents a single additional byte in the prefix sequence.
    /// </summary>
    public IReadOnlyDictionary<byte, EscPosCommandTrieNode> Children { get; }

    /// <summary>
    /// The command descriptors associated with this node.
    /// </summary>
    public IReadOnlyList<ICommandDescriptor> Descriptors { get; }

    /// <summary>
    /// Gets a value indicating whether this node or any of its descendants contains a command descriptor.
    /// </summary>
    public bool IsLeaf { get; }
}
