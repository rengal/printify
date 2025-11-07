using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Core;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;

namespace Printify.Infrastructure.Printing.EscPos;

public class EscPosPrintJobSession : PrintJobSession
{
    private const byte Esc = 0x1B;
    private const byte Gs = 0x1D;
    private const byte Lf = 0x0A;
    private const byte Bell = 0x07;
    private const byte Fs = 0x1C;
    private const byte FsSelectChinese = 0x26; // '&'
    private const byte EscSelectCodePageCommand = (byte)'t';

    public override event Func<IPrintJobSession, PrintJobSessionDataTimedOutEventArgs, ValueTask>? DataTimedOut;

    static EscPosPrintJobSession()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static readonly IReadOnlyDictionary<byte, string> EscCodePageMap = new Dictionary<byte, string>
    {
        [0x00] = "437",
        [0x20] = "720",
        [0x0E] = "737",
        [0x21] = "775",
        [0x02] = "850",
        [0x12] = "852",
        [0x22] = "855",
        [0x0D] = "857",
        [0x13] = "858",
        [0x03] = "860",
        [0x23] = "861",
        [0x24] = "862",
        [0x04] = "863",
        [0x25] = "864",
        [0x05] = "865",
        [0x11] = "866",
        [0x26] = "869",
        [0x29] = "1098",
        [0x2A] = "1118",
        [0x2B] = "1119",
        [0x2C] = "1125",
        [0x2D] = "1250",
        [0x2E] = "1251",
        [0x10] = "1252",
        [0x2F] = "1253",
        [0x30] = "1254",
        [0x31] = "1255",
        [0x32] = "1256",
        [0x33] = "1257",
        [0x34] = "1258"
    };

    private static readonly IReadOnlyDictionary<byte, PulsePin> PulsePinMap = new Dictionary<byte, PulsePin>
    {
        [0x00] = PulsePin.Drawer1,
        [0x01] = PulsePin.Drawer2
    };

    private IList<Element> ElementBuffer => MutableElements;
    private readonly List<byte> textBytes = new();
    private readonly List<byte> commandBuffer = new();
    private int processedOffset = 0;
    private int? activeTextLineIndex;
    private Encoding currentEncoding = Encoding.GetEncoding(437);
    private string? pendingQrData;
    private IClock idleClock;
    private readonly EscPosParser parser;
    private readonly ParserState parserState = new();

    public EscPosPrintJobSession(IClockFactory clockFactory, PrintJob job, IPrinterChannel channel) : base(clockFactory, job, channel)
    {
        ArgumentNullException.ThrowIfNull(clockFactory);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(channel);
        idleClock = clockFactory.Create();
        parser = new EscPosParser([]); //todo debugnow
    }

    public override Task Feed(ReadOnlyMemory<byte> input, CancellationToken ct)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        // Append new data to the buffer
        commandBuffer.AddRange(input.Span);

        // Process from where we left off
        var data = CollectionsMarshal.AsSpan(commandBuffer);
        var index = processedOffset;

