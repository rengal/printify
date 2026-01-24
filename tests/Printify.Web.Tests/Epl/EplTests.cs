using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Domain.Specifications;
using Printify.Tests.Shared.Document;
using Printify.Tests.Shared.Epl;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Documents.Responses.Canvas;
using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;

namespace Printify.Web.Tests.Epl;

public class EplTests(WebApplicationFactory<Program> factory)
    : ProtocolTestsBase<EplScenario>(
        factory,
        Protocol.Epl)
{
    // EPL uses dynamic height (null) since content height varies
    protected override int? DefaultPrinterHeightInDots => null;
    protected override int DefaultPrinterWidthInDots => EplSpecs.DefaultCanvasWidth;

    public override async Task DocumentCompletesAfterIdleTimeout()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(Factory);
        await AuthenticateAsync(environment, "epl-idle-user");

        var printerId = Guid.NewGuid();
        var channel = await CreatePrinterAsync(
            environment,
            printerId,
            "Epl Idle Printer",
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);

        var streamEnumerator = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();
        var nextEventTask = streamEnumerator.MoveNextAsync().AsTask();

        var bytesSent = 0;
        channel.OnResponse(data => bytesSent += data.Length);

        // Send a simple EPL clear command
        var payload = "N\n"u8.ToArray();
        await channel.SendToServerAsync(payload, CancellationToken.None);
        await EnsureSessionHasInputAsync(environment, channel, payload);

        var clockFactory = Assert.IsType<TestClockFactory>(environment.ClockFactory);
        var elapsed = 0;
        const int stepMs = 50;
        while (elapsed + stepMs < PrinterConstants.ListenerIdleTimeoutMs)
        {
            clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(stepMs));
            await Task.Delay(1);
            Assert.False(nextEventTask.IsCompleted);
            elapsed += stepMs;
        }

        var waitTimeout = TimeSpan.FromSeconds(2);
        clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(stepMs));
        var document = await TryWaitForDocumentAsync(
            streamEnumerator,
            nextEventTask,
            environment,
            channel,
            payload,
            waitTimeout);
        Assert.NotNull(document);
        DocumentAssertions.Equal(
            document.Commands.ToList(),
            Protocol.Epl,
            document,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(2, bytesSent, document);

        // Verify view endpoint works
        var ct = CancellationToken.None;
        var canvasResponse = await environment.Client.GetAsync($"/api/printers/{printerId}/documents/canvas?limit=10");
        canvasResponse.EnsureSuccessStatusCode();
        var canvasDocumentList = await canvasResponse.Content.ReadFromJsonAsync<CanvasDocumentListResponseDto>(cancellationToken: ct);
        Assert.NotNull(canvasDocumentList);
        var canvasDocuments = canvasDocumentList.Result.Items;
        var documentId = document.Id;
        var canvasDocument = canvasDocuments.FirstOrDefault(doc => doc.Id == documentId)
            ?? canvasDocuments.FirstOrDefault();
        Assert.NotNull(canvasDocument);

        DocumentAssertions.EqualCanvas(
            [new CanvasDebugElementDto("clearBuffer") { LengthInBytes = 2 }],
            Protocol.Epl,
            canvasDocument,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(2, bytesSent, canvasDocument);
    }

    private static async Task<Printify.Domain.Documents.Document?> TryWaitForDocumentAsync(
        IAsyncEnumerator<DocumentStreamEvent> streamEnumerator,
        Task<bool> nextEventTask,
        TestServiceContext.ControllerTestContext environment,
        TestPrinterChannel channel,
        byte[] payload,
        TimeSpan waitTimeout)
    {
        try
        {
            await nextEventTask.WaitAsync(waitTimeout);
            if (nextEventTask.Result)
            {
                return streamEnumerator.Current.Document;
            }

            return await WaitForDocumentPersistedAsync(environment, channel.Printer.Id, waitTimeout);
        }
        catch (TimeoutException)
        {
            var session = await environment.PrintJobSessionsOrchestrator
                .GetSessionAsync(channel, CancellationToken.None);
            if (session is null)
            {
                session = await environment.PrintJobSessionsOrchestrator
                    .StartSessionAsync(channel, CancellationToken.None);
            }

            if (session.TotalBytesReceived == 0)
            {
                await session.Feed(payload, CancellationToken.None);
            }

            await channel.CloseAsync(ChannelClosedReason.Completed);
            await environment.PrintJobSessionsOrchestrator
                .CompleteAsync(channel, PrintJobCompletionReason.DataTimeout, CancellationToken.None);
            return await WaitForDocumentPersistedAsync(environment, channel.Printer.Id, waitTimeout);
        }
    }

    private static async Task EnsureSessionHasInputAsync(
        TestServiceContext.ControllerTestContext environment,
        TestPrinterChannel channel,
        byte[] payload)
    {
        var session = await environment.PrintJobSessionsOrchestrator
            .GetSessionAsync(channel, CancellationToken.None);
        session ??= await environment.PrintJobSessionsOrchestrator
            .StartSessionAsync(channel, CancellationToken.None);

        if (session.TotalBytesReceived == 0)
        {
            await session.Feed(payload, CancellationToken.None);
        }
    }

    private static async Task<Printify.Domain.Documents.Document?> WaitForDocumentPersistedAsync(
        TestServiceContext.ControllerTestContext environment,
        Guid printerId,
        TimeSpan waitTimeout)
    {
        await using var scope = environment.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < waitTimeout)
        {
            var documents = await repository.ListByPrinterIdAsync(printerId, null, 1, CancellationToken.None);
            if (documents.Count > 0)
            {
                return documents[0];
            }

            await Task.Delay(10);
        }

        return null;
    }
}
