using System.Runtime.InteropServices;
using System.Text;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Microsoft.Extensions.DependencyInjection;

namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// Parser modes shared across all protocol parsers.
/// </summary>
public enum ParserMode
{
    Text,
    Command,
    Error
}

/// <summary>
/// Generic base parser for printer protocols that use command trie matching.
/// Handles command and error parsing modes. Derived classes can extend with
/// additional modes (e.g., Text mode for ESC/POS).
/// </summary>
/// <typeparam name="TState">The parser state type.</typeparam>
/// <typeparam name="TCommandTrieProvider">The command trie provider type.</typeparam>
public abstract class Parser<TState, TCommandTrieProvider>
    where TState : class
    where TCommandTrieProvider : CommandTrieProvider<TState, ICommandDescriptor<TState>>
{
    private readonly TState state;
    private readonly IServiceScopeFactory? scopeFactory;
    private readonly Printer? printer;
    private readonly PrinterSettings? settings;
    private readonly Action<Element> onElement;
    private readonly Action<ReadOnlyMemory<byte>>? onResponse;
    private bool bufferOverflowEmitted;
    private CancellationToken currentCancellationToken;
    private const int CommandRawMaxBytes = 64;

    /// <summary>
    /// Gets the command trie provider.
    /// </summary>
    protected TCommandTrieProvider TrieProvider { get; }

    /// <summary>
    /// Gets the root command trie node.
    /// </summary>
    protected CommandTrieNode<TState> Root => TrieProvider.Root;

    protected Parser(
        TCommandTrieProvider trieProvider,
        TState state,
        IServiceScopeFactory? scopeFactory,
        Printer? printer,
        PrinterSettings? settings,
        Action<Element> onElement,
        Action<ReadOnlyMemory<byte>>? onResponse)
    {
        TrieProvider = trieProvider;
        this.state = state;
        this.scopeFactory = scopeFactory;
        this.printer = printer;
        this.settings = settings;
        this.onElement = onElement;
        this.onResponse = onResponse;
    }

    /// <summary>
    /// Gets the current parser state.
    /// </summary>
    protected TState State => state;

    /// <summary>
    /// Gets the cancellation token for the current operation.
    /// </summary>
    protected CancellationToken CurrentCancellationToken => currentCancellationToken;

    /// <summary>
    /// Feeds a buffer of bytes to the parser.
    /// </summary>
    public void Feed(ReadOnlySpan<byte> buffer, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        currentCancellationToken = ct;

        foreach (var value in buffer)
            Feed(value, ct);
    }

    /// <summary>
    /// Processes a single byte. Derived classes can override to handle custom modes.
    /// </summary>
    protected virtual void Feed(byte value, CancellationToken ct)
    {
        currentCancellationToken = ct;

        bool handled;
        do
        {
            handled = GetMode() switch
            {
                ParserMode.Command => ProcessInParseCommandMode(value),
                ParserMode.Error => ProcessInAppendErrorMode(value),
                _ => HandleCustomMode(value)
            };
        } while (!handled);
    }

    /// <summary>
    /// Derived classes implement this to handle custom modes (e.g., Text mode for ESC/POS).
    /// Should return true if the byte was handled, false if processing needs to be retried.
    /// </summary>
    protected virtual bool HandleCustomMode(byte value)
    {
        // Default implementation: unknown mode, treat as unhandled
        return false;
    }

    /// <summary>
    /// Derived classes implement this to return the current parser mode.
    /// </summary>
    protected abstract ParserMode GetMode();

    /// <summary>
    /// Derived classes implement this to change the parser mode.
    /// </summary>
    protected abstract void SetMode(ParserMode newMode);

    /// <summary>
    /// Derived classes implement this to emit accumulated buffer when changing modes.
    /// </summary>
    protected abstract void EmitBufferForModeChange(ParserMode oldMode, ParserMode newMode);

    /// <summary>
    /// Derived classes implement this to get the buffer list.
    /// </summary>
    protected abstract List<byte> GetBuffer();

    /// <summary>
    /// Derived classes implement this to get the unrecognized buffer list.
    /// </summary>
    protected abstract List<byte> GetUnrecognizedBuffer();

    /// <summary>
    /// Derived classes implement this to get the command state.
    /// </summary>
    protected abstract ICommandState<TState> GetCommandState();

    /// <summary>
    /// Processes a byte while in ParseCommand mode using trie navigation.
    /// </summary>
    protected bool ProcessInParseCommandMode(byte value)
    {
        var buffer = GetBuffer();
        buffer.Add(value);

        var commandState = GetCommandState();

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
        if (buffer.Count < commandState.MinLength.Value)
            return true;

        if (commandState.ExactLength is null && buffer.Count >= commandState.MinLength.Value)
        {
            var exactLength = descriptor.TryGetExactLength(CollectionsMarshal.AsSpan(buffer));
            if (exactLength.HasValue)
                commandState.ExactLength = exactLength.Value;
        }

        // If exact length is known and not met, return
        if (commandState.ExactLength.HasValue && buffer.Count < commandState.ExactLength.Value)
            return true;

        var result = descriptor.TryParse(CollectionsMarshal.AsSpan(buffer), state);

        if (result.Kind == MatchKind.NeedMore)
            return true;

        if (result.Kind == MatchKind.Matched)
        {
            EmitCommandElement(result.Element);
            // Command completed, restore default mode
            ChangeState(GetDefaultMode());
            return true;
        }

        // Parse failed - switch to error mode
        ChangeState(ParserMode.Error);
        return true;
    }

    /// <summary>
    /// Processes a byte while in AppendError mode.
    /// </summary>
    protected bool ProcessInAppendErrorMode(byte value)
    {
        // Check if this byte might start a valid command sequence
        if (Root.Children.ContainsKey(value))
        {
            // Switch to Command mode - ChangeState will emit accumulated errors
            ChangeState(ParserMode.Command);
            return false;
        }

        // Check if this is a valid text byte (for protocols with text mode)
        if (TryProcessAsTextByte(value))
        {
            // Switch to text mode - ChangeState will emit accumulated errors
            var textMode = GetTextMode();
            if (textMode.HasValue)
            {
                ChangeState(textMode.Value);
            }
            return false;
        }

        // Invalid byte (not command, not text) - continue accumulating error bytes
        GetUnrecognizedBuffer().Add(value);
        return true;
    }

    /// <summary>
    /// Derived classes can override to check if a byte should be processed as text.
    /// Returns true if the byte is valid text and the parser should switch to text mode.
    /// </summary>
    protected virtual bool TryProcessAsTextByte(byte value) => false;

    /// <summary>
    /// Derived classes return the text mode (or null if no text mode).
    /// </summary>
    protected virtual ParserMode? GetTextMode() => null;

    /// <summary>
    /// Derived classes return the default mode after command completion.
    /// </summary>
    protected virtual ParserMode GetDefaultMode() => ParserMode.Command;

    /// <summary>
    /// Changes parser state, automatically emitting accumulated buffer from current state.
    /// </summary>
    protected void ChangeState(ParserMode newMode)
    {
        var oldMode = GetMode();
        if (oldMode == newMode)
            return;

        // Emit accumulated buffer based on CURRENT state before transitioning
        var buffer = GetBuffer();
        var unrecognizedBuffer = GetUnrecognizedBuffer();

        if (buffer.Count > 0 || unrecognizedBuffer.Count > 0)
        {
            EmitBufferForModeChange(oldMode, newMode);
        }

        SetMode(newMode);
    }

    /// <summary>
    /// Tries to navigate to a child node in the trie.
    /// </summary>
    protected bool TryNavigateChild(byte value)
    {
        var commandState = GetCommandState();
        if (commandState.CurrentNode.Children.TryGetValue(value, out var nextNode))
        {
            Navigate(nextNode);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Navigates to a new trie node.
    /// </summary>
    protected void Navigate(CommandTrieNode<TState> nextNode)
    {
        var commandState = GetCommandState();
        commandState.Reset();
        commandState.CurrentNode = nextNode;
    }

    /// <summary>
    /// Emits a parsed command from buffer and clears it.
    /// Derived classes can override to handle protocol-specific element processing.
    /// </summary>
    protected virtual void EmitCommandElement(Element? element)
    {
        if (element == null)
        {
            GetBuffer().Clear();
            return;
        }

        var buffer = GetBuffer();
        var rawBytes = CollectionsMarshal.AsSpan(buffer);
        element = element with
        {
            CommandRaw = BuildCommandRaw(rawBytes),
            LengthInBytes = rawBytes.Length
        };

        EmitElement(element, rawBytes.Length);
        buffer.Clear();
    }

    /// <summary>
    /// Emits an unrecognized buffer as an error.
    /// </summary>
    protected void EmitUnrecognizedBufferAsError()
    {
        var unrecognizedBuffer = GetUnrecognizedBuffer();
        if (unrecognizedBuffer.Count == 0)
            return;

        var rawBytes = CollectionsMarshal.AsSpan(unrecognizedBuffer);

        var element = new PrinterError($"Unrecognized {rawBytes.Length} bytes")
        {
            CommandRaw = BuildCommandRaw(rawBytes),
            LengthInBytes = rawBytes.Length
        };
        unrecognizedBuffer.Clear();
        EmitElement(element, rawBytes.Length);
    }

    /// <summary>
    /// Emits an element to the callback.
    /// </summary>
    protected void EmitElement(Element element, int lengthInBytes)
    {
        // Check buffer overflow and emit PrinterError, if needed.
        if (!bufferOverflowEmitted &&
            scopeFactory is not null &&
            printer is not null &&
            settings is not null &&
            !ShouldSkipBufferOverflowCheck(element))
        {
            using var scope = scopeFactory.CreateScope();
            var bufferCoordinator = scope.ServiceProvider.GetRequiredService<IPrinterBufferCoordinator>();
            var availableBytes = bufferCoordinator.GetAvailableBytes(printer, settings);
            if (availableBytes.HasValue && lengthInBytes > availableBytes.Value && !bufferOverflowEmitted)
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
    /// Derived classes can override to skip buffer overflow check for certain elements.
    /// </summary>
    protected virtual bool ShouldSkipBufferOverflowCheck(Element element)
    {
        return element is ParseError or PrinterError;
    }

    /// <summary>
    /// Builds a hex string representation of command bytes for debugging.
    /// </summary>
    protected static string BuildCommandRaw(ReadOnlySpan<byte> bytes)
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

    /// <summary>
    /// Completes parsing and flushes any pending data.
    /// </summary>
    public virtual void Complete()
    {
        var buffer = GetBuffer();
        var unrecognizedBuffer = GetUnrecognizedBuffer();

        // Flush remaining buffer based on current mode
        if (buffer.Count > 0 || unrecognizedBuffer.Count > 0)
        {
            var currentMode = GetMode();
            EmitBufferForModeChange(currentMode, GetDefaultMode());

            if (currentMode == ParserMode.Command || currentMode == ParserMode.Error)
            {
                EmitUnrecognizedBufferAsError();
            }
        }

        // Reset to initial state
        SetMode(GetDefaultMode());
        Navigate(Root);
        bufferOverflowEmitted = false;
    }
}
