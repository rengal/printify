using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Printify.Domain.Config;
using Printify.Domain.Core;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Media;
using Printify.Domain.Printers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Printify.Services.Tokenizer;

internal sealed class EscPosTokenizerSession : ITokenizerSession
{
    private const byte Fs = 0x1C; // FS
    private const byte FsSelectChinese = 0x26; // '&'
    private const byte EscSelectCodePageCommand = (byte)'t';

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

    static EscPosTokenizerSession()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private readonly BufferOptions bufferOptions;
    private double bufferedBytes;
    private long lastDrainSampleMs;
    private bool hasOverflow;

    private bool IsBufferTrackingEnabled => bufferOptions.DrainRate.HasValue || bufferOptions.MaxCapacity.HasValue;

    private readonly IClock clock;
    private readonly List<Element> elements = new();
    private readonly List<byte> textBytes = new();
    private int? activeTextLineIndex;
    private int sequence;
    private long totalConsumed;
    private bool isCompleted;
    private Document? document;
    private Encoding currentEncoding = Encoding.GetEncoding(437);
    private string? pendingQrData;

    public EscPosTokenizerSession(IOptions<BufferOptions> bufferOptions, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        this.bufferOptions = bufferOptions.Value;
        this.clock = clock;
        this.clock.Start();
        lastDrainSampleMs = this.clock.ElapsedMs;
    }

    public int Sequence => sequence;

    public long TotalConsumed => totalConsumed;

    public IReadOnlyList<Element> Elements => elements;

    /// <inheritdoc />
    public Document? Document
    {
        get
        {
            if (!isCompleted)
            {
                throw new InvalidOperationException("Document is only available after the session completes.");
            }

            return document;
        }
    }

    public bool IsCompleted => isCompleted;

    /// <inheritdoc />
    public bool IsBufferBusy
    {
        get
        {
            // Advance the simulated buffer state using the injected clock before sampling busy status.
            UpdateBufferState();

            if (!IsBufferTrackingEnabled)
            {
                return false;
            }

            if (!bufferOptions.DrainRate.HasValue)
            {
                return false;
            }

            var threshold = bufferOptions.BusyThreshold is > 0 ? bufferOptions.BusyThreshold : 1;
            return bufferedBytes >= threshold;
        }
    }

    /// <inheritdoc />
    public bool HasOverflow => hasOverflow;

