using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Documents;
using Printify.Domain.Printing;
using Printify.Domain.Printers;
using Printify.Domain.Services;
using Printify.Infrastructure.Cryptography;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Printing.Finalization;

public sealed class EscPosDocumentFinalizer(
    IMediaStorage mediaStorage,
    IDocumentRepository documentRepository,
    IPrinterRepository printerRepository,
    IEscPosBarcodeService barcodeService)
    : IProtocolDocumentFinalizer
{
    public Protocol Protocol => Protocol.EscPos;

    public async Task<Document> FinalizeAsync(Document document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        ct.ThrowIfCancellationRequested();

        var sourceCommands = document.Commands;
        var hasUploadCommands = sourceCommands.Any(c =>
            c is EscPos.Commands.RasterImageUpload
            or EscPos.Commands.PrintBarcodeUpload
            or EscPos.Commands.PrintQrCodeUpload);

        if (!hasUploadCommands)
        {
            return document;
        }

        var resultCommands = new List<Command>(sourceCommands.Count);
        var printer = await printerRepository.GetByIdAsync(document.PrinterId, ct).ConfigureAwait(false);
        var settings = printer is null
            ? null
            : await printerRepository.GetSettingsAsync(document.PrinterId, ct).ConfigureAwait(false);
        var barcodeState = new BarcodeState();
        var qrState = new QrState();
        EscPos.Commands.TextJustification? justification = null;

        foreach (var sourceCommand in sourceCommands)
        {
            if (sourceCommand is EscPos.Commands.RasterImageUpload
                or EscPos.Commands.PrintQrCodeUpload
                or EscPos.Commands.PrintBarcodeUpload)
            {
                // Media rendering depends on printer settings; skip conversion if settings are missing.
                if (printer == null || settings == null)
                {
                    continue;
                }

                EscPos.Commands.RasterImageUpload? imageUpload = null;

                switch (sourceCommand)
                {
                    case EscPos.Commands.RasterImageUpload rasterImageUpload:
                        imageUpload = rasterImageUpload;
                        break;
                    case EscPos.Commands.PrintQrCodeUpload when string.IsNullOrEmpty(qrState.Payload):
                        continue;
                    case EscPos.Commands.PrintQrCodeUpload:
                        imageUpload = barcodeService.GenerateQrMedia(new QrRenderOptions(
                            qrState.Payload,
                            qrState.Model,
                            qrState.ModuleSizeInDots,
                            qrState.ErrorCorrectionLevel,
                            justification,
                            settings.WidthInDots));
                        break;
                    case EscPos.Commands.PrintBarcodeUpload barcodeUpload when string.IsNullOrEmpty(barcodeUpload.Data):
                        continue;
                    case EscPos.Commands.PrintBarcodeUpload barcodeUpload:
                        imageUpload = barcodeService.GenerateBarcodeMedia(
                            barcodeUpload,
                            new BarcodeRenderOptions(
                                barcodeState.HeightInDots,
                                barcodeState.ModuleWidthInDots,
                                barcodeState.LabelPosition,
                                justification,
                                settings.WidthInDots));
                        break;
                }

                if (imageUpload == null)
                {
                    continue;
                }

                var sha256Checksum = Sha256Checksum.ComputeLowerHex(imageUpload.Media.Content.Span);
                var savedMedia = await documentRepository
                    .GetMediaByChecksumAsync(sha256Checksum, printer.OwnerWorkspaceId, ct)
                    .ConfigureAwait(false);

                if (savedMedia == null)
                {
                    // Content-addressed storage guarantees deterministic media deduplication.
                    savedMedia = await mediaStorage.SaveAsync(imageUpload.Media, printer.OwnerWorkspaceId, sha256Checksum, ct)
                        .ConfigureAwait(false);
                    await documentRepository.AddMediaAsync(savedMedia, ct).ConfigureAwait(false);
                }

                switch (sourceCommand)
                {
                    case EscPos.Commands.RasterImageUpload:
                        resultCommands.Add(new EscPos.Commands.RasterImage(imageUpload.Width, imageUpload.Height, savedMedia)
                        {
                            RawBytes = sourceCommand.RawBytes,
                            LengthInBytes = sourceCommand.LengthInBytes
                        });
                        break;
                    case EscPos.Commands.PrintQrCodeUpload:
                        if (!string.IsNullOrEmpty(qrState.Payload))
                        {
                            resultCommands.Add(new EscPos.Commands.PrintQrCode(
                                qrState.Payload,
                                imageUpload.Width,
                                imageUpload.Height,
                                savedMedia)
                            {
                                RawBytes = sourceCommand.RawBytes,
                                LengthInBytes = sourceCommand.LengthInBytes
                            });
                        }
                        break;
                    case EscPos.Commands.PrintBarcodeUpload barcodeUpload:
                        resultCommands.Add(new EscPos.Commands.PrintBarcode(
                            barcodeUpload.Symbology,
                            barcodeUpload.Data,
                            imageUpload.Width,
                            imageUpload.Height,
                            savedMedia)
                        {
                            RawBytes = sourceCommand.RawBytes,
                            LengthInBytes = sourceCommand.LengthInBytes
                        });
                        break;
                }

                continue;
            }

            if (sourceCommand is EscPos.Commands.SetBarcodeHeight barcodeHeight)
            {
                barcodeState = barcodeState with { HeightInDots = barcodeHeight.HeightInDots };
            }
            else if (sourceCommand is EscPos.Commands.SetBarcodeModuleWidth moduleWidth)
            {
                barcodeState = barcodeState with { ModuleWidthInDots = moduleWidth.ModuleWidth };
            }
            else if (sourceCommand is EscPos.Commands.SetBarcodeLabelPosition labelPosition)
            {
                barcodeState = barcodeState with { LabelPosition = labelPosition.Position };
            }
            else if (sourceCommand is EscPos.Commands.SetQrModel qrModel)
            {
                qrState = qrState with { Model = qrModel.Model };
            }
            else if (sourceCommand is EscPos.Commands.SetQrModuleSize qrModuleSize)
            {
                qrState = qrState with { ModuleSizeInDots = qrModuleSize.ModuleSize };
            }
            else if (sourceCommand is EscPos.Commands.SetQrErrorCorrection qrErrorCorrection)
            {
                qrState = qrState with { ErrorCorrectionLevel = qrErrorCorrection.Level };
            }
            else if (sourceCommand is EscPos.Commands.StoreQrData qrData)
            {
                qrState = qrState with { Payload = qrData.Content };
            }
            else if (sourceCommand is EscPos.Commands.SetJustification justificationElement)
            {
                justification = justificationElement.Justification;
            }

            resultCommands.Add(sourceCommand);
        }

        return document with { Commands = resultCommands.AsReadOnly() };
    }

    private sealed record BarcodeState(
        int? HeightInDots = null,
        int? ModuleWidthInDots = null,
        EscPos.Commands.BarcodeLabelPosition? LabelPosition = null);

    private sealed record QrState(
        string? Payload = null,
        EscPos.Commands.QrModel Model = EscPos.Commands.QrModel.Model2,
        int? ModuleSizeInDots = null,
        EscPos.Commands.QrErrorCorrectionLevel? ErrorCorrectionLevel = null);
}
