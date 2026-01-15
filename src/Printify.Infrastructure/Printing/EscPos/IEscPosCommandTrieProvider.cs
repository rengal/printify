using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.EscPos;

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
}
