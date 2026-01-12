using System.Linq;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.EscPos;

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
/// Type alias for the generic command trie node specialized for ESC/POS parser state.
/// </summary>
public sealed class EscPosCommandTrieNode : CommandTrieNode<ParserState>
{
    internal EscPosCommandTrieNode(
        IReadOnlyDictionary<byte, EscPosCommandTrieNode> children,
        CommandDescriptors.ICommandDescriptor? descriptor,
        bool isLeaf)
        : base(
            ConvertChildren(children),
            descriptor,
            isLeaf)
    {
    }

    private static IReadOnlyDictionary<byte, CommandTrieNode<ParserState>> ConvertChildren(
        IReadOnlyDictionary<byte, EscPosCommandTrieNode> children)
    {
        var result = new Dictionary<byte, CommandTrieNode<ParserState>>(children.Count);
        foreach (var kvp in children)
        {
            result[kvp.Key] = kvp.Value;
        }
        return result;
    }

    /// <summary>
    /// Gets the ESC/POS-specific descriptor for this node.
    /// </summary>
    public new CommandDescriptors.ICommandDescriptor? Descriptor =>
        (CommandDescriptors.ICommandDescriptor?)base.Descriptor;
}