    public void Feed(ReadOnlySpan<byte> data)
    {
        if (isCompleted)
        {
            throw new InvalidOperationException("Cannot feed a completed tokenizer session.");
        }

        // Drain the simulated buffer before processing the newly received bytes.
        UpdateBufferState();

        for (var index = 0; index < data.Length; index++)
        {
            var value = data[index];

            if (IsPrintable(value))
            {
                // Printable character.
                // ASCII: {printable}
                // HEX: {value:X2}
                AppendPrintable(value);
                continue;
            }

            CommitPendingText();

            if (value == Services.Tokenizer.EscPosTokenizer.Lf)
            {
                // Command: Line Feed - output current line and start a new one.
                // ASCII: LF
                // HEX: 0A
                FlushText(allowEmpty: true);
                continue;
            }

            if (value == Services.Tokenizer.EscPosTokenizer.Esc)
            {
                if (index + 1 < data.Length)
                {
                    var command = data[index + 1];

                    // Command: ESC i / ESC m - paper cut.
                    // ASCII: ESC {i|m}
                    // HEX: 1B {69|6D}
                    if (command == (byte)'i' || command == (byte)'m')
                    {
                        FlushText(allowEmpty: false);
                        elements.Add(new Pagecut(++sequence));
                        index += 1;
                        continue;
                    }

                    // Command: ESC t n - select character code table.
                    // ASCII: ESC t n
                    // HEX: 1B 74 0xNN
                    if (command == EscSelectCodePageCommand && index + 2 < data.Length)
                    {
                        var codePageId = data[index + 2];
                        if (EscCodePageMap.TryGetValue(codePageId, out var codePage))
                        {
                            FlushText(allowEmpty: false);
                            UpdateCodePage(codePage);
                            elements.Add(new SetCodePage(++sequence, codePage));
                        }

                        index += 2;
                        continue;
                    }

                    // Command: ESC p m t1 t2 - cash drawer pulse.
                    // ASCII: ESC p m t1 t2
                    // HEX: 1B 70 0xMM 0xT1 0xT2
                    if (command == (byte)'p' && index + 4 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var pinId = data[index + 2];
                        var onTime = data[index + 3];
                        var offTime = data[index + 4];
                        var pin = PulsePinMap.GetValueOrDefault(pinId, PulsePin.Drawer1);
                        var onTimeMs = onTime * 2;
                        var offTimeMs = offTime * 2;
                        elements.Add(new Pulse(++sequence, pin, onTimeMs, offTimeMs));
                        index += 4;
                        continue;
                    }

                    // Command: ESC E n - enable/disable emphasized (bold) mode.
                    // ASCII: ESC E n
                    // HEX: 1B 45 0xNN (00=off, 01=on)
                    if (command == (byte)'E' && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var enabled = data[index + 2] != 0;
                        elements.Add(new SetBoldMode(++sequence, enabled));
                        index += 2;
                        continue;
                    }

                    // Command: ESC - n - enable/disable underline mode.
                    // ASCII: ESC - n
                    // HEX: 1B 2D 0xNN (00=off, 01=on)
                    if (command == 0x2D && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var enabled = data[index + 2] != 0;
                        elements.Add(new SetUnderlineMode(++sequence, enabled));
                        index += 2;
                        continue;
                    }

                    // Command: ESC a n - select justification.
                    // ASCII: ESC a n
                    // HEX: 1B 61 0xNN (00=left, 01=center, 02=right)
                    if (command == (byte)'a' && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var justificationValue = data[index + 2];
                        if (TryGetJustification(justificationValue, out var justification))
                        {
                            elements.Add(new SetJustification(++sequence, justification));
                        }

                        index += 2;
                        continue;
                    }


                    // Command: ESC @ - reset printer.
                    // ASCII: ESC @
                    // HEX: 1B 40
                    if (command == 0x40)
                    {
                        FlushText(allowEmpty: false);
                        elements.Add(new ResetPrinter(++sequence));
                        index += 1;
                        continue;
                    }

                    // Command: ESC ! n - select font characteristics.
                    // ASCII: ESC ! n
                    // HEX: 1B 21 0xNN
                    if (command == 0x21 && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var parameter = data[index + 2];
                        var fontNumber = parameter & 0x07;
                        var isDoubleHeight = (parameter & 0x10) != 0;
                        var isDoubleWidth = (parameter & 0x20) != 0;
                        elements.Add(new SetFont(++sequence, fontNumber, isDoubleWidth, isDoubleHeight));
                        index += 2;
                        continue;
                    }

                    // Command: ESC 3 n - set line spacing.
                    // ASCII: ESC 3 n
                    // HEX: 1B 33 0xNN
                    if (command == 0x33 && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var spacing = data[index + 2];
                        elements.Add(new SetLineSpacing(++sequence, spacing));
                        index += 2;
                        continue;
                    }

                    // Command: ESC 2 - set default line spacing (approx. 30 dots).
                    // ASCII: ESC 2
                    // HEX: 1B 32
                    if (command == 0x32)
                    {
                        FlushText(allowEmpty: false);
                        elements.Add(new SetLineSpacing(++sequence, 30));
                        index += 1;
                        continue;
                    }
                }

                continue;
            }

            if (value == Services.Tokenizer.EscPosTokenizer.Gs)
            {
                if (index + 1 < data.Length)
                {
                    var command = data[index + 1];
                    // Command: GS ( k - QR configuration and print workflow.
                    // ASCII: GS ( k
                    // HEX: 1D 28 6B pL pH cn fn [data]
                    if (command == 0x28 && index + 3 < data.Length)
                    {
                        var parameterLength = data[index + 3] | (data[index + 4] << 8);
                        if (index + 5 + parameterLength <= data.Length && parameterLength >= 2)
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
                                            elements.Add(new SetQrModel(++sequence, model));
                                            handled = true;
                                        }

                                        break;

                                    case 0x43:
                                        if (payloadSpan.Length > 0)
                                        {
                                            FlushText(allowEmpty: false);
                                            elements.Add(new SetQrModuleSize(++sequence, payloadSpan[0]));
                                            handled = true;
                                        }

                                        break;

                                    case 0x45:
                                        if (payloadSpan.Length > 0 && TryGetQrErrorCorrection(payloadSpan[0], out var level))
                                        {
                                            FlushText(allowEmpty: false);
                                            elements.Add(new SetQrErrorCorrection(++sequence, level));
                                            handled = true;
                                        }

                                        break;

                                    case 0x50:
                                    {
                                        var contentSpan = payloadSpan.Length > 1 ? payloadSpan.Slice(1) : ReadOnlySpan<byte>.Empty;
                                        var content = contentSpan.Length > 0 ? currentEncoding.GetString(contentSpan) : string.Empty;
                                        pendingQrData = content;
                                        FlushText(allowEmpty: false);
                                        elements.Add(new StoreQrData(++sequence, content));
                                        handled = true;
                                        break;
                                    }

                                    case 0x51:
                                    {
                                        var content = pendingQrData ?? string.Empty;
                                        FlushText(allowEmpty: false);
                                        elements.Add(new PrintQrCode(++sequence, content));
                                        handled = true;
                                        break;
                                    }
                                }
                            }

                            index += 4 + parameterLength;
                            if (handled)
                            {
                                continue;
                            }

                            continue;
                        }

                        index += 4 + parameterLength;
                        continue;
                    }


