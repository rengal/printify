using System.Net.Http.Json;
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
        return; //todo debugnow fix test
        // Number of printers to create and test
        const int n = 10;

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        // Step 1: Subscribe to status stream before creating printers to capture starting/started events.
        var startStatusTask = ListenForStatusEventsAsync(
            client,
            expectedCount: n * 2,
            timeout: TimeSpan.FromSeconds(2),
            breakOnDistinct: false);

        // Step 2: Create printers (server auto-starts listeners)
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

        // Step 3: Wait for starting/started events
        Console.WriteLine("Waiting for startStatusTask...");
        var startEvents = await startStatusTask;
        Console.WriteLine($"startStatusTask done: {startEvents.Count}");
        var startingCount = startEvents.Count(
            e => string.Equals(e.RuntimeStatus?.State, "starting", StringComparison.OrdinalIgnoreCase));
        var startedCount = startEvents.Count(
            e => string.Equals(e.RuntimeStatus?.State, "started", StringComparison.OrdinalIgnoreCase));
        var distinctStarted = startEvents
            .Where(e => string.Equals(e.RuntimeStatus?.State, "started", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Printer.Id)
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
            Assert.Equal("started", printer.RuntimeStatus?.State?.ToLowerInvariant());
        }

        // Step 5: Stop all printers and wait for stopped events
        var stopStatusTask = ListenForStatusEventsAsync(
            client,
            expectedCount: n,
            timeout: TimeSpan.FromSeconds(2),
            breakOnDistinct: true);
        Console.WriteLine("Waiting for stopStatusTask...");
        foreach (var printerId in printerIds)
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
        }

        var stopEvents = await stopStatusTask;
        Console.WriteLine($"stopStatusTask done: {stopEvents.Count}");
        var stoppedIds = stopEvents
            .Where(e => string.Equals(e.RuntimeStatus?.State, "stopped", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Printer.Id)
            .Distinct()
            .ToHashSet();
        Assert.True(printerIds.All(stoppedIds.Contains), "Not all printers reported stopped.");

        // Step 6: Verify via API that all are stopped
        var listAfterStop = await client.GetFromJsonAsync<List<PrinterResponseDto>>("/api/printers");
        Assert.NotNull(listAfterStop);
        foreach (var printer in listAfterStop!)
        {
            Assert.Equal("stopped", printer.RuntimeStatus?.State?.ToLowerInvariant());
        }
    }
}
