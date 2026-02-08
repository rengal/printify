using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Documents;
using Printify.Domain.Media;
using Printify.Domain.Printing;
using Printify.Domain.Printers;
using Printify.Domain.Services;
using Printify.Infrastructure.Cryptography;

namespace Printify.Infrastructure.Printing.Finalization;

public sealed class EplDocumentFinalizer(
    IMediaStorage mediaStorage,
    IDocumentRepository documentRepository,
    IPrinterRepository printerRepository)
    : IProtocolDocumentFinalizer
{
    public Protocol Protocol => Protocol.Epl;

    public async Task<Document> FinalizeAsync(Document document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        ct.ThrowIfCancellationRequested();

        var sourceCommands = document.Commands;
        var hasUploadCommands = sourceCommands.Any(c =>
            c is Epl.Commands.EplRasterImageUpload
            or Epl.Commands.EplPrintBarcodeUpload);

        if (!hasUploadCommands)
        {
            return document;
        }

        var resultCommands = new List<Command>(sourceCommands.Count);
        var printer = await printerRepository.GetByIdAsync(document.PrinterId, ct).ConfigureAwait(false);

        if (printer == null)
        {
            // Protocol conversion requires workspace context for media persistence.
            return document;
        }

        foreach (var sourceCommand in sourceCommands)
        {
            if (sourceCommand is Epl.Commands.EplRasterImageUpload or Epl.Commands.EplPrintBarcodeUpload)
            {
                MediaUpload? mediaUpload = null;
                var x = 0;
                var y = 0;
                var width = 0;
                var height = 0;
                var rotation = 0;
                var barcodeType = string.Empty;
                var hri = 'N';
                var barcodeData = string.Empty;

                switch (sourceCommand)
                {
                    case Epl.Commands.EplRasterImageUpload rasterImageUpload:
                        mediaUpload = rasterImageUpload.MediaUpload;
                        x = rasterImageUpload.X;
                        y = rasterImageUpload.Y;
                        width = rasterImageUpload.Width;
                        height = rasterImageUpload.Height;
                        break;
                    case Epl.Commands.EplPrintBarcodeUpload barcodeUpload:
                        mediaUpload = barcodeUpload.MediaUpload;
                        x = barcodeUpload.X;
                        y = barcodeUpload.Y;
                        rotation = barcodeUpload.Rotation;
                        barcodeType = barcodeUpload.Type;
                        width = barcodeUpload.Width;
                        height = barcodeUpload.Height;
                        hri = barcodeUpload.Hri;
                        barcodeData = barcodeUpload.Data;
                        break;
                }

                if (mediaUpload == null)
                {
                    continue;
                }

                var sha256Checksum = Sha256Checksum.ComputeLowerHex(mediaUpload.Content.Span);
                var savedMedia = await documentRepository
                    .GetMediaByChecksumAsync(sha256Checksum, printer.OwnerWorkspaceId, ct)
                    .ConfigureAwait(false);

                if (savedMedia == null)
                {
                    // Content-addressed storage guarantees deterministic media deduplication.
                    savedMedia = await mediaStorage.SaveAsync(mediaUpload, printer.OwnerWorkspaceId, sha256Checksum, ct)
                        .ConfigureAwait(false);
                    await documentRepository.AddMediaAsync(savedMedia, ct).ConfigureAwait(false);
                }

                switch (sourceCommand)
                {
                    case Epl.Commands.EplRasterImageUpload:
                        resultCommands.Add(new Epl.Commands.EplRasterImage(x, y, width, height, savedMedia)
                        {
                            RawBytes = sourceCommand.RawBytes,
                            LengthInBytes = sourceCommand.LengthInBytes
                        });
                        break;
                    case Epl.Commands.EplPrintBarcodeUpload:
                        resultCommands.Add(new Epl.Commands.EplPrintBarcode(
                            x,
                            y,
                            rotation,
                            barcodeType,
                            width,
                            height,
                            hri,
                            barcodeData,
                            savedMedia)
                        {
                            RawBytes = sourceCommand.RawBytes,
                            LengthInBytes = sourceCommand.LengthInBytes
                        });
                        break;
                }

                continue;
            }

            resultCommands.Add(sourceCommand);
        }

        return document with { Commands = resultCommands.AsReadOnly() };
    }
}
