using Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

namespace Printify.Infrastructure.Printing.EscPos;

public sealed class EscPosParser
{
    private readonly TrieNode root = new();

    public EscPosParser(IEnumerable<ICommandDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        foreach (var descriptor in descriptors)
        {
            AddDescriptor(descriptor);
        }
    }

    public MatchResult Parse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        if (buffer.IsEmpty)
        {
            return MatchResult.NeedMore();
        }

        var node = root;
        var depth = 0;
        TrieNode? matchedNode = null;

        while (depth < buffer.Length && node.Children.TryGetValue(buffer[depth], out var next))
        {
            node = next;
            depth++;
            if (node.HasTerminalDescriptors)
            {
                matchedNode = node;
            }
        }

        if (matchedNode is null)
        {
            return node.HasDescriptorInSubtree ? MatchResult.NeedMore() : MatchResult.NoMatch();
        }

        foreach (var descriptor in matchedNode.Descriptors)
        {
            if (buffer.Length < descriptor.Prefix.Length)
            {
                return MatchResult.NeedMore();
            }

            if (buffer.Length < descriptor.MinLength)
            {
                return MatchResult.NeedMore();
            }

            var exactLength = descriptor.TryGetExactLength(buffer);
            if (exactLength.HasValue && buffer.Length < exactLength.Value)
            {
                return MatchResult.NeedMore();
            }

            var result = descriptor.TryParse(buffer, state);
            if (result.Kind != MatchKind.NoMatch)
            {
                return result;
            }
        }

        return MatchResult.NoMatch();
    }

    private void AddDescriptor(ICommandDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (descriptor.Prefix.IsEmpty)
        {
            throw new InvalidOperationException("Descriptor prefix cannot be empty.");
        }

        var current = root;
        var visited = new List<TrieNode> { current };
        foreach (var value in descriptor.Prefix.Span)
        {
            if (!current.Children.TryGetValue(value, out var next))
            {
                next = new TrieNode();
                current.Children[value] = next;
            }

            current = next;
            visited.Add(current);
        }

        current.Descriptors.Add(descriptor);

        foreach (var node in visited)
        {
            node.HasDescriptorInSubtree = true;
        }
    }

    private sealed class TrieNode
    {
        public Dictionary<byte, TrieNode> Children { get; } = new();
        public List<ICommandDescriptor> Descriptors { get; } = new();
        public bool HasTerminalDescriptors => Descriptors.Count > 0;
        public bool HasDescriptorInSubtree { get; set; }
    }
}
