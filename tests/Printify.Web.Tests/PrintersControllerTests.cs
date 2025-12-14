using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;

namespace Printify.Web.Tests;

public sealed class PrintersControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task UpdatePrinter_WithValidRequest_UpdatesPrinter()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "Receipt Printer", "EscPos", 512, null, false, null, null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var updateBody = new UpdatePrinterRequestDto("Updated Printer", "EscPos", 576, null, true, 1024m, 4096);
        var updateResponse = await client.PutAsJsonAsync($"/api/printers/{printerId}", updateBody);
        updateResponse.EnsureSuccessStatusCode();

        var updatedPrinter = await updateResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(updatedPrinter);
        Assert.Equal("Updated Printer", updatedPrinter.DisplayName);
        Assert.Equal(576, updatedPrinter.WidthInDots);
        Assert.True(updatedPrinter.TcpListenPort > 0);
        Assert.True(updatedPrinter.EmulateBufferCapacity);
        Assert.Equal(1024m, updatedPrinter.BufferDrainRate);
        Assert.Equal(4096, updatedPrinter.BufferMaxCapacity);
        Assert.False(updatedPrinter.IsPinned);

        var fetchResponse = await client.GetAsync($"/api/printers/{printerId}");
        fetchResponse.EnsureSuccessStatusCode();
        var fetchedPrinter = await fetchResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(fetchedPrinter);
        Assert.Equal("Updated Printer", fetchedPrinter.DisplayName);
        Assert.True(fetchedPrinter.TcpListenPort > 0);
        Assert.True(fetchedPrinter.EmulateBufferCapacity);
        Assert.Equal(1024m, fetchedPrinter.BufferDrainRate);
        Assert.Equal(4096, fetchedPrinter.BufferMaxCapacity);
        Assert.False(fetchedPrinter.IsPinned);
    }

    [Fact]
    public async Task PinPrinter_TogglesPinnedState()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "Pin Printer", "EscPos", 512, null, true, 2048m, 8192);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var pinResponse = await client.PostAsJsonAsync($"/api/printers/{printerId}/pin", new PinPrinterRequestDto(true));
        pinResponse.EnsureSuccessStatusCode();
        var pinnedPrinter = await pinResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(pinnedPrinter);
        Assert.True(pinnedPrinter.IsPinned);
        Assert.True(pinnedPrinter.TcpListenPort > 0);
        Assert.True(pinnedPrinter.EmulateBufferCapacity);
        Assert.Equal(2048m, pinnedPrinter.BufferDrainRate);
        Assert.Equal(8192, pinnedPrinter.BufferMaxCapacity);

        var fetchResponse = await client.GetAsync($"/api/printers/{printerId}");
        fetchResponse.EnsureSuccessStatusCode();
        var fetchedPrinter = await fetchResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(fetchedPrinter);
        Assert.True(fetchedPrinter.IsPinned);
        Assert.True(fetchedPrinter.TcpListenPort > 0);
        Assert.True(fetchedPrinter.EmulateBufferCapacity);
        Assert.Equal(2048m, fetchedPrinter.BufferDrainRate);
        Assert.Equal(8192, fetchedPrinter.BufferMaxCapacity);

        var unpinResponse = await client.PostAsJsonAsync($"/api/printers/{printerId}/pin", new PinPrinterRequestDto(false));
        unpinResponse.EnsureSuccessStatusCode();
        var unpinnedPrinter = await unpinResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(unpinnedPrinter);
        Assert.False(unpinnedPrinter.IsPinned);
        Assert.True(unpinnedPrinter.TcpListenPort > 0);
        Assert.True(unpinnedPrinter.EmulateBufferCapacity);
        Assert.Equal(2048m, unpinnedPrinter.BufferDrainRate);
        Assert.Equal(8192, unpinnedPrinter.BufferMaxCapacity);
    }

    [Fact]
    public async Task DeletePrinter_RemovesPrinter()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "Temp Printer", "EscPos", 512, null, false, null, null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync($"/api/printers/{printerId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var fetchResponse = await client.GetAsync($"/api/printers/{printerId}");
        Assert.Equal(HttpStatusCode.NotFound, fetchResponse.StatusCode);
    }

    [Fact]
    public async Task DeletePrinter_WithDifferentUser_ReturnsNotFound()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "Shared Printer", "EscPos", 512, null, true, 1024, 4096);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var deleteResponse = await client.DeleteAsync($"/api/printers/{printerId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreatePrinter_RegistersInMemoryListenerChannel()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "Listener Printer", "EscPos", 384, null, false, null, null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        if (!TestPrinterListenerFactory.TryGetListener(printerId, out var listener))
        {
            throw new InvalidOperationException($"Listener for printer {printerId} was not registered.");
        }

        var channel = await listener.AcceptClientAsync();

        var payloadReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        channel.DataReceived += (_, args) =>
        {
            payloadReceived.TrySetResult(args.Buffer.ToArray());
            return ValueTask.CompletedTask;
        };

        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        await channel.WriteAsync(payload, CancellationToken.None);

        var observedPayload = await payloadReceived.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(payload.SequenceEqual(observedPayload));
        Assert.False(channel.IsDisposed);
    }

    [Fact]
    public async Task CreateTwoPrinters_AssignsDifferentTcpPorts()
    {
        // Arrange: create workspace and authenticate
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        // Act: create first printer
        var firstId = Guid.NewGuid();
        var firstRequest = new CreatePrinterRequestDto(firstId, "Port Printer 1", "EscPos", 512, null, false, null, null);
        var firstResponse = await client.PostAsJsonAsync("/api/printers", firstRequest);
        firstResponse.EnsureSuccessStatusCode();
        var firstDto = await firstResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(firstDto);

        // Act: create second printer
        var secondId = Guid.NewGuid();
        var secondRequest = new CreatePrinterRequestDto(secondId, "Port Printer 2", "EscPos", 512, null, false, null, null);
        var secondResponse = await client.PostAsJsonAsync("/api/printers", secondRequest);
        secondResponse.EnsureSuccessStatusCode();
        var secondDto = await secondResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(secondDto);

        // Assert: ports are assigned by server and are different
        Assert.True(firstDto!.TcpListenPort > 0);
        Assert.True(secondDto!.TcpListenPort > 0);
        Assert.NotEqual(firstDto.TcpListenPort, secondDto.TcpListenPort);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task CreateNPrintersInParallel_AssignsUniquePorts(int n)
    {
        // Scenario: many concurrent clients create printers; server must assign unique ports.
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var statusTask = ListenForStatusEventsAsync(environment.Client, n, TimeSpan.FromSeconds(10));

        var createTasks = new List<Task<PrinterResponseDto?>>(n);

        for (var i = 0; i < n; i++)
        {
            var client = environment.CreateClient();
            client.DefaultRequestHeaders.Authorization = environment.Client.DefaultRequestHeaders.Authorization;

            var printerId = Guid.NewGuid();
            var request = new CreatePrinterRequestDto(printerId, $"Parallel-{i}", "EscPos", 512, null, false, null, null);
            createTasks.Add(CreatePrinterAsync(client, request));
        }

        try
        {
            var created = await Task.WhenAll(createTasks).WaitAsync(TimeSpan.FromSeconds(15));
            var ports = created
                .Where(p => p != null)
                .Select(p => p!.TcpListenPort)
                .ToList();

            Assert.Equal(ports.Count, ports.Distinct().Count());
            Assert.True(ports.All(p => p > 0));
        }
        catch (TimeoutException)
        {
            throw new TimeoutException("Timed out while creating printers in parallel.");
        }

        var statusEvents = await statusTask;
        var distinctStatusPrinters = statusEvents.Select(e => e.PrinterId).Distinct().Count();
        Assert.True(distinctStatusPrinters >= n, "Did not observe status events for all printers.");
    }

    private static async Task<PrinterResponseDto?> CreatePrinterAsync(HttpClient client, CreatePrinterRequestDto request)
    {
        var response = await client.PostAsJsonAsync("/api/printers", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PrinterResponseDto>();
    }

    private static async Task<List<PrinterStatusEventDto>> ListenForStatusEventsAsync(
        HttpClient client,
        int expectedCount,
        TimeSpan timeout,
        bool breakOnDistinct = true)
    {
        using var cts = new CancellationTokenSource(timeout);
        var events = new List<PrinterStatusEventDto>();

        using var response = await client.GetAsync("/api/printers/status/stream", HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        string? currentEvent = null;
        string? currentData = null;

        var start = DateTimeOffset.UtcNow;

        while (!cts.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line is null)
            {
                break;
            }

            if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
            {
                currentEvent = line[6..].Trim();
            }
            else if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                currentData = line[5..].Trim();
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                if (currentEvent == "status" && !string.IsNullOrEmpty(currentData))
                {
                    var ev = JsonSerializer.Deserialize<PrinterStatusEventDto>(currentData, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (ev != null)
                    {
                        events.Add(ev);
                    }
                }

                currentEvent = null;
                currentData = null;
            }

            var distinctCount = events.Select(e => e.PrinterId).Distinct().Count();
            if (breakOnDistinct && distinctCount >= expectedCount)
            {
                break;
            }

            if (!breakOnDistinct && events.Count >= expectedCount)
            {
                break;
            }
        }

        return events;
    }

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
