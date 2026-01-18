using System.Net.Http.Json;
using System.Linq;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
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
    [Fact]
    public async Task Stats_LastDocumentTimestamp_IgnoresClearedDocuments()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Stats Printer"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId);
        var channel = await listener.AcceptClientAsync(CancellationToken.None);

        await channel.SendToServerAsync("hello"u8.ToArray(), CancellationToken.None);
        await channel.CloseAsync(ChannelClosedReason.Completed);

        var document = await WaitForDocumentAsync(client, printerId, CancellationToken.None);

        var statsResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.NotNull(statsResponse);
        Assert.NotNull(statsResponse!.Printer.LastDocumentReceivedAt);
        var delta = (statsResponse.Printer.LastDocumentReceivedAt.Value - document.Timestamp).Duration();
        Assert.True(delta <= TimeSpan.FromMilliseconds(1), $"Expected timestamps within 1ms but got {delta}.");

        var clearResponse = await client.DeleteAsync($"/api/printers/{printerId}/documents");
        clearResponse.EnsureSuccessStatusCode();

        var clearedResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.NotNull(clearedResponse);
        Assert.Null(clearedResponse!.Printer.LastDocumentReceivedAt);
    }

    private static async Task<CanvasDocumentDto> WaitForDocumentAsync(
        HttpClient client,
        Guid printerId,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/printers/{printerId}/documents/canvas?limit=1", ct);
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<CanvasDocumentListResponseDto>(cancellationToken: ct);
                var document = list?.Result.Items.FirstOrDefault();
                if (document is not null)
                {
                    return document;
                }
            }

            await Task.Delay(100, ct);
        }

        throw new TimeoutException($"Document for printer {printerId} was not persisted within timeout.");
    }
}
