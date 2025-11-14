using Printify.Domain.Documents.Elements;
using System.Runtime.InteropServices;
using System.Text;

namespace Printify.Infrastructure.Printing.EscPos;

public sealed class EscPosParser
{
    private readonly EscPosCommandTrieNode root;
    private readonly ParserState state;
    private readonly Action<Element> onElement;

    public EscPosParser(IEscPosCommandTrieProvider trieProvider, Action<Element> onElement)
    {
        ArgumentNullException.ThrowIfNull(trieProvider);
        root = trieProvider.Root;
        this.onElement = onElement;
        state = new ParserState(root); 
    }

    private void EmitPendingElement()
    {
        if (state.Pending != null)
            Emit(state.Pending.Value.length, state.Pending.Value.element);
    }

    private void Emit(int count, Element? element)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");

        if (element != null)
        {
            if (element is SetCodePage setCodePage)
            {
                Encoding encoding;
                try
                {
                    encoding = Encoding.GetEncoding(setCodePage.CodePage);
                }
                catch
                {
                    encoding = Encoding.GetEncoding("437");
                }
                state.Encoding = encoding;
            }

            onElement.Invoke(element);
        }

        state.Pending = null;

        if (state.Buffer.Count >= count)
            state.Buffer.RemoveRange(0, count);
        else
            state.Buffer.Clear();
    }

    private void CheckEmitAsError()
    {
        if (state.Buffer.Count <= 0)
            return;

        var element = new PrinterError($"Unrecognized {state.Buffer.Count} bytes");
        onElement.Invoke(element);
        state.Pending = null;
        state.Buffer.Clear();
    }

    private bool TryNavigateChild(byte value)
    {
        if (state.CurrentNode.Children.TryGetValue(value, out var nextNode))
        {
            Navigate(nextNode);
            return true;
        }

        return false;
    }

    private void Navigate(EscPosCommandTrieNode nextNode)
    {
        state.CurrentNode = nextNode;
        state.MinLength = null;
        state.ExactLength = null;
    }

    public void Feed(ReadOnlySpan<byte> buffer, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var value in buffer)
            Feed(value, ct);
    }

    public void Feed(byte value, CancellationToken ct)
    {
        state.Buffer.Add(value);

        // Try to navigate deeper
        if (!state.CurrentNode.IsLeaf)
        {
            if (!TryNavigateChild(value))
            {
                if (state.CurrentNode != root)
                {
                    CheckEmitAsError();
                    Navigate(root);
                    return;
                }
            }
        }

        var descriptor = state.CurrentNode.Descriptor;

        // Process current trie node
        if (descriptor == null)
            return;

        state.MinLength ??= descriptor.MinLength;

        if (state is { MinLength: not null, ExactLength: null } && state.Buffer.Count >= state.MinLength.Value)
        {
            var exactLength = descriptor.TryGetExactLength(CollectionsMarshal.AsSpan(state.Buffer));
            if (exactLength.HasValue)
                state.ExactLength = exactLength.Value;
        }

        // If exact length is known and met, try to parse
        if (state.ExactLength.HasValue && state.Buffer.Count >= state.ExactLength.Value)
        {
            var result = descriptor.TryParse(CollectionsMarshal.AsSpan(state.Buffer), state);
            
            if (result.Kind == MatchKind.Matched)
            {
                if (result.BytesConsumed > 0)
                {
                    EmitPendingElement();
                    Emit(result.BytesConsumed, result.Element);
                }
            }
            else
            {
                CheckEmitAsError();
            }

            Navigate(root);
            return;
        }

        // If exact length is not known, but buffer length meets/exceeds MinLength, try to parse
        if (!state.ExactLength.HasValue && state.MinLength.HasValue && state.Buffer.Count >= state.MinLength.Value)
        {
            var result = descriptor.TryParse(CollectionsMarshal.AsSpan(state.Buffer), state);

            if (result.Kind == MatchKind.Matched)
            {
                if (result.Element != null)
                    Emit(result.BytesConsumed, result.Element);
                Navigate(root);
            }
            else if (result.Kind == MatchKind.MatchedPending)
            {
                state.Pending = (result.BytesConsumed, result.Element!);
            }
            else if (result.Kind == MatchKind.NeedMore)
            {
                // Continue accumulating
            }
            else if (result.Kind == MatchKind.NoMatch)
            {
                EmitPendingElement();
                CheckEmitAsError();
                Navigate(root);
            }
        }
    }

    /// <summary>
    /// Signals the end of input and flushes any pending data.
    /// Call this when no more bytes will be fed to the parser.
    /// </summary>
    public void Complete()
    {
        // Emit any pending element first
        EmitPendingElement();
        
        // If there's leftover data in the buffer, treat it as an error
        CheckEmitAsError();
        
        // Reset to initial state
        Navigate(root);
    }
}
