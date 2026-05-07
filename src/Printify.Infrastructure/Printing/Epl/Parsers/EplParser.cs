using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Printify.Domain.Printing;
using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.Epl.Commands;

namespace Printify.Infrastructure.Printing.Epl.Parsers;

/// <summary>
/// Parser for EPL (Eltron Programming Language) page mode printer commands.
/// EPL uses newline-terminated ASCII commands that draw elements on a label.
/// Unlike ESC/POS line mode, EPL accumulates drawing commands until a print command (P) is received.
/// EPL does not support free text - any bytes that aren't valid commands are treated as errors.
/// </summary>
public sealed class EplParser : Parser<EplDeviceContext, EplCommandTrieProvider>
{
    private const int GraphicCommandParameterCount = 4;

    private readonly record struct InitialParserContext(
        EplCommandTrieProvider TrieProvider,
        ParserState<EplDeviceContext> State);

    private static InitialParserContext CreateInitialState()
    {
        var trieProvider = new EplCommandTrieProvider();
        var state = new ParserState<EplDeviceContext>(new EplDeviceContext(), trieProvider.Root)
        {
            Mode = ParserMode.Command
        };
        return new InitialParserContext(trieProvider, state);
    }

    /// <summary>
    /// Initializes a new EPL parser with the specified element callback.
    /// </summary>
    /// <param name="onElement">Callback invoked for each parsed element.</param>
    public EplParser(Action<Command> onElement)
        : this(
            context: CreateInitialState(),
            scopeFactory: null,
            printer: null,
            settings: null,
            onElement: onElement)
    {
    }

    /// <summary>
    /// Full constructor with printer context for readiness and overflow checks.
    /// </summary>
    public EplParser(
        IServiceScopeFactory scopeFactory,
        Printer printer,
        PrinterSettings settings,
        Action<Command> onElement)
        : this(
            context: CreateInitialState(),
            scopeFactory: scopeFactory,
            printer: printer,
            settings: settings,
            onElement: onElement)
    {
    }

    private EplParser(
        InitialParserContext context,
        IServiceScopeFactory? scopeFactory,
        Printer? printer,
        PrinterSettings? settings,
        Action<Command> onElement)
        : base(
            context.TrieProvider,
            context.State,
            scopeFactory: scopeFactory,
            printer: printer,
            settings: settings,
            onElement: onElement,
            onResponse: null)
    {
        // EPL starts in Command mode (no Text mode)
        State.Mode = ParserMode.Command;
    }

    /// <summary>
    /// Modifies the device context based on the parsed element.
    /// Called after successful parse but before emitting.
    /// </summary>
    protected override void ModifyDeviceContext(Command element)
    {
        // EPL commands that modify device context state
        switch (element)
        {
            case EplSetLabelWidth setLabelWidth:
                State.DeviceContext.LabelWidth = setLabelWidth.Width;
                break;
            case EplSetLabelHeight setLabelHeight:
                State.DeviceContext.LabelHeight = setLabelHeight.Height;
                break;
            case EplSetPrintSpeed setPrintSpeed:
                State.DeviceContext.PrintSpeed = setPrintSpeed.Speed;
                break;
            case EplSetPrintDarkness setPrintDarkness:
                State.DeviceContext.PrintDarkness = setPrintDarkness.Darkness;
                break;
            case EplSetInternationalCharacter setInternationalCharacter:
                // Update encoding based on international character set
                // P1 is the primary character set code
                if (setInternationalCharacter.P1 is 8 or 38) // DOS 866 Cyrillic
                    State.DeviceContext.Encoding = System.Text.Encoding.GetEncoding(866); //todo debugnow: move to dict, add all codepages
                break;
        }
    }

