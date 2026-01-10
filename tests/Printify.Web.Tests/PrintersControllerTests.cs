using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;
using PrinterRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterDto;
using PrinterSettingsRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterSettingsDto;

namespace Printify.Web.Tests;

public sealed partial class PrintersControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CreatePrinter_RegistersInMemoryListenerChannel()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Listener Printer"),
            new PrinterSettingsRequestDto("EscPos", 384, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId);

        var channel = await listener.AcceptClientAsync();

        var payloadReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        channel.DataReceived += (_, args) =>
        {
            payloadReceived.TrySetResult(args.Buffer.ToArray());
            return ValueTask.CompletedTask;
        };

        // Arbitrary byte pattern to prove the in-memory channel forwards raw data as-is.
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        await channel.SendToServerAsync(payload, CancellationToken.None);

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
        var firstRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(firstId, "Port Printer 1"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var firstResponse = await client.PostAsJsonAsync("/api/printers", firstRequest);
        firstResponse.EnsureSuccessStatusCode();
        var firstDto = await firstResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(firstDto);

        // Act: create second printer
        var secondId = Guid.NewGuid();
        var secondRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(secondId, "Port Printer 2"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var secondResponse = await client.PostAsJsonAsync("/api/printers", secondRequest);
        secondResponse.EnsureSuccessStatusCode();
        var secondDto = await secondResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(secondDto);

        // Assert: ports are assigned by server and are different
        Assert.True(firstDto!.Settings.TcpListenPort > 0);
        Assert.True(secondDto!.Settings.TcpListenPort > 0);
        Assert.NotEqual(firstDto.Settings.TcpListenPort, secondDto.Settings.TcpListenPort);
    }

    private static async Task<List<PrinterSidebarSnapshotDto>> ListenForStatusEventsAsync(
        HttpClient client,
        int expectedCount,
        TimeSpan timeout,
        bool breakOnDistinct = true,
        string? url = null)
    {
        const string defaultUrl = "/api/printers/sidebar/stream";
        using var cts = new CancellationTokenSource(timeout);
        var events = new List<PrinterSidebarSnapshotDto>();

        var requestUrl = string.IsNullOrWhiteSpace(url) ? defaultUrl : url;
        using var response = await client.GetAsync(
            requestUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        string? currentEvent = null;
        string? currentData = null;

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
                if (string.Equals(currentEvent, "sidebar", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(currentData))
                {
                    var ev = JsonSerializer.Deserialize<PrinterSidebarSnapshotDto>(currentData, new JsonSerializerOptions
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

            var distinctCount = events.Select(e => e.Printer.Id).Distinct().Count();
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

    private static async Task<List<PrinterStatusUpdateDto>> ListenForFullStatusEventsAsync(
        HttpClient client,
        Guid printerId,
        int expectedCount,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var events = new List<PrinterStatusUpdateDto>();

        var url = $"/api/printers/{printerId}/runtime/stream";
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        string? currentEvent = null;
        string? currentData = null;

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
                if (string.Equals(currentEvent, "status", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(currentData))
                {
                    var ev = JsonSerializer.Deserialize<PrinterStatusUpdateDto>(currentData, new JsonSerializerOptions
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

            if (events.Count >= expectedCount)
            {
                break;
            }
        }

        return events;
    }
}
