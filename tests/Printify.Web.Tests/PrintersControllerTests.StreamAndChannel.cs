using System.Net.Http.Json;
using Printify.Domain.Printers;
using Printify.TestServices;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;
using PrinterRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterDto;
using PrinterSettingsRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterSettingsDto;

namespace Printify.Web.Tests;

public sealed partial class PrintersControllerTests
{
    [Fact]
    public async Task StartStopPrinters_StatusEventsAndApiReflectState()
    {
        const int n = 10;

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        using var patchClient = environment.CreateClient();
        await AuthHelper.CreateWorkspaceAndLogin(environment);
        patchClient.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;

        // Step 1: Create printers (server auto-starts listeners)
        var printerIds = new List<Guid>(n);
        for (var i = 0; i < n; i++)
        {
            var printerId = Guid.NewGuid();
            printerIds.Add(printerId);
            var request = new CreatePrinterRequestDto(
                new PrinterRequestDto(printerId, $"Loop-{i}"),
                new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
            var response = await client.PostAsJsonAsync("/api/printers", request);
            response.EnsureSuccessStatusCode();
        }

        // Step 2: Wait for all printers to reach Started via API
        foreach (var printerId in printerIds)
            await WaitForPrinterStateAsync(client, printerId, PrinterState.Started, CancellationToken.None);

        // Step 3: Verify via API that all are started
        var listResponse = await client.GetFromJsonAsync<List<PrinterResponseDto>>("/api/printers");
        Assert.NotNull(listResponse);
        foreach (var printer in listResponse!)
            Assert.Equal("started", printer.RuntimeStatus?.State?.ToLowerInvariant());

        // Step 4: Open SSE stream and wait for connected signal
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var sseResponse = await client.GetAsync("/api/printers/sidebar/stream", HttpCompletionOption.ResponseHeadersRead, cts.Token);
        sseResponse.EnsureSuccessStatusCode();
        await using var sseStream = await sseResponse.Content.ReadAsStreamAsync(cts.Token);
        using var sseReader = new StreamReader(sseStream);

        string? connectedLine;
        do { connectedLine = await sseReader.ReadLineAsync(cts.Token); }
        while (connectedLine != null && !connectedLine.StartsWith(':'));

        // Step 5: Stop all printers and collect stopped events via SSE
        var listenTask = CollectSidebarEventsAsync(sseReader, expectedCount: n, cts.Token);

        foreach (var printerId in printerIds)
        {
            var stopResponse = await patchClient.PatchAsJsonAsync(
                $"/api/printers/{printerId}/operational-flags",
                new UpdatePrinterOperationalFlagsRequestDto(null, null, null, null, null, TargetState: "Stopped"));
            stopResponse.EnsureSuccessStatusCode();
        }

        var stopEvents = await listenTask;
        var stoppedIds = stopEvents
            .Where(e => string.Equals(e.RuntimeStatus?.State, "Stopped", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Printer.Id)
            .Distinct()
            .ToHashSet();
        Assert.True(printerIds.All(stoppedIds.Contains), "Not all printers reported stopped.");

        // Step 6: Verify via API that all are stopped
        var listAfterStop = await client.GetFromJsonAsync<List<PrinterResponseDto>>("/api/printers");
        Assert.NotNull(listAfterStop);
        foreach (var printer in listAfterStop!)
            Assert.Equal("stopped", printer.RuntimeStatus?.State?.ToLowerInvariant());
    }
}
