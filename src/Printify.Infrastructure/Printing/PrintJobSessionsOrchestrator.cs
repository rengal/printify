using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Coordinates live print job sessions per printer channel.
/// </summary>
public sealed class PrintJobSessionsOrchestrator(
    IPrintJobSessionFactory printJobSessionFactory,
    IServiceScopeFactory scopeFactory,
    IPrinterDocumentStream documentStream)
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
            await documentRepository.AddAsync(finalizedDocument, ct).ConfigureAwait(false);
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
        var sourceElements = document.Elements ?? [];
        var changed = false;
        var resultElements = new List<Element>(sourceElements.Count);

        foreach (var sourceElement in sourceElements)
        {
            if (sourceElement is RasterImageUpload rasterUpload)
            {
                var media = await mediaStorage.SaveAsync(rasterUpload.Media, ct).ConfigureAwait(false);
                resultElements.Add(new RasterImage(rasterUpload.Width, rasterUpload.Height, media));
                changed = true;
            }
            else
            {
                resultElements.Add(sourceElement);
            }
        }

        return changed
            ? document with { Elements = resultElements.AsReadOnly() }
            : document;
    }
}
