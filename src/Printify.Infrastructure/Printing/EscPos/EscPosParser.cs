using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.EscPos;
using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.Common;
using PrinterOperationalFlags = Printify.Domain.Printers.PrinterOperationalFlags;

namespace Printify.Infrastructure.Printing.EscPos;

/// <summary>
/// Parser for ESC/POS protocol commands.
/// ESC/POS is a command-based protocol that supports interleaved text and commands.
/// The parser starts in Text mode and switches to Command mode when a command prefix is detected.
/// </summary>
public sealed class EscPosParser : Parser<EscPosDeviceContext, EscPosCommandTrieProvider>
{
    private static readonly Encoding DefaultCodePage = Encoding.GetEncoding(437); // OEM-US (DOS)

    /// <summary>
    /// Simple constructor for testing or scenarios without printer context.
    /// </summary>
    public EscPosParser(EscPosCommandTrieProvider trieProvider, Action<Element> onElement)
        : base(trieProvider, CreateInitialState(trieProvider), null, null, null, onElement, null)
    {
    }

    /// <summary>
    /// Full constructor with printer context for buffer overflow checking and status responses.
    /// </summary>
    public EscPosParser(
        EscPosCommandTrieProvider trieProvider,
        IServiceScopeFactory scopeFactory,
        Printer printer,
        PrinterSettings settings,
        Action<Element> onElement,
        Action<ReadOnlyMemory<byte>>? onResponse = null)
        : base(trieProvider, CreateInitialState(trieProvider), scopeFactory, printer, settings, onElement, onResponse)
    {
    }

    /// <summary>
    /// Creates the initial parser state with ESC/POS device context.
    /// </summary>
    private static ParserState<EscPosDeviceContext> CreateInitialState(EscPosCommandTrieProvider trieProvider)
    {
        var deviceContext = new EscPosDeviceContext();
        var state = new ParserState<EscPosDeviceContext>(deviceContext, trieProvider.Root);
        // ESC/POS starts in Text mode
        state.Mode = ParserMode.Text;
        return state;
    }

    /// <summary>
    /// Handles Text mode processing for ESC/POS.
    /// </summary>
    protected override bool HandleCustomMode(byte value)
    {
        if (State.Mode != ParserMode.Text)
            return false;

        return ProcessInTextMode(value);
    }

    /// <summary>
    /// Processes a byte while in Text mode.
    /// Returns true if the byte was handled, false if processing needs to be retried (mode changed).
    /// </summary>
    private bool ProcessInTextMode(byte value)
    {
        // Check if current byte could start a command
        if (Root.Children.ContainsKey(value))
        {
            // Switch to Command mode - ChangeState will emit accumulated text
            ChangeState(ParserMode.Command);
            return false; // Reprocess in Command mode
        }

        // Check if this is a valid text byte
        if (!EscPosTextByteRules.IsTextByte(value))
        {
            // Switch to Error mode - ChangeState will emit accumulated text
            ChangeState(ParserMode.Error);
            return false; // Reprocess in Error mode
        }

        // Valid text byte - add to buffer
        State.Buffer.Add(value);
        return true;
    }

    /// <summary>
    /// ESC/POS supports text mode, so return Text mode when queried.
    /// </summary>
    protected override ParserMode? GetTextMode() => ParserMode.Text;

    /// <summary>
    /// ESC/POS returns to Text mode after command completion.
    /// </summary>
    protected override ParserMode GetDefaultMode() => ParserMode.Text;

    /// <summary>
    /// Checks if a byte should be processed as text (for mode switching from Error mode).
    /// </summary>
    protected override bool TryProcessAsTextByte(byte value) => EscPosTextByteRules.IsTextByte(value);

    /// <summary>
    /// Emits buffer when changing modes in ESC/POS.
    /// Handles Text mode (emits text elements) and Command/Error modes (emits errors).
    /// </summary>
    protected override void EmitBufferForModeChange(ParserMode oldMode, ParserMode newMode)
    {
        switch (oldMode)
        {
            case ParserMode.Text:
                EmitTextElement();
                break;
            case ParserMode.Command:
                if (newMode == ParserMode.Error)
                {
                    // Move command buffer to error buffer
                    State.UnrecognizedBuffer.AddRange(State.Buffer);
                    State.Buffer.Clear();
                }
                break;
            case ParserMode.Error:
                EmitUnrecognizedBufferAsError();
                break;
        }
    }

    /// <summary>
    /// Emits accumulated text from buffer and clears it.
    /// </summary>
    private void EmitTextElement()
    {
        if (State.Buffer.Count == 0)
            return;

        var textBytes = CollectionsMarshal.AsSpan(State.Buffer);
        var text = State.DeviceContext.Encoding.GetString(textBytes);

        if (!string.IsNullOrEmpty(text))
        {
            var element = new AppendText(text)
            {
                CommandRaw = BuildCommandRaw(textBytes),
                LengthInBytes = textBytes.Length
            };
            EmitElement(element, textBytes.Length);
        }

        State.Buffer.Clear();
    }

    /// <summary>
    /// Modifies the device context based on the parsed element.
    /// Called after successful parse but before emitting.
    /// </summary>
    protected override void ModifyDeviceContext(Element element)
    {
        // Handle encoding change when SetCodePage is received
        if (element is SetCodePage setCodePage)
        {
            State.DeviceContext.Encoding = GetEncodingFromCodePage(setCodePage.CodePage);
        }
    }