    /// <summary>
    /// EPL doesn't have text mode, so we only handle command and error modes.
    /// When switching from Command to Error mode, emit any buffered bytes as an error.
    /// </summary>
    protected override void EmitBufferForModeChange(ParserMode oldMode, ParserMode newMode)
    {
        switch (oldMode)
        {
            case ParserMode.Command:
                if (newMode == ParserMode.Error)
                {
                    // Move command buffer to error buffer (same as EscPos)
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
    /// EPL doesn't have text mode, so the default mode is Command.
    /// </summary>
    protected override ParserMode GetDefaultMode() => ParserMode.Command;

    /// <summary>
    /// Skip buffer overflow check for error elements.
    /// </summary>
    protected override bool ShouldSkipBufferOverflowCheck(Command element)
    {
        return IsErrorCommand(element);
    }

    protected override bool TryGetImageDimensions(Command element, out int x, out int y, out int width, out int height)
    {
        if (element is EplBaseRasterImage img)
        {
            x = img.X;
            y = img.Y;
            width = img.Width;
            height = img.Height;
            return true;
        }

        x = y = width = height = 0;
        return false;
    }

    protected override bool TryGetImageMarkedRightEdge(Command element, out int rightEdge)
    {
        rightEdge = 0;

        if (element is not EplBaseRasterImage img ||
            !TryReadGraphicPayload(element.RawBytes, out var payloadOffset, out var bytesPerRow, out var height))
        {
            return false;
        }

        var dataLength = bytesPerRow * height;
        if (element.RawBytes.Length < payloadOffset + dataLength)
        {
            return false;
        }

        // EPL GW payloads are inverted during media conversion, so unset raw bits are printed black dots.
        var payload = element.RawBytes.AsSpan(payloadOffset, dataLength);
        rightEdge = FindMarkedRightEdge(payload, bytesPerRow, height, img.X);
        return true;
    }

    protected override bool IsPrintableCommand(Command element)
    {
        return element is EplScalableText
            or EplDrawHorizontalLine
            or EplDrawBox
            or EplRasterImageUpload
            or EplRasterImage
            or EplPrintBarcodeUpload
            or EplPrintBarcode;
    }

    protected override void EmitCommandElement(Command? element)
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
            RawBytes = rawBytes.ToArray(),
            LengthInBytes = rawBytes.Length
        };

        EmitElement(element, rawBytes.Length);
        buffer.Clear();
    }

    protected override Command CreatePrinterError(string? message)
    {
        return new EplPrinterError(message);
    }

    protected override bool IsErrorCommand(Command element)
    {
        return element is EplParseError or EplPrinterError;
    }

    private static bool TryReadGraphicPayload(
        byte[] rawBytes,
        out int payloadOffset,
        out int bytesPerRow,
        out int height)
    {
        payloadOffset = 0;
        bytesPerRow = 0;
        height = 0;

        var commaCount = 0;
        var headerEndIndex = -1;
        for (var i = 0; i < rawBytes.Length; i++)
        {
            if (rawBytes[i] != ',')
            {
                continue;
            }

            commaCount++;
            if (commaCount == GraphicCommandParameterCount)
            {
                headerEndIndex = i;
                break;
            }
        }

        if (headerEndIndex < 0)
        {
            return false;
        }

        var header = System.Text.Encoding.ASCII.GetString(rawBytes.AsSpan(0, headerEndIndex));
        var parts = header[2..].Split(',');
        if (parts.Length < GraphicCommandParameterCount ||
            !TryParseInvariantInt(parts[2], out bytesPerRow) ||
            !TryParseInvariantInt(parts[3], out height))
        {
            return false;
        }

        payloadOffset = headerEndIndex + 1;
        return bytesPerRow > 0 && height > 0;
    }

    private static bool TryParseInvariantInt(string value, out int result)
    {
        return int.TryParse(
            value.Trim(),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out result);
    }

    private static int FindMarkedRightEdge(ReadOnlySpan<byte> rasterData, int bytesPerRow, int height, int x)
    {
        var rightEdge = x;

        for (var row = 0; row < height; row++)
        {
            var rowOffset = row * bytesPerRow;
            for (var byteIndex = bytesPerRow - 1; byteIndex >= 0; byteIndex--)
            {
                var value = rasterData[rowOffset + byteIndex];
                if (value == 0xFF)
                {
                    continue;
                }

                for (var bitPosition = 7; bitPosition >= 0; bitPosition--)
                {
                    var mask = 1 << (7 - bitPosition);
                    if ((value & mask) != 0)
                    {
                        continue;
                    }

                    // The right boundary is exclusive, so add one to the marked pixel coordinate.
                    rightEdge = Math.Max(rightEdge, x + (byteIndex * 8) + bitPosition + 1);
                    break;
                }

                break;
            }
        }

        return rightEdge;
    }
}