                    // Command: GS v 0 m xL xH yL yH [data] - raster bit image print.
                    // ASCII: GS v 0
                    // HEX: 1D 76 30 m xL xH yL yH ...
                    if (command == 0x76 && index + 7 < data.Length && data[index + 2] == 0x30)
                    {
                        var mode = data[index + 3];
                        var widthBytes = data[index + 4] | (data[index + 5] << 8);
                        var height = data[index + 6] | (data[index + 7] << 8);
                        var payloadLength = widthBytes * height;

                        if (payloadLength > 0 && index + 8 + payloadLength <= data.Length)
                        {
                            var payload = data.Slice(index + 8, payloadLength).ToArray();
                            FlushText(allowEmpty: false);
                            StoreRasterImage(payload, widthBytes, height);
                            index += 7 + payloadLength;
                            continue;
                        }
                    }

                    // Command: GS a n - real-time printer status.
                    // ASCII: GS a n
                    // HEX: 1D 61 0xNN
                    if (command == 0x61 && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var status = data[index + 2];
                        elements.Add(new PrinterStatus(++sequence, status, null));
                        index += 2;
                        continue;
                    }

                    // Command: GS h n - set barcode height.
                    // ASCII: GS h n
                    // HEX: 1D 68 0xNN
                    if (command == 0x68 && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var height = data[index + 2];
                        elements.Add(new SetBarcodeHeight(++sequence, height));
                        index += 2;
                        continue;
                    }

                    // Command: GS w n - set barcode module width.
                    // ASCII: GS w n
                    // HEX: 1D 77 0xNN
                    if (command == 0x77 && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var width = data[index + 2];
                        elements.Add(new SetBarcodeModuleWidth(++sequence, width));
                        index += 2;
                        continue;
                    }

                    // Command: GS H n - set barcode label position.
                    // ASCII: GS H n
                    // HEX: 1D 48 0xNN
                    if (command == 0x48 && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var positionValue = data[index + 2];
                        if (TryGetBarcodeLabelPosition(positionValue, out var position))
                        {
                            elements.Add(new SetBarcodeLabelPosition(++sequence, position));
                        }

                        index += 2;
                        continue;
                    }

