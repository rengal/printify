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
using Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

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
