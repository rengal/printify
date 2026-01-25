using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;
using PrinterRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterDto;
using PrinterSettingsRequestDto = Printify.Web.Contracts.Printers.Requests.PrinterSettingsDto;

namespace Printify.Web.Tests;

public sealed partial class PrintersControllerTests
{
    [Fact]
    public async Task Drawer_Opening_Works()
    {
        // 1. Create workspace and printer
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Drawer Printer"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));

        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // 2. Request status and make sure drawer 1 and drawer 2 are closed
        var printerResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.NotNull(printerResponse);
        Assert.NotNull(printerResponse.RuntimeStatus);

        // Verify default state is Closed
        Assert.Equal(DrawerState.Closed.ToString(), printerResponse.RuntimeStatus.Drawer1State);
        Assert.Equal(DrawerState.Closed.ToString(), printerResponse.RuntimeStatus.Drawer2State);

        // Get the listener first
        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId);

        // Start listening to runtime/stream before sending commands
        var listenTask = ListenForRuntimeEventsAsync(
            client,
            printerId,
            expectedCount: 2,
            timeout: TimeSpan.FromSeconds(10));

        // Accept a connection to start the printer session (puts it in "Started" state)
        var warmupChannel = await listener.AcceptClientAsync(CancellationToken.None);
        await warmupChannel.SendToServerAsync(Array.Empty<byte>(), CancellationToken.None);
        await warmupChannel.CloseAsync(ChannelClosedReason.Completed);

        // 4. Send escp/pos command to open drawer 1
        var channel1 = await listener.AcceptClientAsync(CancellationToken.None);
        // ESC p m t1 t2. m=0 for pin 2 (Drawer 1). t1/t2 are pulse durations.
        // 0x1B 0x70 0x00 0x32 0x32
        var openDrawer1Command = new byte[] { 0x1B, 0x70, 0x00, 0x32, 0x32 };
        await channel1.SendToServerAsync(openDrawer1Command, CancellationToken.None);
        await channel1.CloseAsync(ChannelClosedReason.Completed);

        // 5. Send esc/pos command to open drawer 2
        var channel2 = await listener.AcceptClientAsync(CancellationToken.None);
        // ESC p m t1 t2. m=1 for pin 5 (Drawer 2).
        // 0x1B 0x70 0x01 0x32 0x32
        var openDrawer2Command = new byte[] { 0x1B, 0x70, 0x01, 0x32, 0x32 };
        await channel2.SendToServerAsync(openDrawer2Command, CancellationToken.None);
        await channel2.CloseAsync(ChannelClosedReason.Completed);

        // 6. Wait for both drawer state updates via SSE stream
        var updates = await listenTask;

        Assert.Equal(2, updates.Count);

        // First update should have drawer 1 opened (drawer 2 unchanged/null)
        var firstUpdate = updates[0];
        Assert.Equal(DrawerState.OpenedByCommand.ToString(), firstUpdate.Runtime?.Drawer1State);
        // Drawer2State may be null in partial updates
        var firstDrawer2State = firstUpdate.Runtime?.Drawer2State ?? DrawerState.Closed.ToString();

        // Second update should have drawer 2 opened (drawer 1 unchanged/null)
        var secondUpdate = updates[1];
        Assert.Equal(DrawerState.OpenedByCommand.ToString(), secondUpdate.Runtime?.Drawer2State);
        // Drawer1State may be null in partial updates
        var secondDrawer1State = secondUpdate.Runtime?.Drawer1State ?? firstUpdate.Runtime?.Drawer1State!;

        // 7. Verify final state via API
        printerResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.Equal(DrawerState.OpenedByCommand.ToString(), printerResponse!.RuntimeStatus!.Drawer1State);
        Assert.Equal(DrawerState.OpenedByCommand.ToString(), printerResponse.RuntimeStatus.Drawer2State);
    }

    private static async Task<List<PrinterStatusUpdateDto>> ListenForRuntimeEventsAsync(
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
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        while (!cts.IsCancellationRequested && events.Count < expectedCount)
        {
            var line = await reader.ReadLineAsync().WaitAsync(cts.Token);
            if (line is null)
            {
                break;
            }

            Console.WriteLine($"[SSE] {line}");

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
                    var update = JsonSerializer.Deserialize<PrinterStatusUpdateDto>(currentData, options);
                    if (update?.PrinterId == printerId && update.Runtime != null)
                    {
                        events.Add(update);
                    }
                }
                currentEvent = null;
                currentData = null;
            }
        }

        return events;
    }
}
