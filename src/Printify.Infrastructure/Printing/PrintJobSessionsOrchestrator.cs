using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Documents;
using Printify.Domain.Printing;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;
using Printify.Infrastructure.Cryptography;
using Printify.Infrastructure.Printing.EscPos;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Coordinates live print job sessions per printer channel.
/// </summary>
public sealed class PrintJobSessionsOrchestrator(
    IPrintJobSessionFactory printJobSessionFactory,
    IServiceScopeFactory scopeFactory,
    IPrinterDocumentStream documentStream,
    IPrinterBufferCoordinator bufferCoordinator,
    IPrinterStatusStream statusStream,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IPrintJobSessionsOrchestrator
{
    private readonly ConcurrentDictionary<IPrinterChannel, IPrintJobSession> jobSessions = new();

    public async Task<IPrintJobSession> StartSessionAsync(IPrinterChannel channel, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ct.ThrowIfCancellationRequested();

        var printer = channel.Printer;

        await using var scope = scopeFactory.CreateAsyncScope();
        var printerRepository = scope.ServiceProvider.GetRequiredService<IPrinterRepository>();
        var printerSettings = await printerRepository.GetSettingsAsync(printer.Id, ct)
            .ConfigureAwait(false) ?? channel.Settings;

        var printJob = new PrintJob(
            Guid.NewGuid(),
            printer,
            printerSettings,
            channel.Settings.Protocol,
            DateTimeOffset.Now,
            channel.ClientAddress);

        var printJobRepository = scope.ServiceProvider.GetRequiredService<IPrintJobRepository>();
        await printJobRepository.AddAsync(printJob, ct).ConfigureAwait(false);

        var jobSession = await printJobSessionFactory.Create(printJob, channel, ct);
        jobSessions[channel] = jobSession;
        return jobSession;
    }

    public Task<IPrintJobSession?> GetSessionAsync(IPrinterChannel channel, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ct.ThrowIfCancellationRequested();

        return jobSessions.TryGetValue(channel, out var session)
            ? Task.FromResult<IPrintJobSession?>(session)
            : Task.FromResult<IPrintJobSession?>(null);
    }

    public async Task FeedAsync(IPrinterChannel channel, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        if (!jobSessions.TryGetValue(channel, out var session) || data.Length == 0)
            return;

        ct.ThrowIfCancellationRequested();
        await session.Feed(data, ct);
    }

    public async Task CompleteAsync(IPrinterChannel channel, PrintJobCompletionReason reason, CancellationToken ct)
    {
        if (!jobSessions.TryRemove(channel, out var session))
            return;
        await session.Complete(reason).ConfigureAwait(false);
        // Emit a final buffer update so subscribers see the latest drained state.
        bufferCoordinator.ForcePublish(channel.Printer, channel.Settings);

        var document = session.Document;
        if (document is not null)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var finalizedDocument = await FinalizeDocumentAsync(document, scope.ServiceProvider, ct).ConfigureAwait(false);
            var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var printerRepository = scope.ServiceProvider.GetRequiredService<IPrinterRepository>();
            await documentRepository.AddAsync(finalizedDocument, ct).ConfigureAwait(false);
            await printerRepository.SetLastDocumentReceivedAtAsync(finalizedDocument.PrinterId, finalizedDocument.Timestamp, ct)
                .ConfigureAwait(false);
            // Update cash drawers status, if needed
            var drawerUpdate = await TryUpdateDrawerStateFromElementsAsync(
                channel.Printer,
                finalizedDocument.Commands,
                ct).ConfigureAwait(false);
            if (drawerUpdate is not null)
            {
                if (drawerUpdate.RuntimeUpdate is not null)
                {
                    runtimeStatusStore.Update(drawerUpdate.RuntimeUpdate);
                }
                statusStream.Publish(channel.Printer.OwnerWorkspaceId, drawerUpdate);
            }
            documentStream.Publish(new DocumentStreamEvent(finalizedDocument));
        }
    }


    /// <summary>
    /// Saves media to storage and document to repository. Update MediaUpload and RasterImageUpload objects into Media and Raster image
    /// </summary>
    private static async Task<Document> FinalizeDocumentAsync(Document document, IServiceProvider services,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(services);

        var sourceElements = document.Commands;
        var resultCommands = new List<Command>(sourceElements.Count);

        // Check if this document has any upload commands that need finalization
        var hasUploadCommands = sourceElements.Any(c =>
            c is EscPos.Commands.RasterImageUpload
            or EscPos.Commands.PrintBarcodeUpload
            or EscPos.Commands.PrintQrCodeUpload
            or Epl.Commands.EplRasterImageUpload
            or Epl.Commands.EplPrintBarcodeUpload);

        if (!hasUploadCommands)
        {
            // No finalization needed, return original document
            return document;
        }

        // Protocol-specific finalization
        if (document.Protocol == Protocol.EscPos)
        {
            return await FinalizeEscPosDocumentAsync(document, services, ct).ConfigureAwait(false);
        }
        else if (document.Protocol == Protocol.Epl)
        {
            return await FinalizeEplDocumentAsync(document, services, ct).ConfigureAwait(false);
        }

        // Unknown protocol, return original document
        return document;
    }

    /// <summary>
    /// Finalizes ESC/POS documents by converting upload commands to final commands with persisted media.
    /// </summary>
    private static async Task<Document> FinalizeEscPosDocumentAsync(Document document, IServiceProvider services,
        CancellationToken ct)
    {
        var mediaStorage = services.GetRequiredService<IMediaStorage>();
        var documentRepository = services.GetRequiredService<IDocumentRepository>();
        var printerRepository = services.GetRequiredService<IPrinterRepository>();
        var barcodeService = services.GetRequiredService<IEscPosBarcodeService>();
        var sourceElements = document.Commands;
        var resultCommands = new List<Command>(sourceElements.Count);

        var printer = await printerRepository.GetByIdAsync(document.PrinterId, ct).ConfigureAwait(false);
        var settings = printer is null
            ? null
            : await printerRepository.GetSettingsAsync(document.PrinterId, ct).ConfigureAwait(false);
        var barcodeState = new BarcodeState();
        var qrState = new QrState();
        TextJustification? justification = null;

        foreach (var sourceElement in sourceElements)
        {
            if (sourceElement is EscPos.Commands.RasterImageUpload or EscPos.Commands.PrintQrCodeUpload or EscPos.Commands.PrintBarcodeUpload)
            {
                // Media rendering depends on printer settings; skip conversion if settings are missing.
                if (printer == null || settings is null)
                    continue;

                EscPos.Commands.RasterImageUpload? imageUpload = null;

                switch (sourceElement)
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

                // Images are content-addressed (SHA-256), enabling safe deduplication without relying on file names or IDs.
                var sha256Checksum = Sha256Checksum.ComputeLowerHex(imageUpload!.Media.Content.Span);

                var savedMedia = await documentRepository
                                     .GetMediaByChecksumAsync(sha256Checksum, printer.OwnerWorkspaceId, ct)
                                     .ConfigureAwait(false);

                if (savedMedia == null)
                {
                    // Save file to disk
                    savedMedia = await mediaStorage.SaveAsync(imageUpload.Media, printer.OwnerWorkspaceId,
                            sha256Checksum, ct)
                        .ConfigureAwait(false);

                    // Save media entity to database
                    await documentRepository.AddMediaAsync(savedMedia, ct).ConfigureAwait(false);
                }

                switch (sourceElement)
                {
                    case EscPos.Commands.RasterImageUpload:
                        // Preserve the original command bytes for downstream diagnostics.
                        resultCommands.Add(new EscPos.Commands.RasterImage(imageUpload.Width, imageUpload.Height, savedMedia)
                        {
                            RawBytes = sourceElement.RawBytes,
                            LengthInBytes = sourceElement.LengthInBytes
                        });
                        break;
                    case EscPos.Commands.PrintQrCodeUpload:
                        if (!string.IsNullOrEmpty(qrState.Payload))
                        {
                            // Preserve the original command bytes for downstream diagnostics.
                            resultCommands.Add(new EscPos.Commands.PrintQrCode(qrState.Payload, imageUpload.Width, imageUpload.Height,
                                    savedMedia)
                                {
                                    RawBytes = sourceElement.RawBytes,
                                    LengthInBytes = sourceElement.LengthInBytes
                                });
                        }
                        break;
                    case EscPos.Commands.PrintBarcodeUpload barcodeUpload:
                        // Preserve the original command bytes for downstream diagnostics.
                        resultCommands.Add(new EscPos.Commands.PrintBarcode(barcodeUpload.Symbology, barcodeUpload.Data,
                            imageUpload.Width, imageUpload.Height,
                            savedMedia)
                        {
                            RawBytes = sourceElement.RawBytes,
                            LengthInBytes = sourceElement.LengthInBytes
                        });
                        break;
                }

                continue;
            }

            if (sourceElement is EscPos.Commands.SetBarcodeHeight barcodeHeight)
                barcodeState = barcodeState with { HeightInDots = barcodeHeight.HeightInDots };
            else if (sourceElement is EscPos.Commands.SetBarcodeModuleWidth moduleWidth)
                barcodeState = barcodeState with { ModuleWidthInDots = moduleWidth.ModuleWidth };
            else if (sourceElement is EscPos.Commands.SetBarcodeLabelPosition labelPosition)
                barcodeState = barcodeState with { LabelPosition = labelPosition.Position };
            else if (sourceElement is EscPos.Commands.SetQrModel qrModel)
                qrState = qrState with { Model = qrModel.Model };
            else if (sourceElement is EscPos.Commands.SetQrModuleSize qrModuleSize)
                qrState = qrState with { ModuleSizeInDots = qrModuleSize.ModuleSize };
            else if (sourceElement is EscPos.Commands.SetQrErrorCorrection qrErrorCorrection)
                qrState = qrState with { ErrorCorrectionLevel = qrErrorCorrection.Level };
            else if (sourceElement is EscPos.Commands.StoreQrData qrData)
                qrState = qrState with { Payload = qrData.Content };
            else if (sourceElement is EscPos.Commands.SetJustification justificationElement)
                justification = justificationElement.Justification;

            resultCommands.Add(sourceElement);
        }

        return document with { Commands = resultCommands.AsReadOnly() };
    }

    /// <summary>
    /// Finalizes EPL documents by converting upload commands to final commands with persisted media.
    /// </summary>
    private static async Task<Document> FinalizeEplDocumentAsync(Document document, IServiceProvider services,
        CancellationToken ct)
    {
        var mediaStorage = services.GetRequiredService<IMediaStorage>();
        var documentRepository = services.GetRequiredService<IDocumentRepository>();
        var printerRepository = services.GetRequiredService<IPrinterRepository>();
        var sourceElements = document.Commands;
        var resultCommands = new List<Command>(sourceElements.Count);

        var printer = await printerRepository.GetByIdAsync(document.PrinterId, ct).ConfigureAwait(false);

        if (printer == null)
        {
            // Can't finalize without printer info
            return document;
        }

        foreach (var sourceElement in sourceElements)
        {
            if (sourceElement is Epl.Commands.EplRasterImageUpload or Epl.Commands.EplPrintBarcodeUpload)
            {
                Domain.Media.MediaUpload? mediaUpload = null;
                int? x = null;
                int? y = null;
                int? width = null;
                int? height = null;
                int? rotation = null;
                string? barcodeType = null;
                char? hri = null;
                string? barcodeData = null;

                switch (sourceElement)
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
                    continue;

                // Images are content-addressed (SHA-256), enabling safe deduplication without relying on file names or IDs.
                var sha256Checksum = Sha256Checksum.ComputeLowerHex(mediaUpload.Content.Span);

                var savedMedia = await documentRepository
                                     .GetMediaByChecksumAsync(sha256Checksum, printer.OwnerWorkspaceId, ct)
                                     .ConfigureAwait(false);

                if (savedMedia == null)
                {
                    // Save file to disk
                    savedMedia = await mediaStorage.SaveAsync(mediaUpload, printer.OwnerWorkspaceId,
                            sha256Checksum, ct)
                        .ConfigureAwait(false);

                    // Save media entity to database
                    await documentRepository.AddMediaAsync(savedMedia, ct).ConfigureAwait(false);
                }

                switch (sourceElement)
                {
                    case Epl.Commands.EplRasterImageUpload:
                        // Preserve the original command bytes for downstream diagnostics.
                        resultCommands.Add(new Epl.Commands.EplRasterImage(
                            x ?? 0,
                            y ?? 0,
                            width ?? 0,
                            height ?? 0,
                            savedMedia)
                        {
                            RawBytes = sourceElement.RawBytes,
                            LengthInBytes = sourceElement.LengthInBytes
                        });
                        break;
                    case Epl.Commands.EplPrintBarcodeUpload:
                        // Preserve the original command bytes for downstream diagnostics.
                        resultCommands.Add(new Epl.Commands.EplPrintBarcode(
                            x ?? 0,
                            y ?? 0,
                            rotation ?? 0,
                            barcodeType ?? string.Empty,
                            width ?? 0,
                            height ?? 0,
                            hri ?? 'N',
                            barcodeData ?? string.Empty,
                            savedMedia)
                        {
                            RawBytes = sourceElement.RawBytes,
                            LengthInBytes = sourceElement.LengthInBytes
                        });
                        break;
                }

                continue;
            }

            resultCommands.Add(sourceElement);
        }

        return document with { Commands = resultCommands.AsReadOnly() };
    }

    private sealed record BarcodeState(
        int? HeightInDots = null,
        int? ModuleWidthInDots = null,
        BarcodeLabelPosition? LabelPosition = null);

    private sealed record QrState(
        string? Payload = null,
        QrModel Model = QrModel.Model2,
        int? ModuleSizeInDots = null,
        QrErrorCorrectionLevel? ErrorCorrectionLevel = null);

    /// <summary>
    /// Emits drawer state updates derived from ESC/POS pulse elements.
    /// </summary>
    private static Task<PrinterStatusUpdate?> TryUpdateDrawerStateFromElementsAsync(
        Printer printer,
        IReadOnlyCollection<Command> elements,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var (drawer1, drawer2) = GetDrawerStateUpdates(elements);
        if (drawer1 is null && drawer2 is null)
        {
            return Task.FromResult<PrinterStatusUpdate?>(null);
        }

        var updatedAt = DateTimeOffset.UtcNow;
        var runtimeUpdate = new PrinterRuntimeStatusUpdate(
            printer.Id,
            updatedAt,
            Drawer1State: drawer1,
            Drawer2State: drawer2);
        var update = new PrinterStatusUpdate(
            printer.Id,
            updatedAt,
            RuntimeUpdate: runtimeUpdate);

        return Task.FromResult<PrinterStatusUpdate?>(update);
    }

    private static (DrawerState? Drawer1State, DrawerState? Drawer2State) GetDrawerStateUpdates(
        IReadOnlyCollection<Command> elements)
    {
        DrawerState? drawer1 = null;
        DrawerState? drawer2 = null;

        foreach (var element in elements)
        {
            if (element is not Pulse pulse)
            {
                continue;
            }

            // ESC/POS pin 0/1 maps to drawer 1/2 for emulated cash drawer pulses.
            if (pulse.Pin == 0)
            {
                drawer1 = DrawerState.OpenedByCommand;
            }
            else if (pulse.Pin == 1)
            {
                drawer2 = DrawerState.OpenedByCommand;
            }
        }

        return (drawer1, drawer2);
    }
}
