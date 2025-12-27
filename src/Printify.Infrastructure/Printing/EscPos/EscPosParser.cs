using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Printing.EscPos.CommandDescriptors;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ZXing;

namespace Printify.Infrastructure.Printing.EscPos;

public sealed class EscPosParser
{
    private readonly EscPosCommandTrieNode root;
    private readonly ParserState state;
    private readonly Action<Element> onElement;
    private static readonly Encoding DefaultCodePage;
    private const int CommandRawMaxBytes = 64;

    static EscPosParser()
    {
        DefaultCodePage = Encoding.GetEncoding(437);  // OEM-US (DOS)
    }

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
        {
            // Pending element uses partial buffer - need to emit with specific length
            var (length, element) = state.Pending.Value;
            if (element != null)
            {
                var rawBytes = CollectionsMarshal.AsSpan(state.Buffer).Slice(0, length);
                element = element with { CommandRaw = BuildCommandRaw(rawBytes) };

                if (element is SetCodePage setCodePage)
                {
                    state.Encoding = GetEncodingFromCodePage(setCodePage.CodePage);
                }

                onElement.Invoke(element);
            }

            state.Pending = null;

            // Remove emitted bytes from buffer
            if (state.Buffer.Count >= length)
                state.Buffer.RemoveRange(0, length);
            else
                state.Buffer.Clear();
        }
    }

    private static Encoding GetEncodingFromCodePage(string codePage)
    {
        try
        {
            return int.TryParse(codePage, out var codePageInt)
                ? Encoding.GetEncoding(codePageInt)
                : Encoding.GetEncoding(codePage);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return DefaultCodePage;
        }
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

    private ICommandDescriptor? ResolveDescriptor(byte value)
    {
        var descriptors = state.CurrentNode.Descriptors;
        if (descriptors.Count == 0)
        {
            return null;
        }

        if (descriptors.Count == 1)
        {
            return descriptors[0];
        }

        foreach (var descriptor in descriptors)
        {
            if (descriptor.PrefixAcceptsNext(value))
            {
                return descriptor;
            }
        }

        return descriptors[0];
    }

    private bool TryHandleRootFallback(byte value)
    {
        var descriptor = ResolveDescriptor(value);
        if (descriptor is null)
        {
            return false;
        }

        if (descriptor is ErrorDescriptor)
        {
            EmitPendingElement();
            EmitErrorFromDescriptor(descriptor);
            Navigate(root);
            return false;
        }

        return true;
    }

    private void EmitErrorFromDescriptor(ICommandDescriptor descriptor)
    {
        var result = descriptor.TryParse(CollectionsMarshal.AsSpan(state.Buffer), state);
        if (result.Kind == MatchKind.Matched && result.BytesConsumed > 0)
        {
            // Error descriptor matched - emit the error element
            if (result.Element != null)
            {
                var rawBytes = CollectionsMarshal.AsSpan(state.Buffer).Slice(0, result.BytesConsumed);
                var element = result.Element with
                {
                    CommandRaw = BuildCommandRaw(rawBytes),
                    LengthInBytes = rawBytes.Length
                };
                onElement.Invoke(element);

                // Remove emitted bytes
                if (state.Buffer.Count >= result.BytesConsumed)
                    state.Buffer.RemoveRange(0, result.BytesConsumed);
                else
                    state.Buffer.Clear();
            }
        }
        else
        {
            EmitError();
        }
    }

    private static string BuildCommandRaw(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        var capped = bytes.Length > CommandRawMaxBytes
            ? bytes[..CommandRawMaxBytes]
            : bytes;

        return Convert.ToHexString(capped);
    }

    public void Feed(ReadOnlySpan<byte> buffer, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var value in buffer)
            Feed(value, ct);
    }

    public void Feed(byte value, CancellationToken ct)
    {
        // Process the byte - handlers are responsible for adding to buffer
        // This allows buffer to semantically represent "bytes accumulated for current state"
        bool stateChanged;
        do
        {
            stateChanged = state.Mode switch
            {
                ParserMode.Text => ProcessInAppendTextMode(value),
                ParserMode.Command => ProcessInParseCommandMode(value),
                ParserMode.Error => ProcessInAppendErrorMode(value),
                _ => false
            };
        } while (stateChanged);
    }

    /// <summary>
    /// Changes parser state, automatically emitting accumulated buffer from current state.
    /// </summary>
    private void ChangeState(ParserMode newMode)
    {
        if (state.Mode == newMode)
            return;

        // Emit accumulated buffer based on CURRENT state before transitioning
        if (state.Buffer.Count > 0)
        {
            switch (state.Mode)
            {
                case ParserMode.Text:
                    EmitText();
                    break;
                case ParserMode.Command:
                    if (newMode == ParserMode.Error)
                        AppendError();
                    // Command emits via EmitCommand when successfully parsed
                    // If we're transitioning out, it means parsing failed - emit as error
                    break;
                case ParserMode.Error:
                    // Append any accumulated error, but don't emit yet
                    AppendError();
                    break;
            }
        }

        state.Mode = newMode;
    }

    /// <summary>
    /// Processes a byte while in AppendText mode. Returns true if state changed and needs reprocessing.
    /// </summary>
    private bool ProcessInAppendTextMode(byte value)
    {
        // In AppendText mode, check if current byte could start a command
        if (root.Children.ContainsKey(value))
        {
            // Switch to Command mode - ChangeState will emit accumulated text
            ChangeState(ParserMode.Command);
            Navigate(root);
            return true; // State changed, need to reprocess in Command mode
        }

        // Check if this is a valid text byte
        if (!EscPosTextByteRules.IsTextByte(value))
        {
            // Switch to Error mode - ChangeState will emit accumulated text
            ChangeState(ParserMode.Error);
            return true; // State changed, need to reprocess in Error mode
        }

        // Valid text byte - add to text buffer
        state.Buffer.Add(value);
        return false; // No state change
    }

    /// <summary>
    /// Processes a byte while in ParseCommand mode using trie navigation. Returns true if state changed and needs reprocessing.
    /// </summary>
    private bool ProcessInParseCommandMode(byte value)
    {
        // Add byte to command buffer first
        state.Buffer.Add(value);

        // Try to navigate deeper in the trie
        if (!state.CurrentNode.IsLeaf)
        {
            if (!TryNavigateChild(value))
            {
                // We were in the middle of a command but hit invalid byte
                ChangeState(ParserMode.Error);
                return true; // State changed to Error
            }
        }

        // Node is not leaf, we are in the middle of the command. Current processing is completed
        if (!state.CurrentNode.IsLeaf)
            return true;

        var descriptor = state.CurrentNode.Descriptors

        // Process current trie node
        if (descriptor == null)
            return false;

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
                    EmitCommand(result.Element);
                }

                // Stay in Command mode, reset to root
                Navigate(root);
                return false; // No state change
            }
            else
            {
                // Parse failed - switch to error mode
                ChangeState(ParserMode.Error);
                return true; // State changed to Error
            }
        }

        // If exact length is not known, but buffer length meets/exceeds MinLength, try to parse
        if (!state.ExactLength.HasValue && state.MinLength.HasValue && state.Buffer.Count >= state.MinLength.Value)
        {
            var result = descriptor.TryParse(CollectionsMarshal.AsSpan(state.Buffer), state);

            if (result.Kind == MatchKind.Matched)
            {
                if (result.Element != null)
                    EmitCommand(result.Element);

                Navigate(root);
                return false; // Stay in Command mode
            }
            else if (result.Kind == MatchKind.MatchedPending)
            {
                state.Pending = (state.Buffer.Count, result.Element!);
                return false; // Stay in Command mode, waiting for more bytes
            }
            else if (result.Kind == MatchKind.NeedMore)
            {
                // Continue accumulating in Command mode
                return false;
            }
            else if (result.Kind == MatchKind.NoMatch)
            {
                EmitPendingElement();
                ChangeState(ParserMode.Error);
                return true; // State changed to Error
            }
        }

        return false; // No state change
    }

    /// <summary>
    /// Processes a byte while in AppendError mode. Returns true if state changed and needs reprocessing.
    /// </summary>
    private bool ProcessInAppendErrorMode(byte value)
    {
        // Check if this byte might start a valid command sequence
        if (root.Children.ContainsKey(value))
        {
            // Switch to Command mode - ChangeState will emit accumulated errors
            ChangeState(ParserMode.Command);
            Navigate(root);
            return true; // State changed to Command
        }

        // Check if this is a valid text byte
        if (EscPosTextByteRules.IsTextByte(value))
        {
            // Switch to Text mode - ChangeState will emit accumulated errors
            ChangeState(ParserMode.Text);
            return true; // State changed to Text
        }

        // Invalid byte (not command, not text) - continue accumulating error bytes
        state.Buffer.Add(value);
        return false; // No state change
    }

    /// <summary>
    /// Emits accumulated text from buffer and clears it.
    /// </summary>
    private void EmitText()
    {
        if (state.Buffer.Count == 0)
            return;

        var textBytes = CollectionsMarshal.AsSpan(state.Buffer);
        var text = state.Encoding.GetString(textBytes);

        if (!string.IsNullOrEmpty(text))
        {
            var element = new AppendToLineBuffer(text)
            {
                CommandRaw = BuildCommandRaw(textBytes),
                LengthInBytes = textBytes.Length
            };
            onElement.Invoke(element);
        }

        state.Buffer.Clear();
    }

    /// <summary>
    /// Emits a parsed command from buffer and clears it.
    /// </summary>
    private void EmitCommand(Element? element)
    {
        if (element == null)
        {
            state.Buffer.Clear();
            return;
        }

        var rawBytes = CollectionsMarshal.AsSpan(state.Buffer);
        element = element with
        {
            CommandRaw = BuildCommandRaw(rawBytes),
            LengthInBytes = rawBytes.Length
        };

        if (element is SetCodePage setCodePage)
        {
            state.Encoding = GetEncodingFromCodePage(setCodePage.CodePage);
        }

        onElement.Invoke(element);
        state.Buffer.Clear();
    }

    /// <summary>
    /// Only emit error buffer when emitting other element, otherwise keep accumulating
    /// </summary>
    private void AppendError()
    {
        if (state.Buffer.Count == 0)
            return;

        state.PendingErrorBuffer.AddRange(state.Buffer);
        state.Buffer.Clear();
    }

    private void EmitErrorFinally()
    {
        if (state.PendingErrorBuffer.Count == 0)
            return;

        var rawBytes = CollectionsMarshal.AsSpan(state.PendingErrorBuffer);

        var element = new PrinterError($"Unrecognized {rawBytes.Length} bytes")
        {
            CommandRaw = BuildCommandRaw(rawBytes),
            LengthInBytes = rawBytes.Length
        };
        state.PendingErrorBuffer.Clear();
        onElement.Invoke(element);
    }

    /// <summary>
    /// Signals the end of input and flushes any pending data.
    /// Call this when no more bytes will be fed to the parser.
    /// </summary>
    public void Complete()
    {
        // Emit any pending element first
        EmitPendingElement();

        // Flush remaining buffer based on current mode
        if (state.Buffer.Count > 0)
        {
            switch (state.Mode)
            {
                case ParserMode.Text:
                    EmitText();
                    break;
                case ParserMode.Command:
                case ParserMode.Error:
                    EmitError();
                    break;
            }
        }

        // Reset to initial state
        state.Mode = ParserMode.Command;
        Navigate(root);
    }
}
