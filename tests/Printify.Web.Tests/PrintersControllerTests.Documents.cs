using System.Net.Http.Json;
using System.Text;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Documents.Responses.Canvas;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;
using PrinterRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterDto;
using PrinterSettingsRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterSettingsDto;

namespace Printify.Web.Tests;

public sealed partial class PrintersControllerTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task StartStopPrinter_DocumentsFlowAcrossRestarts(int iterations)
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Lifecycle Printer"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        await using var documentStream = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();

        await SendDocumentAndVerifyAsync(
            client,
            documentStream,
            printerId,
            "test document 1",
            createRequest.Settings.WidthInDots,
            createRequest.Settings.HeightInDots,
            CancellationToken.None);

        for (var i = 1; i <= iterations; i++)
        {
            var stopResponse = await client.PatchAsJsonAsync(
                $"/api/printers/{printerId}/operational-flags",
                new UpdatePrinterOperationalFlagsRequestDto(
                    IsCoverOpen: null,
                    IsPaperOut: null,
                    IsOffline: null,
                    HasError: null,
                    IsPaperNearEnd: null,
                    TargetState: "Stopped"));
            stopResponse.EnsureSuccessStatusCode();

            await WaitForPrinterStateAsync(client, printerId, PrinterState.Stopped, CancellationToken.None);

            var startResponse = await client.PatchAsJsonAsync(
                $"/api/printers/{printerId}/operational-flags",
                new UpdatePrinterOperationalFlagsRequestDto(
                    IsCoverOpen: null,
                    IsPaperOut: null,
                    IsOffline: null,
                    HasError: null,
                    IsPaperNearEnd: null,
                    TargetState: "Started"));
            startResponse.EnsureSuccessStatusCode();

            await WaitForPrinterStateAsync(client, printerId, PrinterState.Started, CancellationToken.None);

            var text = $"test document {i + 1}";
            await SendDocumentAndVerifyAsync(
                client,
                documentStream,
                printerId,
                text,
                createRequest.Settings.WidthInDots,
                createRequest.Settings.HeightInDots,
                CancellationToken.None);
        }
    }

    [Fact]
    public async Task DocumentStream_CapturesWidthPerPrint()
    {
        /*
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Stream Width Printer"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        await using var documentStream = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();

        var firstDocument = await SendDocumentAndReadStreamAsync(
            documentStream,
            printerId,
            "first print",
            CancellationToken.None);
        Assert.Equal(512m, firstDocument.WidthInDots);

        var updateRequest = new UpdatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Stream Width Printer"),
            new PrinterSettingsRequestDto("EscPos", 576, null, false, null, null));
        var updateResponse = await client.PutAsJsonAsync($"/api/printers/{printerId}", updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        var secondDocument = await SendDocumentAndReadStreamAsync(
            documentStream,
            printerId,
            "second print",
            CancellationToken.None);
        Assert.Equal(576m, secondDocument.WidthInDots);

        // Stream should contain two documents with captured widths.
        Assert.All(
            new[] { firstDocument, secondDocument },
            doc => Assert.True(doc.WidthInDots > 0));

        var canvasResponse = await client.GetAsync($"/api/printers/{printerId}/documents/canvas?limit=2");
        canvasResponse.EnsureSuccessStatusCode();
        var canvasList = await canvasResponse.Content.ReadFromJsonAsync<CanvasDocumentListResponseDto>();
        Assert.NotNull(canvasList);

        var firstCanvas = canvasList.Result.Items.FirstOrDefault(doc => doc.Id == firstDocument.Id);
        var secondCanvas = canvasList.Result.Items.FirstOrDefault(doc => doc.Id == secondDocument.Id);
        Assert.NotNull(firstCanvas);
        Assert.NotNull(secondCanvas);
        Assert.Equal(512, firstCanvas!.Canvases[0].WidthInDots);
        Assert.Equal(576, secondCanvas!.Canvases[0].WidthInDots);
        */
    }

    private static async Task WaitForPrinterStateAsync(
        HttpClient client,
        Guid printerId,
        PrinterState expectedStatus,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/printers/{printerId}", ct);
            if (response.IsSuccessStatusCode)
            {
                var printer = await response.Content.ReadFromJsonAsync<PrinterResponseDto>(cancellationToken: ct);
                if (printer?.RuntimeStatus?.State == expectedStatus.ToString())
                {
                    return;
                }
            }

            await Task.Delay(100, ct);
        }

        throw new TimeoutException($"Printer {printerId} did not reach State={expectedStatus} within timeout");
    }

    private static async Task SendDocumentAndVerifyAsync(
        HttpClient client,
        IAsyncEnumerator<DocumentStreamEvent> documentStream,
        Guid printerId,
        string text,
        int expectedWidthInDots,
        int? expectedHeightInDots,
        CancellationToken ct)
    {
        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId, cancellationToken: ct);
        var channel = await listener.AcceptClientAsync(ct);
        var payload = Encoding.ASCII.GetBytes(text);
        await channel.SendToServerAsync(payload, ct);
        await channel.CloseAsync(ChannelClosedReason.Completed);

        var hasNext = await documentStream.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1), ct);
        Assert.True(hasNext, "Expected document event from stream");

        var document = documentStream.Current.Document;

        // Verify view endpoint returns the document
        var response = await client.GetAsync($"/api/printers/{printerId}/documents/canvas?limit=10", ct);
        response.EnsureSuccessStatusCode();

        var viewDocumentList = await response.Content.ReadFromJsonAsync<CanvasDocumentListResponseDto>(
            cancellationToken: ct);
        Assert.NotNull(viewDocumentList);

        var viewDocument = viewDocumentList.Result.Items.FirstOrDefault(doc => doc.Id == document.Id)
            ?? viewDocumentList.Result.Items.FirstOrDefault();
        Assert.NotNull(viewDocument);

        Assert.Equal(expectedWidthInDots, viewDocument.Canvases[0].WidthInDots);
        Assert.Equal(expectedHeightInDots, viewDocument.Canvases[0].HeightInDots);
    }

    private static async Task<Document> SendDocumentAndReadStreamAsync(
        IAsyncEnumerator<DocumentStreamEvent> documentStream,
        Guid printerId,
        string text,
        CancellationToken ct)
    {
        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId, cancellationToken: ct);
        var channel = await listener.AcceptClientAsync(ct);
        var payload = Encoding.ASCII.GetBytes(text);
        await channel.SendToServerAsync(payload, ct);
        await channel.CloseAsync(ChannelClosedReason.Completed);

        var hasNext = await documentStream.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1), ct);
        Assert.True(hasNext, "Expected document event from stream");

        return documentStream.Current.Document;
    }
}