                    // Command: GS k m d... - print barcode.
                    // ASCII: GS k m d1... or GS k m k d1...
                    // HEX: 1D 6B 0xMM ...
                    if (command == 0x6B && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var barcodeType = data[index + 2];
                        if (!TryResolveBarcodeSymbology(barcodeType, out var symbology, out var usesLengthIndicator))
                        {
                            index += 2;
                            continue;
                        }

                        if (usesLengthIndicator)
                        {
                            if (index + 3 >= data.Length)
                            {
                                index += 2;
                                continue;
                            }

                            var length = data[index + 3];
                            var payloadStart = index + 4;
                            if (payloadStart + length > data.Length)
                            {
                                index += 3;
                                continue;
                            }

                            var payload = data.Slice(payloadStart, length).ToArray();
                            var content = currentEncoding.GetString(payload);
                            elements.Add(new PrintBarcode(++sequence, symbology, content));
                            index += 3 + length;
                            continue;
                        }
                        else
                        {
                            var payloadStart = index + 3;
                            var terminatorIndex = FindNullTerminator(data, payloadStart);
                            if (terminatorIndex == -1)
                            {
                                index = data.Length - 1;
                                continue;
                            }

                            var payloadLength = terminatorIndex - payloadStart;
                            var payload = payloadLength > 0 ? data.Slice(payloadStart, payloadLength).ToArray() : Array.Empty<byte>();
                            var content = currentEncoding.GetString(payload);
                            elements.Add(new PrintBarcode(++sequence, symbology, content));
                            index = terminatorIndex;
                            continue;
                        }
                    }

                    // Command: GS B n - enable/disable reverse (white-on-black) mode.
                    // ASCII: GS B n
                    // HEX: 1D 42 0xNN (00=off, 01=on)
                    if (command == 0x42 && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var enabled = data[index + 2] != 0;
                        elements.Add(new SetReverseMode(++sequence, enabled));
                        index += 2;
                        continue;
                    }

