using System.Net.Http.Json;
using Printify.TestServices;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;

namespace Printify.Web.Tests;

public sealed partial class PrintersControllerTests
{
    [Fact]
    public async Task StartStopPrinters_StatusEventsAndApiReflectState()
    {
        const int n = 10;

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        // Step 1: Subscribe to status stream before creating printers to capture starting/started events.
        var startStatusTask = ListenForStatusEventsAsync(client, expectedCount: n * 2, timeout: TimeSpan.FromSeconds(15), breakOnDistinct: false);

        // Step 2: Create printers (server auto-starts listeners)
        var printerIds = new List<Guid>(n);
        for (var i = 0; i < n; i++)
        {
            var printerId = Guid.NewGuid();
            printerIds.Add(printerId);
            var request = new CreatePrinterRequestDto(printerId, $"Loop-{i}", "EscPos", 512, null, false, null, null);
            var response = await client.PostAsJsonAsync("/api/printers", request);
            response.EnsureSuccessStatusCode();
        }

        // Step 3: Wait for starting/started events
        var startEvents = await startStatusTask;
        var startingCount = startEvents.Count(e => string.Equals(e.RuntimeStatus, "starting", StringComparison.OrdinalIgnoreCase));
        var startedCount = startEvents.Count(e => string.Equals(e.RuntimeStatus, "started", StringComparison.OrdinalIgnoreCase));
        var distinctStarted = startEvents
            .Where(e => string.Equals(e.RuntimeStatus, "started", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.PrinterId)
            .Distinct()
            .ToHashSet();

        Assert.True(startingCount >= n, "Expected at least one 'starting' event per printer.");
        Assert.True(startedCount >= n, "Expected at least one 'started' event per printer.");
        Assert.True(printerIds.All(distinctStarted.Contains), "Not all printers reported started.");

        // Step 4: Verify via API that all are started
        var listResponse = await client.GetFromJsonAsync<List<PrinterResponseDto>>("/api/printers");
        Assert.NotNull(listResponse);
        foreach (var printer in listResponse!)
        {
            Assert.Equal("started", printer.RuntimeStatus.ToLowerInvariant());
        }

        // Step 5: Stop all printers and wait for stopped events
        var stopStatusTask = ListenForStatusEventsAsync(client, expectedCount: n, timeout: TimeSpan.FromSeconds(10), breakOnDistinct: true);
        foreach (var printerId in printerIds)
        {
            var stopResponse = await client.PostAsJsonAsync($"/api/printers/{printerId}/status", new SetPrinterStatusRequestDto { TargetStatus = "Stopped" });
            stopResponse.EnsureSuccessStatusCode();
        }

        var stopEvents = await stopStatusTask;
        var stoppedIds = stopEvents
            .Where(e => string.Equals(e.RuntimeStatus, "stopped", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.PrinterId)
            .Distinct()
            .ToHashSet();
        Assert.True(printerIds.All(stoppedIds.Contains), "Not all printers reported stopped.");

        // Step 6: Verify via API that all are stopped
        var listAfterStop = await client.GetFromJsonAsync<List<PrinterResponseDto>>("/api/printers");
        Assert.NotNull(listAfterStop);
        foreach (var printer in listAfterStop!)
        {
            Assert.Equal("stopped", printer.RuntimeStatus.ToLowerInvariant());
        }
    }

}
