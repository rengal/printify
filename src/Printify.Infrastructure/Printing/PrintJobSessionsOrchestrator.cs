using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;
using Printify.Infrastructure.Cryptography;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Coordinates live print job sessions per printer channel.
/// </summary>
public sealed class PrintJobSessionsOrchestrator(
    IPrintJobSessionFactory printJobSessionFactory,
    IServiceScopeFactory scopeFactory,
    IPrinterDocumentStream documentStream,
    IPrinterStatusStream statusStream)
    : IPrintJobSessionsOrchestrator
{
    private readonly ConcurrentDictionary<IPrinterChannel, IPrintJobSession> jobSessions = new();

    public async Task<IPrintJobSession> StartSessionAsync(IPrinterChannel channel, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ct.ThrowIfCancellationRequested();

        var printer = channel.Printer;

        var printJob = new PrintJob(Guid.NewGuid(), printer, DateTimeOffset.Now, channel.ClientAddress);
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var printJobRepository = scope.ServiceProvider.GetRequiredService<IPrintJobRepository>();
            await printJobRepository.AddAsync(printJob, ct).ConfigureAwait(false);
        }

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

        var document = session.Document;
        if (document is not null)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var finalizedDocument = await FinalizeDocumentAsync(document, scope.ServiceProvider, ct).ConfigureAwait(false);
            var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var printerRepository = scope.ServiceProvider.GetRequiredService<IPrinterRepository>();
            await documentRepository.AddAsync(finalizedDocument, ct).ConfigureAwait(false);
            await printerRepository.SetLastDocumentReceivedAtAsync(finalizedDocument.PrinterId, finalizedDocument.CreatedAt, ct)
                .ConfigureAwait(false);
            // Update cash drawers status, if needed
            var drawerUpdate = await TryUpdateDrawerStateFromElementsAsync(
                channel.Printer,
                finalizedDocument.Elements,
                printerRepository,
                ct).ConfigureAwait(false);
            if (drawerUpdate is not null)
            {
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

        var mediaStorage = services.GetRequiredService<IMediaStorage>();
        var documentRepository = services.GetRequiredService<IDocumentRepository>();
        var printerRepository = services.GetRequiredService<IPrinterRepository>();
        var mediaService = services.GetRequiredService<IMediaService>();
        var sourceElements = document.Elements;
        var changed = false;
        var resultElements = new List<Element>(sourceElements.Count);

        var printer = await printerRepository.GetByIdAsync(document.PrinterId, ct).ConfigureAwait(false);
        var barcodeState = new BarcodeState();
        var qrState = new QrState();
        TextJustification? justification = null;

        foreach (var sourceElement in sourceElements)
        {
            if (sourceElement is RasterImageUpload or PrintQrCodeUpload or PrintBarcodeUpload)
            {
                if (printer == null)
                    continue;

                //Domain.Media.MediaUpload media = null;
                RasterImageUpload? imageUpload = null;

                switch (sourceElement)
                {
                    case RasterImageUpload rasterImageUpload:
                        imageUpload = rasterImageUpload;
                        break;
                    case PrintQrCodeUpload when string.IsNullOrEmpty(qrState.Payload):
                        continue;
                    case PrintQrCodeUpload:
                        imageUpload = mediaService.GenerateQrMedia(new QrRenderOptions(
                            qrState.Payload,
                            qrState.Model,
                            qrState.ModuleSizeInDots,
                            qrState.ErrorCorrectionLevel,
                            justification,
                            printer.WidthInDots));
                        break;
                    case PrintBarcodeUpload barcodeUpload when string.IsNullOrEmpty(barcodeUpload.Data):
                        continue;
                    case PrintBarcodeUpload barcodeUpload:
                        imageUpload = mediaService.GenerateBarcodeMedia(
                            barcodeUpload,
                            new BarcodeRenderOptions(
                                barcodeState.HeightInDots,
                                barcodeState.ModuleWidthInDots,
                                barcodeState.LabelPosition,
                                justification,
                                printer.WidthInDots));
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
                    case RasterImageUpload:
                        // Preserve the original command bytes for downstream diagnostics.
                        resultElements.Add(new RasterImage(imageUpload.Width, imageUpload.Height, savedMedia)
                        {
                            CommandRaw = sourceElement.CommandRaw,
                            LengthInBytes = sourceElement.LengthInBytes
                        });
                        break;
                    case PrintQrCodeUpload:
                        if (!string.IsNullOrEmpty(qrState.Payload))
                        {
                            // Preserve the original command bytes for downstream diagnostics.
                            resultElements.Add(new PrintQrCode(qrState.Payload, imageUpload.Width, imageUpload.Height,
                                savedMedia)
                            {
                                CommandRaw = sourceElement.CommandRaw,
                                LengthInBytes = sourceElement.LengthInBytes
                            });
                        }
                        break;
                    case PrintBarcodeUpload barcodeUpload:
                        // Preserve the original command bytes for downstream diagnostics.
                        resultElements.Add(new PrintBarcode(barcodeUpload.Symbology, barcodeUpload.Data,
                            imageUpload.Width, imageUpload.Height,
                            savedMedia)
                        {
                            CommandRaw = sourceElement.CommandRaw,
                            LengthInBytes = sourceElement.LengthInBytes
                        });
                        break;
                }

                changed = true;
                continue;
            }

            if (sourceElement is SetBarcodeHeight barcodeHeight)
                barcodeState = barcodeState with { HeightInDots = barcodeHeight.HeightInDots };
            else if (sourceElement is SetBarcodeModuleWidth moduleWidth)
                barcodeState = barcodeState with { ModuleWidthInDots = moduleWidth.ModuleWidth };
            else if (sourceElement is SetBarcodeLabelPosition labelPosition)
                barcodeState = barcodeState with { LabelPosition = labelPosition.Position };
            else if (sourceElement is SetQrModel qrModel)
                qrState = qrState with { Model = qrModel.Model };
            else if (sourceElement is SetQrModuleSize qrModuleSize)
                qrState = qrState with { ModuleSizeInDots = qrModuleSize.ModuleSize };
            else if (sourceElement is SetQrErrorCorrection qrErrorCorrection)
                qrState = qrState with { ErrorCorrectionLevel = qrErrorCorrection.Level };
            else if (sourceElement is StoreQrData qrData)
                qrState = qrState with { Payload = qrData.Content };
            else if (sourceElement is SetJustification justificationElement)
                justification = justificationElement.Justification;

            resultElements.Add(sourceElement);
        }

        return changed
            ? document with { Elements = resultElements.AsReadOnly() }
            : document;
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
    /// Updates drawer state based on ESC/POS pulse elements and returns the new snapshot for publishing.
    /// </summary>
    private static async Task<PrinterRealtimeStatusUpdate?> TryUpdateDrawerStateFromElementsAsync(
        Printer printer,
        IReadOnlyCollection<Element> elements,
        IPrinterRepository printerRepository,
        CancellationToken ct)
    {
        var (drawer1, drawer2) = GetDrawerStateUpdates(elements);
        if (drawer1 is null && drawer2 is null)
        {
            return null;
        }

        var existing = await printerRepository.GetRealtimeStatusAsync(printer.Id, ct).ConfigureAwait(false);
        // Default to Started so drawer updates align with the default target state when no snapshot exists.
        var targetState = existing?.TargetState ?? PrinterTargetState.Started;
        var nextDrawer1 = drawer1 ?? existing?.Drawer1State ?? DrawerState.Closed;
        var nextDrawer2 = drawer2 ?? existing?.Drawer2State ?? DrawerState.Closed;
        if (nextDrawer1 == existing?.Drawer1State && nextDrawer2 == existing?.Drawer2State)
        {
            return null;
        }

        var updatedAt = DateTimeOffset.UtcNow;
        // Only persist target state when there is no existing snapshot.
        var update = new PrinterRealtimeStatusUpdate(
            printer.Id,
            updatedAt,
            TargetState: existing is null ? targetState : null,
            State: PrinterState.Started,
            Drawer1State: nextDrawer1,
            Drawer2State: nextDrawer2);
        await printerRepository.UpsertRealtimeStatusAsync(update, ct).ConfigureAwait(false);
        return update;
    }

    private static (DrawerState? Drawer1State, DrawerState? Drawer2State) GetDrawerStateUpdates(
        IReadOnlyCollection<Element> elements)
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
