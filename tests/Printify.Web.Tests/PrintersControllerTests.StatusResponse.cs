using System.Net.Http.Json;
using System.Text;
using Printify.Application.Printing.Events;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Printers.Requests;

namespace Printify.Web.Tests;

public sealed partial class PrintersControllerTests
{
    [Fact]
    public async Task SendStatusRequest_PrinterRespondsWithStatusByte()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        // Create printer
        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "Status Test Printer", "EscPos", 512, null, false, null, null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Get the test listener
        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId);

        // Accept client connection
        var channel = await listener.AcceptClientAsync(CancellationToken.None);

        // Set up response capture
        var responseReceived = new TaskCompletionSource<byte[]>();
        channel.OnResponse(data =>
        {
            responseReceived.TrySetResult(data.ToArray());
        });

        // Send ESC/POS status request: DLE EOT n (0x10 0x04 0x02)
        // 0x10 = DLE (Data Link Escape)
        // 0x04 = EOT (End of Transmission)
        // 0x02 = Request type (printer status)
        var statusRequest = new byte[] { 0x10, 0x04, 0x02 };
        await channel.SendToServerAsync(statusRequest, CancellationToken.None);

        // Wait for response
        var responseTask = responseReceived.Task;
        var completedTask = await Task.WhenAny(responseTask, Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.True(completedTask == responseTask, "Expected status response within timeout");

        var response = await responseTask;
        Assert.NotNull(response);
        Assert.Single(response);

        // Verify status byte (0x12 = ready status)
        // Bit pattern: 0001 0010
        // Bit 1 = 1 (fixed)
        // Bit 4 = 1 (fixed)
        // All error bits = 0 (ready)
        Assert.Equal(0x12, response[0]);

        // Clean up
        await channel.CloseAsync(ChannelClosedReason.Completed);
    }

    [Theory]
    [InlineData(0x01, 0x12)] // PrinterStatus -> 0x12 (ready)
    [InlineData(0x02, 0x12)] // OfflineCause -> 0x12 (no offline condition)
    [InlineData(0x03, 0x02)] // ErrorCause -> 0x02 (no error, bit1 fixed)
    [InlineData(0x04, 0x12)] // PaperRollSensor -> 0x12 (paper present)
    public async Task SendStatusRequest_DifferentRequestTypes_AllRespondWithReadyStatus(byte requestType, byte expectedStatusByte)
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, $"Status Test {requestType:X2}", "EscPos", 512, null, false, null, null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId);
        var channel = await listener.AcceptClientAsync(CancellationToken.None);

        var responseReceived = new TaskCompletionSource<byte[]>();
        channel.OnResponse(data =>
        {
            responseReceived.TrySetResult(data.ToArray());
        });

        // Send status request with different request type
        var statusRequest = new byte[] { 0x10, 0x04, requestType };
        await channel.SendToServerAsync(statusRequest, CancellationToken.None);

        var responseTask = responseReceived.Task;
        var completedTask = await Task.WhenAny(responseTask, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.True(completedTask == responseTask, $"Expected status response for request type 0x{requestType:X2} within timeout");

        var response = await responseTask;
        Assert.NotNull(response);
        Assert.Single(response);
        Assert.Equal(expectedStatusByte, response[0]);

        await channel.CloseAsync(ChannelClosedReason.Completed);
    }

    [Fact]
    public async Task SendMultipleStatusRequests_AllReceiveResponses()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "Multi Status Test", "EscPos", 512, null, false, null, null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId);
        var channel = await listener.AcceptClientAsync(CancellationToken.None);

        var responsesReceived = new List<byte[]>();
        var responseCount = 0;
        var allResponsesReceived = new TaskCompletionSource<bool>();
        const int expectedResponses = 5;

        channel.OnResponse(data =>
        {
            responsesReceived.Add(data.ToArray());
            responseCount++;
            if (responseCount >= expectedResponses)
            {
                allResponsesReceived.TrySetResult(true);
            }
        });

        // Send 5 status requests
        for (int i = 0; i < expectedResponses; i++)
        {
            var statusRequest = new byte[] { 0x10, 0x04, 0x02 };
            await channel.SendToServerAsync(statusRequest, CancellationToken.None);
            await Task.Delay(10); // Small delay between requests
        }

        var completedTask = await Task.WhenAny(allResponsesReceived.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.True(completedTask == allResponsesReceived.Task, "Expected all status responses within timeout");

        Assert.Equal(expectedResponses, responsesReceived.Count);
        foreach (var response in responsesReceived)
        {
            Assert.Single(response);
            Assert.Equal(0x12, response[0]);
        }

        await channel.CloseAsync(ChannelClosedReason.Completed);
    }
}
