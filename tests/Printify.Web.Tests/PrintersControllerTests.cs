using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;

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
}