                    // Command: GS V m [n] - paper cut with mode.
                    // ASCII: GS V m [n]
                    // HEX: 1D 56 0xMM [0xNN]
                    if (command == 0x56 && index + 2 < data.Length)
                    {
                        FlushText(allowEmpty: false);
                        var skip = index + 3 < data.Length ? 3 : 2;
                        index += skip;
                        elements.Add(new Pagecut(++sequence));
                        continue;
                    }
                }

                continue;
            }

            if (value == Fs)
            {
                if (index + 1 < data.Length)
                {
                    var fsCommand = data[index + 1];

                    if (fsCommand == FsSelectChinese)
                    {
                        // Command: FS & - select Chinese (GB2312) character set.
                        // ASCII: FS &
                        // HEX: 1C 26
                        FlushText(allowEmpty: false);
                        UpdateCodePage("936");
                        elements.Add(new SetCodePage(++sequence, "936"));
                        index += 1;
                        continue;
                    }

                    if (fsCommand == (byte)'p' && index + 3 < data.Length)
                    {
                        // Command: FS p m n - print stored logo by identifier.
                        // ASCII: FS p m n
                        // HEX: 1C 70 0xMM 0xNN
                        FlushText(allowEmpty: false);
                        var logoId = data[index + 3];
                        elements.Add(new StoredLogo(++sequence, logoId));
                        index += 3;
                        continue;
                    }
                }

                continue;
            }

            if (value == Services.Tokenizer.EscPosTokenizer.Bell)
            {
                // Command: BEL - buzzer/beeper.
                // ASCII: BEL
                // HEX: 07
                FlushText(allowEmpty: false);
                elements.Add(new Bell(++sequence));
                continue;
            }
        }

        totalConsumed += data.Length;
        CommitPendingText();
    }

    public void Complete(CompletionReason reason)
    {
        if (isCompleted)
        {
            throw new InvalidOperationException("Tokenizer session has already been completed.");
        }

        // Drain the buffer to capture the latest busy state before finalizing.
        UpdateBufferState();

        CommitPendingText();
        FlushText(allowEmpty: false);

        var snapshot = elements.ToArray();

        document = new Document(0, 0, DateTimeOffset.UtcNow, Protocol.EscPos, null, snapshot);
        isCompleted = true;
    }

    private void RegisterPrintingBytes(int count)
    {
        if (count <= 0)
        {
            return;
        }

        if (!IsBufferTrackingEnabled)
        {
            return;
        }

        UpdateBufferState();

        bufferedBytes += count;

        if (bufferOptions.MaxCapacity.HasValue && bufferedBytes > bufferOptions.MaxCapacity.Value)
        {
            TriggerOverflow();
        }
    }

    private void UpdateBufferState()
    {
        if (!IsBufferTrackingEnabled)
        {
            bufferedBytes = 0;
            lastDrainSampleMs = clock.ElapsedMs;
            return;
        }

        var currentMs = clock.ElapsedMs;

        if (bufferedBytes <= 0)
        {
            lastDrainSampleMs = currentMs;
            return;
        }

        if (!bufferOptions.DrainRate.HasValue)
        {
            bufferedBytes = 0;
            lastDrainSampleMs = currentMs;
            return;
        }

        if (bufferOptions.DrainRate.Value <= 0)
        {
            lastDrainSampleMs = currentMs;
            return;
        }

        var elapsedMs = currentMs - lastDrainSampleMs;
        if (elapsedMs <= 0)
        {
            return;
        }

        var drained = bufferOptions.DrainRate.Value * (elapsedMs / 1000.0);
        if (drained > 0)
        {
            // Reduce the buffered byte count while keeping it non-negative.
            bufferedBytes = Math.Max(0d, bufferedBytes - drained);
        }

        lastDrainSampleMs = currentMs;
    }

    private void TriggerOverflow()
    {
        if (!bufferOptions.MaxCapacity.HasValue)
        {
            return;
        }

        if (!hasOverflow)
        {
            hasOverflow = true;
            var message = string.Format(CultureInfo.InvariantCulture, "Simulated buffer overflow after {0:0} bytes", bufferedBytes);
            elements.Add(new PrinterError(++sequence, message));
        }

        if (bufferOptions.MaxCapacity.Value > 0)
        {
            bufferedBytes = Math.Min(bufferedBytes, bufferOptions.MaxCapacity.Value);
        }
        else
        {
            bufferedBytes = 0;
        }
    }

    private void AppendPrintable(byte value)
    {
        // Account for bytes that will be printed so busy/overflow tracking stays accurate.
        RegisterPrintingBytes(1);

        if (!activeTextLineIndex.HasValue)
        {
            textBytes.Clear();
            activeTextLineIndex = elements.Count;
            elements.Add(new TextLine(++sequence, string.Empty));
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
        var existing = (TextLine)elements[index];
        var text = currentEncoding.GetString(textBytes.ToArray());
        elements[index] = existing with { Text = text };
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
            elements.Add(new TextLine(++sequence, string.Empty));
        }
    }

    private void StoreRasterImage(byte[] payload, int widthBytes, int height)
    {
        if (payload.Length == 0 || widthBytes <= 0 || height <= 0)
        {
            return;
        }

        var widthDots = widthBytes * 8;

        using var image = new Image<L8>(widthDots, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < widthDots; x++)
            {
                var byteIndex = y * widthBytes + (x / 8);
                if (byteIndex >= payload.Length)
                {
                    break;
                }

                var bitIndex = 7 - (x % 8);
                var isBlack = (payload[byteIndex] & (1 << bitIndex)) != 0;
                image[x, y] = isBlack ? new L8(0) : new L8(255);
            }
        }

        using var pngStream = new MemoryStream();
        image.SaveAsPng(pngStream, new PngEncoder { ColorType = PngColorType.Grayscale });
        var buffer = pngStream.ToArray();
        var checksum = Convert.ToHexString(SHA256.HashData(buffer));

        var mediaMeta = new  MediaMeta("image/png", buffer.LongLength, checksum);

        var media = new MediaContent(mediaMeta, buffer.AsMemory());
        elements.Add(new RasterImageContent(++sequence, widthDots, height, media));
    }

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
