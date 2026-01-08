using Printify.Domain.Documents.Elements;
using System.Runtime.InteropServices;
using System.Text;

namespace Printify.Infrastructure.Printing.EscPos;

public sealed class EscPosParser
{
    private readonly EscPosCommandTrieNode root;
    private readonly ParserState state;
    private readonly Func<int?>? getAvailableBytes;
    private readonly Action<Element> onElement;
    private readonly Action<ReadOnlyMemory<byte>>? onResponse;
    private bool bufferOverflowEmitted;
    private static readonly Encoding DefaultCodePage;
    private const int CommandRawMaxBytes = 64;

    static EscPosParser()
    {
        DefaultCodePage = Encoding.GetEncoding(437);  // OEM-US (DOS)
    }

    public EscPosParser(IEscPosCommandTrieProvider trieProvider, Action<Element> onElement)
    {
        ArgumentNullException.ThrowIfNull(trieProvider);
        ArgumentNullException.ThrowIfNull(onElement);
        root = trieProvider.Root;
        this.onElement = onElement;
        state = new ParserState(root);
    }

    public EscPosParser(
        IEscPosCommandTrieProvider trieProvider,
        Func<int?> getAvailableBytes,
        Action<Element> onElement,
        Action<ReadOnlyMemory<byte>>? onResponse = null)
    {
        ArgumentNullException.ThrowIfNull(trieProvider);
        ArgumentNullException.ThrowIfNull(getAvailableBytes);
        ArgumentNullException.ThrowIfNull(onElement);
        root = trieProvider.Root;
        this.onElement = onElement;
        this.getAvailableBytes = getAvailableBytes;
        this.onResponse = onResponse;
        state = new ParserState(root);
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
        if (state.CommandState.CurrentNode.Children.TryGetValue(value, out var nextNode))
        {
            Navigate(nextNode);
            return true;
        }

        return false;
    }

    private void Navigate(EscPosCommandTrieNode nextNode)
    {
        state.CommandState.Reset();
        state.CommandState.CurrentNode = nextNode;
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
        bool handled;
        do
        {
            handled = state.Mode switch
            {
                ParserMode.Text => ProcessInAppendTextMode(value),
                ParserMode.Command => ProcessInParseCommandMode(value),
                ParserMode.Error => ProcessInAppendErrorMode(value),
                _ => false
            };
        } while (!handled);
    }

    /// <summary>
    /// Changes parser state, automatically emitting accumulated buffer from current state.
    /// </summary>
    private void ChangeState(ParserMode newMode)
    {
        if (state.Mode == newMode)
            return;

        // Emit accumulated buffer based on CURRENT state before transitioning
        if (state.Buffer.Count > 0 || state.UnrecognizedBuffer.Count > 0)
        {
            switch (state.Mode)
            {
                case ParserMode.Text:
                    EmitTextElement();
                    break;
                case ParserMode.Command:
                    if (newMode == ParserMode.Error)
                        AppendToUnrecognizedBuffer();
                    // Command emits via EmitCommand when successfully parsed
                    // If we're transitioning out, it means parsing failed - emit as error
                    break;
                case ParserMode.Error:
                    EmitUnrecognizedBufferAsError();
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
            return false; // State changed, need to reprocess in Command mode
        }

        // Check if this is a valid text byte
        if (!EscPosTextByteRules.IsTextByte(value))
        {
            // Switch to Error mode - ChangeState will emit accumulated text
            ChangeState(ParserMode.Error);
            return false; // State changed, need to reprocess in Error mode
        }

        // Valid text byte - add to text buffer
        state.Buffer.Add(value);
        return true; // No state change
    }

    /// <summary>
    /// Processes a byte while in ParseCommand mode using trie navigation. Returns true if state changed and needs reprocessing.
    /// </summary>
    private bool ProcessInParseCommandMode(byte value)
    {
        // Add byte to command buffer first
        state.Buffer.Add(value);

        var commandState = state.CommandState;

        // Try to navigate deeper in the trie
        if (!commandState.CurrentNode.IsLeaf)
        {
            if (!TryNavigateChild(value))
            {
                // We were in the middle of a command but hit invalid byte
                ChangeState(ParserMode.Error);
                return false;
            }
        }

        // Node is not leaf, we are in the middle of the command. Current processing is completed
        if (!commandState.CurrentNode.IsLeaf)
            return true;

        var descriptor = commandState.CurrentNode.Descriptor;

        // Process current trie node
        if (descriptor == null)
            throw new InvalidOperationException("Descriptor must not be null for leaf nodes");

        commandState.MinLength ??= descriptor.MinLength;

        // Not all bytes received, return
        if (state.Buffer.Count < commandState.MinLength.Value)
            return true;

        if (commandState is { ExactLength: null } && state.Buffer.Count >= commandState.MinLength.Value)
        {
            var exactLength = descriptor.TryGetExactLength(CollectionsMarshal.AsSpan(state.Buffer));
            if (exactLength.HasValue)
                commandState.ExactLength = exactLength.Value;
        }

        // If exact length is known and not met, return
        if (commandState.ExactLength.HasValue && state.Buffer.Count < commandState.ExactLength.Value)
            return true;

        var result = descriptor.TryParse(CollectionsMarshal.AsSpan(state.Buffer), state);

        if (result.Kind == MatchKind.NeedMore)
            return true;

        if (result.Kind == MatchKind.Matched)
        {
            EmitCommandElement(result.Element);
            // Command completed, restore text state
            ChangeState(ParserMode.Text);
            return true; // No state change
        }

        // Parse failed - switch to error mode
        ChangeState(ParserMode.Error);
        return true;
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
            state.CommandState.Reset();
            return false;
        }

        // Check if this is a valid text byte
        if (EscPosTextByteRules.IsTextByte(value))
        {
            // Switch to Text mode - ChangeState will emit accumulated errors
            ChangeState(ParserMode.Text);
            return false;
        }

        // Invalid byte (not command, not text) - continue accumulating error bytes
        state.UnrecognizedBuffer.Add(value);
        return true;
    }

