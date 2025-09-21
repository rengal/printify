using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Printify.Contracts;
using Printify.Contracts.Elements;
using Printify.Contracts.Service;

namespace Printify.Tokenizer;

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

    private readonly IClock clock;
    private readonly IBlobStorage blobStorage;
    private readonly List<Element> elements = new List<Element>();
    private readonly List<byte> textBytes = new List<byte>();
    private int? activeTextLineIndex;
    private int sequence;
    private long totalConsumed;
    private bool isCompleted;
    private Document? document;
    private string currentCodePage = "437";
    private Encoding currentEncoding = Encoding.GetEncoding(437);

    public EscPosTokenizerSession(TokenizerSessionOptions options, IClock clock, IBlobStorage blobStorage)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        this.clock = clock;
        this.blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
    }

    public int Sequence
    {
        get { return sequence; }
    }

    public long TotalConsumed
    {
        get { return totalConsumed; }
    }

    public IReadOnlyList<Element> Elements
    {
        get { return elements; }
    }

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

    public bool IsCompleted
    {
        get { return isCompleted; }
    }

    public void Feed(ReadOnlySpan<byte> data)
    {
        if (isCompleted)
        {
            throw new InvalidOperationException("Cannot feed a completed tokenizer session.");
        }

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

            if (value == EscPosTokenizer.Lf)
            {
                // Command: Line Feed - output current line and start a new one.
                // ASCII: LF
                // HEX: 0A
                FlushText(allowEmpty: true);
                continue;
            }

            if (value == EscPosTokenizer.Esc)
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
                        elements.Add(new PageCut(++sequence));
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
                        var pin = PulsePinMap.TryGetValue(pinId, out var resolvedPin) ? resolvedPin : PulsePin.Drawer1;
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

            if (value == EscPosTokenizer.Gs)
            {
                if (index + 1 < data.Length)
                {
                    var command = data[index + 1];

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
                            StoreRasterImage(payload, widthBytes, height, mode);
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
                        elements.Add(new PageCut(++sequence));
                        continue;
                    }
                }

                continue;
            }

            if (value == Fs)
            {
                if (index + 1 < data.Length && data[index + 1] == FsSelectChinese)
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

                continue;
            }

            if (value == EscPosTokenizer.Bell)
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

        CommitPendingText();
        FlushText(allowEmpty: false);

        var snapshot = elements.ToArray();

        document = new Document(0, DateTimeOffset.UtcNow, Protocol.EscPos, null, snapshot);
        isCompleted = true;
    }

    private void AppendPrintable(byte value)
    {
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

    private void StoreRasterImage(byte[] payload, int widthBytes, int height, byte mode)
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

        using var uploadStream = new MemoryStream(buffer, writable: false);
        var metadata = new BlobMetadata("image/png", buffer.LongLength, checksum);
        var blobId = blobStorage.PutAsync(uploadStream, metadata).GetAwaiter().GetResult();

        elements.Add(new RasterImage(++sequence, widthDots, height, mode, blobId, metadata.ContentType, metadata.ContentLength, metadata.Checksum));
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

            currentCodePage = codePage;
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



