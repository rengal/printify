using System.Runtime.InteropServices;
using Printify.Domain.Printing;
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
/// <typeparam name="TDeviceContext">The device context type for this protocol.</typeparam>
/// <typeparam name="TCommandTrieProvider">The command trie provider type.</typeparam>
public abstract class Parser<TDeviceContext, TCommandTrieProvider>
    where TDeviceContext : IDeviceContext
    where TCommandTrieProvider : CommandTrieProvider, new()
{
    private readonly ParserState<TDeviceContext> state;
    private readonly IServiceScopeFactory? scopeFactory;
    private readonly Printer? printer;
    private readonly PrinterSettings? settings;
    private readonly Action<Command> onElement;
    private readonly Action<ReadOnlyMemory<byte>>? onResponse;
    private bool bufferOverflowEmitted;
    private CancellationToken currentCancellationToken;

    /// <summary>
    /// Gets the service scope factory for resolving scoped services.
    /// Available only when parser is constructed with full printer context.
    /// </summary>
    protected IServiceScopeFactory? ScopeFactory => scopeFactory;

    /// <summary>
    /// Gets the printer instance.
    /// Available only when parser is constructed with full printer context.
    /// </summary>
    protected Printer? Printer => printer;

    /// <summary>
    /// Gets the printer settings.
    /// Available only when parser is constructed with full printer context.
    /// </summary>
    protected PrinterSettings? Settings => settings;

    /// <summary>
    /// Gets the response callback for sending data back to the client.
    /// Available only when parser is constructed with full printer context.
    /// </summary>
    protected Action<ReadOnlyMemory<byte>>? OnResponse => onResponse;

    /// <summary>
    /// Gets the command trie provider.
    /// </summary>
    protected TCommandTrieProvider TrieProvider { get; }

    /// <summary>
    /// Gets the root command trie node.
    /// </summary>
    protected CommandTrieNode Root => TrieProvider.Root;

    /// <summary>
    /// Gets the parser state.
    /// </summary>
    protected ParserState<TDeviceContext> State => state;

    protected Parser(
        TCommandTrieProvider trieProvider,
        ParserState<TDeviceContext> state,
        IServiceScopeFactory? scopeFactory,
        Printer? printer,
        PrinterSettings? settings,
        Action<Command> onElement,
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
            handled = state.Mode switch
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
    /// Derived classes implement this to emit buffer when changing modes.
    /// </summary>
    protected abstract void EmitBufferForModeChange(ParserMode oldMode, ParserMode newMode);

    /// <summary>
    /// Processes a byte while in ParseCommand mode using trie navigation.
    /// </summary>
    protected bool ProcessInParseCommandMode(byte value)
    {
        state.Buffer.Add(value);

        var trieNavigation = state.TrieNavigation;

        // Try to navigate deeper in the trie
        if (!trieNavigation.CurrentNode.IsLeaf)
        {
            if (!TryNavigateChild(value))
            {
                // We were in the middle of a command but hit invalid byte
                ChangeState(ParserMode.Error);
                return false;
            }
        }

        // Node is not leaf, we are in the middle of the command. Current processing is completed
        if (!trieNavigation.CurrentNode.IsLeaf)
            return true;

        var descriptor = trieNavigation.CurrentNode.Descriptor;

        // Process current trie node
        if (descriptor == null)
            throw new InvalidOperationException("Descriptor must not be null for leaf nodes");

        trieNavigation.MinLength ??= descriptor.MinLength;

        // Not all bytes received, return
        if (state.Buffer.Count < trieNavigation.MinLength.Value)
            return true;

        if (trieNavigation.ExactLength is null && state.Buffer.Count >= trieNavigation.MinLength.Value)
        {
            var exactLength = descriptor.TryGetExactLength(CollectionsMarshal.AsSpan(state.Buffer));
            if (exactLength.HasValue)
                trieNavigation.ExactLength = exactLength.Value;
        }

        // If exact length is known and not met, return
        if (trieNavigation.ExactLength.HasValue && state.Buffer.Count < trieNavigation.ExactLength.Value)
            return true;

        var result = descriptor.TryParse(CollectionsMarshal.AsSpan(state.Buffer));

        if (result.Kind == MatchKind.NeedMore)
            return true;

        if (result.Kind == MatchKind.Matched)
        {
            // Modify device context based on the parsed element
            ModifyDeviceContext(result.Element);

            EmitCommandElement(result.Element);
            // Command completed, restore default mode
            state.Reset(GetDefaultMode());
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
        state.UnrecognizedBuffer.Add(value);
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
        var oldMode = state.Mode;
        if (oldMode == newMode)
            return;

        // Emit accumulated buffer based on CURRENT state before transitioning
        if (state.Buffer.Count > 0 || state.UnrecognizedBuffer.Count > 0)
        {
            EmitBufferForModeChange(oldMode, newMode);
        }

        if (newMode == ParserMode.Command)
        {
            // Reset trie navigation to ensure each command starts from the root descriptor.
            state.TrieNavigation.Reset();
        }

        state.Mode = newMode;
    }

    /// <summary>
    /// Tries to navigate to a child node in the trie.
    /// </summary>
    protected bool TryNavigateChild(byte value)
    {
        var trieNavigation = state.TrieNavigation;
        if (trieNavigation.CurrentNode.Children.TryGetValue(value, out var nextNode))
        {
            Navigate(nextNode);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Navigates to a new trie node.
    /// </summary>
    protected void Navigate(CommandTrieNode nextNode)
    {
        state.TrieNavigation.Reset();
        state.TrieNavigation.CurrentNode = nextNode;
    }

    /// <summary>
    /// Modifies the device context based on the parsed element.
    /// This is called after a successful parse but before emitting the element.
    /// Derived classes override to update protocol-specific state (encoding, label dimensions, etc.).
    /// </summary>
    protected virtual void ModifyDeviceContext(Command element)
    {
        // Default implementation: no state modification
    }

    /// <summary>
    /// Emits a parsed command from buffer and clears it.
    /// Derived classes can override to handle protocol-specific element processing.
    /// </summary>
    protected virtual void EmitCommandElement(Command? element)
    {
        if (element == null)
        {
            state.Buffer.Clear();
            return;
        }

        var buffer = state.Buffer;
        var rawBytes = CollectionsMarshal.AsSpan(buffer);
        element = element with
        {
            RawBytes = rawBytes.ToArray(),
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
        var unrecognizedBuffer = state.UnrecognizedBuffer;
        if (unrecognizedBuffer.Count == 0)
            return;

        var rawBytes = CollectionsMarshal.AsSpan(unrecognizedBuffer);

        var element = new PrinterError($"Unrecognized {rawBytes.Length} bytes")
        {
            RawBytes = rawBytes.ToArray(),
            LengthInBytes = rawBytes.Length
        };
        unrecognizedBuffer.Clear();
        EmitElement(element, rawBytes.Length);
    }

    /// <summary>
    /// Emits an element to the callback.
    /// </summary>
    protected void EmitElement(Command element, int lengthInBytes)
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
                    RawBytes = Array.Empty<byte>(),
                    LengthInBytes = 0
                });
            }
        }

        onElement.Invoke(element);
    }

    /// <summary>
    /// Derived classes can override to skip buffer overflow check for certain elements.
    /// </summary>
    protected virtual bool ShouldSkipBufferOverflowCheck(Command element)
    {
        return element is ParseError or PrinterError;
    }

    /// <summary>
    /// Completes parsing and flushes any pending data.
    /// </summary>
    public virtual void Complete()
    {
        // Flush remaining buffer based on current mode
        if (state.Buffer.Count > 0 || state.UnrecognizedBuffer.Count > 0)
        {
            var currentMode = state.Mode;
            EmitBufferForModeChange(currentMode, GetDefaultMode());

            if (currentMode == ParserMode.Command || currentMode == ParserMode.Error)
            {
                EmitUnrecognizedBufferAsError();
            }
        }

        // Reset to initial state
        state.Reset(GetDefaultMode());
        bufferOverflowEmitted = false;
    }
}
