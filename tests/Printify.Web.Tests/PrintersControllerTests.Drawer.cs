using System.Net.Http.Json;
using System.Text.Json;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;
using Printify.Web.Contracts.Workspaces.Responses;
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

        // Connect to SSE stream
        var sseCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var streamRequest = new HttpRequestMessage(HttpMethod.Get, "/api/printers/sidebar/stream?realtime=full");
        using var response = await client.SendAsync(streamRequest, HttpCompletionOption.ResponseHeadersRead, sseCts.Token);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync(sseCts.Token);
        using var reader = new StreamReader(stream);

        // Get the listener to simulate client connection
        if (!TestPrinterListenerFactory.TryGetListener(printerId, out var listener))
        {
            throw new InvalidOperationException("Listener was not registered for the created printer.");
        }

        // 3. send escp/pos command to open drawer 1
        var channel1 = await listener.AcceptClientAsync(CancellationToken.None);
        // ESC p m t1 t2. m=0 for pin 2 (Drawer 1). t1/t2 are pulse durations.
        // 0x1B 0x70 0x00 0x32 0x32
        var openDrawer1Command = new byte[] { 0x1B, 0x70, 0x00, 0x32, 0x32 };
        await channel1.SendToServerAsync(openDrawer1Command, CancellationToken.None);
        await channel1.CloseAsync(ChannelClosedReason.Completed);

        // 4. Request status and make sure drawer 1 open, drawer 2 closed
        
        // Loop SSE events until we see the state we expect
        while (true)
        {
            var snapshot = await ReadSidebarEventAsync(reader, printerId, "Started", sseCts.Token);
            if (snapshot.RuntimeStatus?.Drawer1State == DrawerState.OpenedByCommand.ToString())
            {
                Assert.Equal(DrawerState.Closed.ToString(), snapshot.RuntimeStatus.Drawer2State);
                break;
            }
        }
        
        printerResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.Equal(DrawerState.OpenedByCommand.ToString(), printerResponse!.RuntimeStatus!.Drawer1State);
        Assert.Equal(DrawerState.Closed.ToString(), printerResponse.RuntimeStatus.Drawer2State);

        // 5. send esc/pos command to open drawer 2
        var channel2 = await listener.AcceptClientAsync(CancellationToken.None);
        // ESC p m t1 t2. m=1 for pin 5 (Drawer 2).
        // 0x1B 0x70 0x01 0x32 0x32
        var openDrawer2Command = new byte[] { 0x1B, 0x70, 0x01, 0x32, 0x32 };
        await channel2.SendToServerAsync(openDrawer2Command, CancellationToken.None);
        await channel2.CloseAsync(ChannelClosedReason.Completed);

        // 6. Request status amd make sure drawer 1 and drawer 2 are both open
        while (true)
        {
            var snapshot = await ReadSidebarEventAsync(reader, printerId, "Started", sseCts.Token);
            if (snapshot.RuntimeStatus?.Drawer2State == DrawerState.OpenedByCommand.ToString())
            {
                Assert.Equal(DrawerState.OpenedByCommand.ToString(), snapshot.RuntimeStatus.Drawer1State);
                break;
            }
        }

        printerResponse = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
        Assert.Equal(DrawerState.OpenedByCommand.ToString(), printerResponse!.RuntimeStatus!.Drawer1State);
        Assert.Equal(DrawerState.OpenedByCommand.ToString(), printerResponse.RuntimeStatus.Drawer2State);
    }

    private async Task WaitForDrawerStateAsync(HttpClient client, Guid printerId, DrawerState expectedState,
        int drawerNum)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await client.GetFromJsonAsync<PrinterResponseDto>($"/api/printers/{printerId}");
            var actualStateString = drawerNum == 1
                ? response?.RuntimeStatus?.Drawer1State
                : response?.RuntimeStatus?.Drawer2State;

            if (actualStateString == expectedState.ToString())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Drawer {drawerNum} did not reach {expectedState} within timeout");
    }
}