    /// <summary>
    /// Emits accumulated text from buffer and clears it.
    /// </summary>
    private void EmitTextElement()
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
            EmitElement(element, textBytes.Length);
        }

        state.Buffer.Clear();
    }

    /// <summary>
    /// Emits a parsed command from buffer and clears it.
    /// </summary>
    private void EmitCommandElement(Element? element)
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

        EmitElement(element, rawBytes.Length);

        // Handle status requests by immediately generating and sending response
        if (element is StatusRequest statusRequest)
        {
            BuildAndSendStatusResponse(statusRequest.RequestType);
        }

        state.Buffer.Clear();
    }

    /// <summary>
    /// Builds and sends an ESC/POS status response for a given request type.
    /// </summary>
    private void BuildAndSendStatusResponse(StatusRequestType requestType)
    {
        // ESC/POS status byte format (for ready printer):
        // Bit 0: Fixed (0)
        // Bit 1: Fixed (1)
        // Bit 2: Cover closed (0 = closed, 1 = open)
        // Bit 3: Not used for offline sensor (0)
        // Bit 4: Fixed (1)
        // Bit 5: Paper present (0 = present, 1 = out)
        // Bit 6: No error (0)
        // Bit 7: Fixed (0)
        // Ready printer = 0x12 (0001 0010)

        const byte readyStatus = 0x12;
        const byte offlineStatus = 0x52;
        // TODO: Make configurable per printer settings for testing

        var availableBytes = getAvailableBytes?.Invoke();
        var isReady = availableBytes is null || availableBytes > 0;
        var statusByte = isReady ? readyStatus : offlineStatus;

        // Send response to client immediately
        onResponse?.Invoke(new[] { statusByte });

        // Also emit the response element for document/debugging
        var response = new StatusResponse(statusByte,
            IsPaperOut: false,
            IsCoverOpen: false,
            IsOffline: !isReady)
        {
            CommandRaw = Convert.ToHexString(new[] { statusByte }),
            LengthInBytes = 1
        };
        onElement.Invoke(response);
    }

    /// <summary>
    /// Only emit error buffer when emitting other element, otherwise keep accumulating
    /// </summary>
    private void AppendToUnrecognizedBuffer()
    {
        if (state.Buffer.Count == 0)
            return;

        state.UnrecognizedBuffer.AddRange(state.Buffer);
        state.Buffer.Clear();
    }

    private void EmitUnrecognizedBufferAsError()
    {
        if (state.UnrecognizedBuffer.Count == 0)
            return;

        var rawBytes = CollectionsMarshal.AsSpan(state.UnrecognizedBuffer);

        var element = new PrinterError($"Unrecognized {rawBytes.Length} bytes")
        {
            CommandRaw = BuildCommandRaw(rawBytes),
            LengthInBytes = rawBytes.Length
        };
        state.UnrecognizedBuffer.Clear();
        EmitElement(element, rawBytes.Length);
    }

    private void EmitElement(Element element, int lengthInBytes)
    {
        // Check buffer overflow and emit PrinterError, if needed.
        // Note: Buffer overflow error is emitted only ONCE per print job.
        // Once overflow occurs, the printer behavior becomes undefined - subsequent commands
        // may be corrupted, partially executed, or ignored entirely as the receive buffer wraps
        // or drops data. This single error signals the point where reliable command processing ends.
        if (!bufferOverflowEmitted &&
            getAvailableBytes is not null &&
            element is not (Error or PrinterError or StatusRequest)
           )
        {
            var availableBytes = getAvailableBytes();
            if (lengthInBytes > availableBytes && !bufferOverflowEmitted)
            {
                // Emit overflow once, just before the element that exceeds capacity.
                bufferOverflowEmitted = true;
                onElement.Invoke(new PrinterError("Buffer overflow")
                {
                    CommandRaw = string.Empty,
                    LengthInBytes = 0
                });
            }
        }

        onElement.Invoke(element);
    }

    /// <summary>
    /// Signals the end of input and flushes any pending data.
    /// Call this when no more bytes will be fed to the parser.
    /// </summary>
    public void Complete()
    {

        // Flush remaining buffer based on current mode
        if (state.Buffer.Count > 0 || state.UnrecognizedBuffer.Count > 0)
        {
            switch (state.Mode)
            {
                case ParserMode.Text:
                    EmitTextElement();
                    break;
                case ParserMode.Command:
                case ParserMode.Error:
                    if (state.Mode == ParserMode.Command)
                        ChangeState(ParserMode.Error);

                    EmitUnrecognizedBufferAsError();
                    break;
            }
        }

        // Reset to initial state
        state.Mode = ParserMode.Text;
        Navigate(root);
        bufferOverflowEmitted = false;
    }
}