    /// <summary>
    /// Overrides to handle ESC/POS-specific element processing:
    /// - Sends status responses for StatusRequest elements
    /// </summary>
    protected override void EmitCommandElement(Element? element)
    {
        if (element == null)
        {
            State.Buffer.Clear();
            return;
        }

        var buffer = State.Buffer;
        var rawBytes = CollectionsMarshal.AsSpan(buffer);
        element = element with
        {
            CommandRaw = BuildCommandRaw(rawBytes),
            LengthInBytes = rawBytes.Length
        };

        EmitElement(element, rawBytes.Length);

        // Handle status requests by immediately generating and sending response
        if (element is StatusRequest statusRequest)
        {
            BuildAndSendStatusResponse(statusRequest.RequestType, CurrentCancellationToken);
        }

        buffer.Clear();
    }

    /// <summary>
    /// Gets encoding from code page string, falling back to default if invalid.
    /// </summary>
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

    /// <summary>
    /// Builds and sends an ESC/POS status response for a given request type.
    /// </summary>
    private void BuildAndSendStatusResponse(StatusRequestType requestType, CancellationToken ct)
    {
        try
        {
            PrinterOperationalFlags? flags = null;
            var snapshot = new PrinterBufferSnapshot(
                BufferedBytes: 0,
                IsBusy: false,
                IsFull: false,
                IsEmpty: true);

            if (ScopeFactory is not null && Printer is not null && Settings is not null)
            {
                using var scope = ScopeFactory.CreateScope();
                var printerRepository = scope.ServiceProvider.GetRequiredService<IPrinterRepository>();
                var bufferCoordinator = scope.ServiceProvider.GetRequiredService<IPrinterBufferCoordinator>();
                flags = printerRepository.GetOperationalFlagsAsync(Printer.Id, ct).GetAwaiter().GetResult();
                snapshot = bufferCoordinator.GetSnapshot(Printer, Settings);
            }

            var isCoverOpen = flags?.IsCoverOpen ?? false;
            var isPaperOut = flags?.IsPaperOut ?? false;
            var hasError = flags?.HasError ?? false;
            var isOfflineFlag = flags?.IsOffline ?? false;
            var isPaperNearEnd = flags?.IsPaperNearEnd ?? false;
            var isBufferFull = snapshot.IsFull;
            var isBufferBusy = snapshot.IsBusy;
            var isPrinterOffline = isOfflineFlag || isBufferFull || isBufferBusy || isCoverOpen || isPaperOut || hasError;

            var statusByte = requestType switch
            {
                StatusRequestType.PrinterStatus => BuildPrinterStatusByte(isPrinterOffline, hasError, isPaperOut),
                StatusRequestType.OfflineCause => BuildOfflineCauseByte(isCoverOpen, isPaperOut, hasError, isOfflineFlag),
                StatusRequestType.ErrorCause => BuildErrorCauseByte(hasError),
                StatusRequestType.PaperRollSensor => BuildPaperRollSensorByte(isPaperNearEnd, isPaperOut),
                _ => BuildPrinterStatusByte(isPrinterOffline, hasError, isPaperOut)
            };

            // Emit the response element for document/debugging
            var response = new StatusResponse(statusByte,
                IsPaperOut: isPaperOut,
                IsCoverOpen: isCoverOpen,
                IsOffline: isPrinterOffline)
            {
                CommandRaw = Convert.ToHexString(new[] { statusByte }),
                LengthInBytes = 1
            };

            // Emit element using base class method (needs access to onElement callback)
            base.EmitElement(response, 1);

            var responseHandler = OnResponse;
            if (responseHandler is not null)
            {
                Task.Run(() => responseHandler.Invoke(new[] { statusByte }), CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the print job is canceled mid-response
        }
        catch (Exception ex)
        {
            // Emit error - need to access onElement through base class
            base.EmitElement(new PrinterError($"Status response failed: {ex.Message}"), 0);
        }
    }

    private static byte BuildPrinterStatusByte(bool isOffline, bool hasError, bool isPaperOut)
    {
        const byte baseStatus = 0x12;
        var status = baseStatus;
        if (isOffline)
            status |= 0x08;
        if (hasError)
            status |= 0x20;
        if (isPaperOut)
            status |= 0x40;
        return status;
    }

    private static byte BuildOfflineCauseByte(bool isCoverOpen, bool isPaperOut, bool hasError, bool isOfflineFlag)
    {
        const byte baseStatus = 0x12;
        var status = baseStatus;
        if (isCoverOpen)
            status |= 0x04;
        if (isPaperOut)
            status |= 0x20;
        if (hasError || isOfflineFlag)
            status |= 0x40;
        return status;
    }

    private static byte BuildErrorCauseByte(bool hasError)
    {
        const byte baseStatus = 0x02;
        var status = baseStatus;
        if (hasError)
            status |= 0x10;
        return status;
    }

    private static byte BuildPaperRollSensorByte(bool isPaperNearEnd, bool isPaperOut)
    {
        const byte baseStatus = 0x12;
        var status = baseStatus;
        if (isPaperNearEnd)
            status |= 0x04;
        if (isPaperOut)
            status |= 0x20;
        return status;
    }

    /// <summary>
    /// Completes parsing and flushes any pending data.
    /// </summary>
    public override void Complete()
    {
        // Flush remaining buffer based on current mode
        if (State.Buffer.Count > 0 || State.UnrecognizedBuffer.Count > 0)
        {
            var currentMode = State.Mode;
            EmitBufferForModeChange(currentMode, GetDefaultMode());

            if (currentMode == ParserMode.Command || currentMode == ParserMode.Error)
            {
                EmitUnrecognizedBufferAsError();
            }
        }

        // Reset to initial state
        State.Reset(GetDefaultMode());
    }
}
