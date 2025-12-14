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

        var created = await Task.WhenAll(createTasks);
        var ports = created
            .Where(p => p != null)
            .Select(p => p!.TcpListenPort)
            .ToList();

        Assert.Equal(ports.Count, ports.Distinct().Count());
        Assert.True(ports.All(p => p > 0));

        var statusEvents = await statusTask;
        Assert.True(statusEvents.Count >= n);
        var distinctStatusPrinters = statusEvents.Select(e => e.PrinterId).Distinct().Count();
        Assert.True(distinctStatusPrinters >= n);
    }

    private static async Task<PrinterResponseDto?> CreatePrinterAsync(HttpClient client, CreatePrinterRequestDto request)
    {
        var response = await client.PostAsJsonAsync("/api/printers", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PrinterResponseDto>();
    }

    private static async Task<List<StatusEvent>> ListenForStatusEventsAsync(HttpClient client, int expectedCount, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var events = new List<StatusEvent>();

        using var response = await client.GetAsync("/api/printers/status/stream", HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        string? currentEvent = null;
        string? currentData = null;

        while (!cts.IsCancellationRequested && events.Count < expectedCount)
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
                    var ev = JsonSerializer.Deserialize<StatusEvent>(currentData, new JsonSerializerOptions
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
        }

        return events;
    }

    private sealed class StatusEvent
    {
        [JsonPropertyName("printerId")]
        public Guid PrinterId { get; init; }

        [JsonPropertyName("targetState")]
        public string? TargetState { get; init; }

        [JsonPropertyName("runtimeStatus")]
        public string? RuntimeStatus { get; init; }
    }
}
