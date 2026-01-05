using System.Net.Http.Json;
using System.Text;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Documents.Responses;
using Printify.Web.Contracts.Documents.Responses.Elements;
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

    private static async Task<DocumentDto> SendDocumentAndVerifyAsync(
        HttpClient client,
        IAsyncEnumerator<DocumentStreamEvent> documentStream,
        Guid printerId,
        string text,
        int expectedWidthInDots,
        int? expectedHeightInDots,
        CancellationToken ct)
    {
        if (!TestPrinterListenerFactory.TryGetListener(printerId, out var listener))
        {
            throw new InvalidOperationException("Listener was not registered for the created printer.");
        }

        var channel = await listener.AcceptClientAsync(ct);
        var payload = Encoding.ASCII.GetBytes(text);
        await channel.SendToServerAsync(payload, ct);
        await channel.CloseAsync(ChannelClosedReason.Completed);

        var hasNext = await documentStream.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1), ct);
        Assert.True(hasNext, "Expected document event from stream");

        var document = documentStream.Current.Document;
        var response = await client.GetAsync($"/api/printers/{printerId}/documents/{document.Id}", ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<DocumentDto>(cancellationToken: ct);
        Assert.NotNull(dto);
        Assert.Equal(expectedWidthInDots, dto!.WidthInDots);
        Assert.Equal(expectedHeightInDots, dto.HeightInDots);
        var textLine = dto!.Elements.OfType<AppendToLineBufferDto>().FirstOrDefault();
        Assert.NotNull(textLine);
        Assert.Equal(text, textLine!.Text);
        return dto!;
    }
}
