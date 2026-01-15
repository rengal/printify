using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;
using Printify.Tests.Shared.Document;
using Printify.Tests.Shared.Epl;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Documents.Responses.View;

namespace Printify.Web.Tests.Epl;

public class EplTests(WebApplicationFactory<Program> factory)
    : ProtocolTestsBase<EplScenario>(
        factory,
        Protocol.Epl,
        "epl")
{
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

        await using var streamEnumerator = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();
        var nextEventTask = streamEnumerator.MoveNextAsync().AsTask();

        var bytesSent = 0;
        channel.OnResponse(data => bytesSent += data.Length);

        // Send a simple EPL clear command
        await channel.SendToServerAsync("N\n"u8.ToArray(), CancellationToken.None);

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

        clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(stepMs));
        await nextEventTask.WaitAsync(TimeSpan.FromMilliseconds(500));

        Assert.True(nextEventTask.Result);
        var documentEvent = streamEnumerator.Current;
        DocumentAssertions.Equal(
            documentEvent.Document.Elements.ToList(),
            Protocol.Epl,
            documentEvent.Document,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(2, bytesSent, documentEvent.Document);

        // Verify view endpoint works
        var ct = CancellationToken.None;
        var viewResponse = await environment.Client.GetAsync($"/api/printers/{printerId}/documents/view?limit=10");
        viewResponse.EnsureSuccessStatusCode();
        var viewDocumentList = await viewResponse.Content.ReadFromJsonAsync<ViewDocumentListResponseDto>(cancellationToken: ct);
        Assert.NotNull(viewDocumentList);
        var viewDocuments = viewDocumentList.Result.Items;
        var documentId = documentEvent.Document.Id;
        var viewDocument = viewDocuments.FirstOrDefault(doc => doc.Id == documentId)
            ?? viewDocuments.FirstOrDefault();
        Assert.NotNull(viewDocument);

        DocumentAssertions.EqualView(
            [],
            Protocol.Epl,
            viewDocument,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(2, bytesSent, viewDocument);
    }
}