        while (index < data.Length)
        {
            var value = data[index];

            if (IsPrintable(value))
            {
                // Printable character.
                // ASCII: {printable}
                // HEX: {value:X2}
                AppendPrintable(value);
                index++;
                processedOffset = index;
                continue;
            }

            CommitPendingText();

            if (value == Lf)
            {
                // Command: Line Feed - output current line and start a new one.
                // ASCII: LF
                // HEX: 0A
                FlushText(allowEmpty: true);
                index++;
                processedOffset = index;
                continue;
            }

            if (value == Esc)
            {
                if (index + 1 >= data.Length)
                {
                    // Incomplete ESC command - wait for more data
                    break;
                }

                var command = data[index + 1];

                // Command: ESC i / ESC m - paper cut.
                // ASCII: ESC {i|m}
                // HEX: 1B {69|6D}
                if (command == (byte)'i' || command == (byte)'m')
                {
                    FlushText(allowEmpty: false);
                    ElementBuffer.Add(new Pagecut());
                    index += 2;
                    processedOffset = index;
                    continue;
                }

                // Command: ESC t n - select character code table.
                // ASCII: ESC t n
                // HEX: 1B 74 0xNN
                if (command == EscSelectCodePageCommand)
                {
                    if (index + 2 >= data.Length)
                    {
                        // Incomplete command - wait for parameter
                        break;
                    }

                    var codePageId = data[index + 2];
                    if (EscCodePageMap.TryGetValue(codePageId, out var codePage))
                    {
                        FlushText(allowEmpty: false);
                        UpdateCodePage(codePage);
                        ElementBuffer.Add(new SetCodePage(codePage));
                    }

                    index += 3;
                    processedOffset = index;
                    continue;
                }

                // Command: ESC p m t1 t2 - cash drawer pulse.
                // ASCII: ESC p m t1 t2
                // HEX: 1B 70 0xMM 0xT1 0xT2
                if (command == (byte)'p')
                {
                    if (index + 4 >= data.Length)
                    {
                        // Incomplete command - wait for all parameters
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var pinId = data[index + 2];
                    var onTime = data[index + 3];
                    var offTime = data[index + 4];
                    var pin = PulsePinMap.GetValueOrDefault(pinId, PulsePin.Drawer1);
                    var onTimeMs = onTime * 2;
                    var offTimeMs = offTime * 2;
                    ElementBuffer.Add(new Pulse(pin, onTimeMs, offTimeMs));
                    index += 5;
                    processedOffset = index;
                    continue;
                }

                // Command: ESC E n - enable/disable emphasized (bold) mode.
                // ASCII: ESC E n
                // HEX: 1B 45 0xNN (00=off, 01=on)
                if (command == (byte)'E')
                {
                    if (index + 2 >= data.Length)
                    {
                        // Incomplete command
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var enabled = data[index + 2] != 0;
                    ElementBuffer.Add(new SetBoldMode(enabled));
                    index += 3;
                    processedOffset = index;
                    continue;
                }

                // Command: ESC - n - enable/disable underline mode.
                // ASCII: ESC - n
                // HEX: 1B 2D 0xNN (00=off, 01=on)
                if (command == 0x2D)
                {
                    if (index + 2 >= data.Length)
                    {
                        // Incomplete command
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var enabled = data[index + 2] != 0;
                    ElementBuffer.Add(new SetUnderlineMode(enabled));
                    index += 3;
                    processedOffset = index;
                    continue;
                }

                // Command: ESC a n - select justification.
                // ASCII: ESC a n
                // HEX: 1B 61 0xNN (00=left, 01=center, 02=right)
                if (command == (byte)'a')
                {
                    if (index + 2 >= data.Length)
                    {
                        // Incomplete command
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var justificationValue = data[index + 2];
                    if (TryGetJustification(justificationValue, out var justification))
                    {
                        ElementBuffer.Add(new SetJustification(justification));
                    }

                    index += 3;
                    processedOffset = index;
                    continue;
                }


                // Command: ESC @ - reset printer.
                // ASCII: ESC @
                // HEX: 1B 40
                if (command == 0x40)
                {
                    FlushText(allowEmpty: false);
                    ElementBuffer.Add(new ResetPrinter());
                    index += 2;
                    processedOffset = index;
                    continue;
                }

                // Command: ESC ! n - select font characteristics.
                // ASCII: ESC ! n
                // HEX: 1B 21 0xNN
                if (command == 0x21)
                {
                    if (index + 2 >= data.Length)
                    {
                        // Incomplete command
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var parameter = data[index + 2];
                    var fontNumber = parameter & 0x07;
                    var isDoubleHeight = (parameter & 0x10) != 0;
                    var isDoubleWidth = (parameter & 0x20) != 0;
                    ElementBuffer.Add(new SetFont(fontNumber, isDoubleWidth, isDoubleHeight));
                    index += 3;
                    processedOffset = index;
                    continue;
                }

                // Command: ESC 3 n - set line spacing.
                // ASCII: ESC 3 n
                // HEX: 1B 33 0xNN
                if (command == 0x33)
                {
                    if (index + 2 >= data.Length)
                    {
                        // Incomplete command
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var spacing = data[index + 2];
                    ElementBuffer.Add(new SetLineSpacing(spacing));
                    index += 3;
                    processedOffset = index;
                    continue;
                }

                // Command: ESC 2 - set default line spacing (approx. 30 dots).
                // ASCII: ESC 2
                // HEX: 1B 32
                if (command == 0x32)
                {
                    FlushText(allowEmpty: false);
                    ElementBuffer.Add(new ResetLineSpacing());
                    index += 2;
                    processedOffset = index;
                    continue;
                }

                // Unknown ESC command - skip it
                index += 2;
                processedOffset = index;
                continue;
            }

            if (value == Gs)
            {
                if (index + 1 >= data.Length)
                {
                    // Incomplete GS command
                    break;
                }

                var command = data[index + 1];
                // Command: GS ( k - QR configuration and print workflow.
                // ASCII: GS ( k
                // HEX: 1D 28 6B pL pH cn fn [data]
                if (command == 0x28)
                {
                    if (index + 4 >= data.Length)
                    {
                        // Incomplete command - need at least pL pH
                        break;
                    }

                    var parameterLength = data[index + 3] | (data[index + 4] << 8);
                    if (index + 5 + parameterLength > data.Length)
                    {
                        // Incomplete command - don't have full payload yet
                        break;
                    }

                    if (parameterLength >= 2)
                    {
                        var cn = data[index + 5];
                        var fn = data[index + 6];
                        var payloadLength = parameterLength - 2;
                        var payloadSpan = payloadLength > 0 ? data.Slice(index + 7, payloadLength) : ReadOnlySpan<byte>.Empty;
                        var handled = false;

                        if (cn == 0x31)
                        {
                            switch (fn)
                            {
                                case 0x41:
                                    if (payloadSpan.Length > 0 && TryGetQrModel(payloadSpan[0], out var model))
                                    {
                                        FlushText(allowEmpty: false);
                                        ElementBuffer.Add(new SetQrModel(model));
                                        handled = true;
                                    }

                                    break;

                                case 0x43:
                                    if (payloadSpan.Length > 0)
                                    {
                                        FlushText(allowEmpty: false);
                                        ElementBuffer.Add(new SetQrModuleSize(payloadSpan[0]));
                                        handled = true;
                                    }

                                    break;

                                case 0x45:
                                    if (payloadSpan.Length > 0 && TryGetQrErrorCorrection(payloadSpan[0], out var level))
                                    {
                                        FlushText(allowEmpty: false);
                                        ElementBuffer.Add(new SetQrErrorCorrection(level));
                                        handled = true;
                                    }

                                    break;

                                case 0x50:
                                    {
                                        var contentSpan = payloadSpan.Length > 1 ? payloadSpan.Slice(1) : ReadOnlySpan<byte>.Empty;
                                        var content = contentSpan.Length > 0 ? currentEncoding.GetString(contentSpan) : string.Empty;
                                        pendingQrData = content;
                                        FlushText(allowEmpty: false);
                                        ElementBuffer.Add(new StoreQrData(content));
                                        handled = true;
                                        break;
                                    }

                                case 0x51:
                                    {
                                        var content = pendingQrData ?? string.Empty;
                                        FlushText(allowEmpty: false);
                                        ElementBuffer.Add(new PrintQrCode(content));
                                        handled = true;
                                        break;
                                    }
                            }
                        }

                        index += 5 + parameterLength;
                        processedOffset = index;
                        if (handled)
                        {
                            continue;
                        }

                        continue;
                    }

                    index += 5 + parameterLength;
                    processedOffset = index;
                    continue;
                }


                // Command: GS v 0 m xL xH yL yH [data] - raster bit image print.
                // ASCII: GS v 0
                // HEX: 1D 76 30 m xL xH yL yH ...
                if (command == 0x76)
                {
                    if (index + 7 >= data.Length)
                    {
                        break;
                    }

                    if (data[index + 2] == 0x30)
                    {
                        var mode = data[index + 3];
                        var widthBytes = data[index + 4] | (data[index + 5] << 8);
                        var height = data[index + 6] | (data[index + 7] << 8);
                        var payloadLength = widthBytes * height;

                        if (payloadLength > 0)
                        {
                            if (index + 8 + payloadLength > data.Length)
                            {
                                // Incomplete image data
                                break;
                            }

                            var payload = data.Slice(index + 8, payloadLength).ToArray();
                            FlushText(allowEmpty: false);
                            //StoreRasterImage(payload, widthBytes, height);
                            index += 8 + payloadLength;
                            processedOffset = index;
                            continue;
                        }
                    }
                }

                // Command: GS a n - real-time printer status.
                // ASCII: GS a n
                // HEX: 1D 61 0xNN
                if (command == 0x61)
                {
                    if (index + 2 >= data.Length)
                    {
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var status = data[index + 2];
                    ElementBuffer.Add(new PrinterStatus(status, null));
                    index += 3;
                    processedOffset = index;
                    continue;
                }

                // Command: GS h n - set barcode height.
                // ASCII: GS h n
                // HEX: 1D 68 0xNN
                if (command == 0x68)
                {
                    if (index + 2 >= data.Length)
                    {
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var height = data[index + 2];
                    ElementBuffer.Add(new SetBarcodeHeight(height));
                    index += 3;
                    processedOffset = index;
                    continue;
                }

                // Command: GS w n - set barcode module width.
                // ASCII: GS w n
                // HEX: 1D 77 0xNN
                if (command == 0x77)
                {
                    if (index + 2 >= data.Length)
                    {
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var width = data[index + 2];
                    ElementBuffer.Add(new SetBarcodeModuleWidth(width));
                    index += 3;
                    processedOffset = index;
                    continue;
                }

                // Command: GS H n - set barcode label position.
                // ASCII: GS H n
                // HEX: 1D 48 0xNN
                if (command == 0x48)
                {
                    if (index + 2 >= data.Length)
                    {
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var positionValue = data[index + 2];
                    if (TryGetBarcodeLabelPosition(positionValue, out var position))
                    {
                        ElementBuffer.Add(new SetBarcodeLabelPosition(position));
                    }

                    index += 3;
                    processedOffset = index;
                    continue;
                }

                // Command: GS k m d... - print barcode.
                // ASCII: GS k m d1... or GS k m k d1...
                // HEX: 1D 6B 0xMM ...
                if (command == 0x6B)
                {
                    if (index + 2 >= data.Length)
                    {
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var barcodeType = data[index + 2];
                    if (!TryResolveBarcodeSymbology(barcodeType, out var symbology, out var usesLengthIndicator))
                    {
                        index += 3;
                        processedOffset = index;
                        continue;
                    }

                    if (usesLengthIndicator)
                    {
                        if (index + 3 >= data.Length)
                        {
                            break;
                        }

                        var length = data[index + 3];
                        var payloadStart = index + 4;
                        if (payloadStart + length > data.Length)
                        {
                            break;
                        }

                        var payload = data.Slice(payloadStart, length).ToArray();
                        var content = currentEncoding.GetString(payload);
                        ElementBuffer.Add(new PrintBarcode(symbology, content));
                        index += 4 + length;
                        processedOffset = index;
                        continue;
                    }
                    else
                    {
                        var payloadStart = index + 3;
                        var terminatorIndex = FindNullTerminator(data, payloadStart);
                        if (terminatorIndex == -1)
                        {
                            // No terminator found - might be incomplete
                            break;
                        }

                        var payloadLength = terminatorIndex - payloadStart;
                        var payload = payloadLength > 0 ? data.Slice(payloadStart, payloadLength).ToArray() : Array.Empty<byte>();
                        var content = currentEncoding.GetString(payload);
                        ElementBuffer.Add(new PrintBarcode(symbology, content));
                        index = terminatorIndex + 1;
                        processedOffset = index;
                        continue;
                    }
                }

                // Command: GS B n - enable/disable reverse (white-on-black) mode.
                // ASCII: GS B n
                // HEX: 1D 42 0xNN (00=off, 01=on)
                if (command == 0x42)
                {
                    if (index + 2 >= data.Length)
                    {
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var enabled = data[index + 2] != 0;
                    ElementBuffer.Add(new SetReverseMode(enabled));
                    index += 3;
                    processedOffset = index;
                    continue;
                }

                // Command: GS V m [n] - paper cut with mode.
                // ASCII: GS V m [n]
                // HEX: 1D 56 0xMM [0xNN]
                if (command == 0x56)
                {
                    if (index + 2 >= data.Length)
                    {
                        break;
                    }

                    FlushText(allowEmpty: false);
                    var skip = index + 3 < data.Length ? 3 : 2;
                    index += skip;
                    processedOffset = index;
                    ElementBuffer.Add(new Pagecut());
                    continue;
                }

                // Unknown GS command
                index += 2;
                processedOffset = index;
                continue;
            }

            if (value == Fs)
            {
                if (index + 1 >= data.Length)
                {
                    break;
                }

                var fsCommand = data[index + 1];

                if (fsCommand == FsSelectChinese)
                {
                    // Command: FS & - select Chinese (GB2312) character set.
                    // ASCII: FS &
                    // HEX: 1C 26
                    FlushText(allowEmpty: false);
                    UpdateCodePage("936");
                    ElementBuffer.Add(new SetCodePage("936"));
                    index += 2;
                    processedOffset = index;
                    continue;
                }

                if (fsCommand == (byte)'p')
                {
                    if (index + 3 >= data.Length)
                    {
                        break;
                    }

                    // Command: FS p m n - print stored logo by identifier.
                    // ASCII: FS p m n
                    // HEX: 1C 70 0xMM 0xNN
                    FlushText(allowEmpty: false);
                    var logoId = data[index + 3];
                    ElementBuffer.Add(new StoredLogo(logoId));
                    index += 4;
                    processedOffset = index;
                    continue;
                }

                // Unknown FS command
                index += 2;
                processedOffset = index;
                continue;
            }

            if (value == Bell)
            {
                // Command: BEL - buzzer/beeper.
                // ASCII: BEL
                // HEX: 07
                FlushText(allowEmpty: false);
                ElementBuffer.Add(new Bell());
                index++;
                processedOffset = index;
                continue;
            }
        }

        CommitPendingText();

        // Trim buffer periodically to prevent unbounded growth
        if (processedOffset > 1024)
        {
            commandBuffer.RemoveRange(0, processedOffset);
            processedOffset = 0;
        }

        idleClock.Restart();
        _ = IdleTimeoutAsync(ct);
        return base.Feed(input, ct);
    }

    private async Task IdleTimeoutAsync(CancellationToken ct)
    {
        try
        {
            await idleClock.DelayAsync(TimeSpan.FromMilliseconds(PrinterConstants.ListenerIdleTimeoutMs), ct);
            if (!IsCompleted && DataTimedOut != null)
            {
                var args = new PrintJobSessionDataTimedOutEventArgs(Channel, ct);
                await DataTimedOut.Invoke(this, args).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected if new data arrives or job is canceled
        }
    }

    public override Task Complete(PrintJobCompletionReason reason)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        // Drain the buffer to capture the latest busy state before finalizing.
        UpdateBufferState();

        CommitPendingText();
        FlushText(allowEmpty: false);

        var snapshot = ElementBuffer.ToArray();
        var document = new Document(
            Guid.NewGuid(),
            Job.Id,
            Printer.Id,
            DateTimeOffset.UtcNow,
            Printer.Protocol,
            Channel.ClientAddress,
            snapshot);
        SetDocument(document);

        return base.Complete(reason);
    }

    private void TriggerOverflow()
    {
        if (!HasOverflow)
            return;

        var message = string.Format(CultureInfo.InvariantCulture, "Simulated buffer overflow after {0:0} bytes",
            bufferedBytes);
        ElementBuffer.Add(new PrinterError(message));
    }

    private void AppendPrintable(byte value)
    {
        if (!activeTextLineIndex.HasValue)
        {
            textBytes.Clear();
            activeTextLineIndex = ElementBuffer.Count;
            ElementBuffer.Add(new TextLine(string.Empty));
        }

        textBytes.Add(value);
    }

    private void CommitPendingText()
    {
        if (!activeTextLineIndex.HasValue)
        {
            return;
        }

        var index = activeTextLineIndex.Value;
        var existing = (TextLine)ElementBuffer[index];
        var text = currentEncoding.GetString(textBytes.ToArray());
        ElementBuffer[index] = existing with { Text = text };
    }

    private void FlushText(bool allowEmpty)
    {
        if (activeTextLineIndex.HasValue)
        {
            CommitPendingText();
            activeTextLineIndex = null;
            textBytes.Clear();
            return;
        }

        if (allowEmpty)
        {
            ElementBuffer.Add(new TextLine(string.Empty));
        }
    }

    // private void StoreRasterImage(byte[] payload, int widthBytes, int height)
    // {
    //     if (payload.Length == 0 || widthBytes <= 0 || height <= 0)
    //     {
    //         return;
    //     }
    //
    //     var widthDots = widthBytes * 8;
    //
    //     using var image = new Image<L8>(widthDots, height);
    //     for (var y = 0; y < height; y++)
    //     {
    //         for (var x = 0; x < widthDots; x++)
    //         {
    //             var byteIndex = y * widthBytes + (x / 8);
    //             if (byteIndex >= payload.Length)
    //             {
    //                 break;
    //             }
    //
    //             var bitIndex = 7 - (x % 8);
    //             var isBlack = (payload[byteIndex] & (1 << bitIndex)) != 0;
    //             image[x, y] = isBlack ? new L8(0) : new L8(255);
    //         }
    //     }
    //
    //     using var pngStream = new MemoryStream();
    //     image.SaveAsPng(pngStream, new PngEncoder { ColorType = PngColorType.Grayscale });
    //     var buffer = pngStream.ToArray();
    //     var checksum = Convert.ToHexString(SHA256.HashData(buffer));
    //
    //     var mediaMeta = new MediaMeta("image/png", buffer.LongLength, checksum);
    //
    //     var media = new MediaContent(mediaMeta, buffer.AsMemory());
    //     ElementBuffer.Add(new RasterImageContent(++sequence, widthDots, height, media));
    // }

    private static bool TryGetJustification(byte value, out TextJustification justification)
    {
        switch (value)
        {
            case 0:
                justification = TextJustification.Left;
                return true;
            case 1:
                justification = TextJustification.Center;
                return true;
            case 2:
                justification = TextJustification.Right;
                return true;
            default:
                justification = default;
                return false;
        }
    }

    private static bool TryGetQrModel(byte value, out QrModel model)
    {
        switch (value)
        {
            case 0x31:
            case 0x01:
                model = QrModel.Model1;
                return true;
            case 0x32:
            case 0x02:
                model = QrModel.Model2;
                return true;
            case 0x33:
            case 0x03:
                model = QrModel.Micro;
                return true;
            default:
                model = default;
                return false;
        }
    }

    private static bool TryGetQrErrorCorrection(byte value, out QrErrorCorrectionLevel level)
    {
        switch (value)
        {
            case (byte)'L':
            case 0x30:
            case 0x00:
                level = QrErrorCorrectionLevel.Low;
                return true;
            case (byte)'M':
            case 0x31:
            case 0x01:
                level = QrErrorCorrectionLevel.Medium;
                return true;
            case (byte)'Q':
            case 0x32:
            case 0x02:
                level = QrErrorCorrectionLevel.Quartile;
                return true;
            case (byte)'H':
            case 0x33:
            case 0x03:
                level = QrErrorCorrectionLevel.High;
                return true;
            default:
                level = default;
                return false;
        }
    }

    private static bool TryGetBarcodeLabelPosition(byte value, out BarcodeLabelPosition position)
    {
        if (value <= 3)
        {
            position = (BarcodeLabelPosition)value;
            return true;
        }

        position = default;
        return false;
    }

    private static bool TryResolveBarcodeSymbology(byte value, out BarcodeSymbology symbology, out bool usesLengthIndicator)
    {
        switch (value)
        {
            case 0x00:
            case 0x41:
                symbology = BarcodeSymbology.UpcA;
                usesLengthIndicator = value >= 0x41;
                return true;
            case 0x01:
            case 0x42:
                symbology = BarcodeSymbology.UpcE;
                usesLengthIndicator = value >= 0x42;
                return true;
            case 0x02:
            case 0x43:
                symbology = BarcodeSymbology.Ean13;
                usesLengthIndicator = value >= 0x43;
                return true;
            case 0x03:
            case 0x44:
                symbology = BarcodeSymbology.Ean8;
                usesLengthIndicator = value >= 0x44;
                return true;
            case 0x04:
            case 0x45:
                symbology = BarcodeSymbology.Code39;
                usesLengthIndicator = value >= 0x45;
                return true;
            case 0x05:
            case 0x46:
                symbology = BarcodeSymbology.Itf;
                usesLengthIndicator = value >= 0x46;
                return true;
            case 0x06:
            case 0x47:
                symbology = BarcodeSymbology.Codabar;
                usesLengthIndicator = value >= 0x47;
                return true;
            case 0x48:
                symbology = BarcodeSymbology.Code93;
                usesLengthIndicator = true;
                return true;
            case 0x49:
                symbology = BarcodeSymbology.Code128;
                usesLengthIndicator = true;
                return true;
            default:
                symbology = default;
                usesLengthIndicator = false;
                return false;
        }
    }

    private static int FindNullTerminator(ReadOnlySpan<byte> data, int startIndex)
    {
        for (var index = startIndex; index < data.Length; index++)
        {
            if (data[index] == 0)
            {
                return index;
            }
        }

        return -1;
    }
    private void UpdateCodePage(string codePage)
    {
        try
        {
            if (int.TryParse(codePage, out var numericCodePage))
            {
                currentEncoding = Encoding.GetEncoding(numericCodePage);
            }
            else
            {
                currentEncoding = Encoding.GetEncoding(codePage);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            // Leave current encoding unchanged if code page is unsupported by the runtime.
        }
    }

    private static bool IsPrintable(byte value)
    {
        return value >= 0x20 && value != 0x7F;
    }
}
