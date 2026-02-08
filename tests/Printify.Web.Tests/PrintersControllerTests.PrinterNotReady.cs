using System.Net.Http.Json;
using System.Text;
using Printify.Application.Printing.Events;
using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Infrastructure.Printing.EscPos.Commands;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Printers.Requests;

namespace Printify.Web.Tests;

public sealed partial class PrintersControllerTests
{
    [Fact]
    public async Task EscPos_PrintableCommands_WithCoverOpen_EmitSinglePrinterNotReadyErrorPerDocument()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "EscPos Not Ready", "EscPos", 512, null, false, null, null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var flagsResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/operational-flags",
            new UpdatePrinterOperationalFlagsRequestDto(
                IsCoverOpen: true,
                IsPaperOut: null,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                TargetState: null));
        flagsResponse.EnsureSuccessStatusCode();

        await using var stream = environment.DocumentStream.Subscribe(printerId, CancellationToken.None).GetAsyncEnumerator();
        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId);
        var channel = await listener.AcceptClientAsync(CancellationToken.None);

        // Two text segments separated by an ESC/POS command to exercise multiple printable chunks.
        var payload = new byte[]
        {
            (byte)'a', (byte)'b', (byte)'c',
            0x1B, 0x40, // ESC @ (initialize)
            (byte)'d', (byte)'e', (byte)'f'
        };
        await channel.SendToServerAsync(payload, CancellationToken.None);
        await channel.CloseAsync(ChannelClosedReason.Completed);

        var hasEvent = await stream.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(hasEvent);

        var document = stream.Current.Document;
        var notReadyErrors = document.Commands
            .OfType<EscPosPrinterError>()
            .Where(error => error.Message?.StartsWith("Printer not ready:", StringComparison.Ordinal) == true)
            .ToList();

        Assert.Single(notReadyErrors);
        Assert.Contains("cover open", notReadyErrors[0].Message, StringComparison.Ordinal);
        Assert.Equal(0, notReadyErrors[0].LengthInBytes);
    }

    [Fact]
    public async Task Epl_PrintableCommands_WithPaperOut_EmitSinglePrinterNotReadyErrorPerDocument()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "Epl Not Ready", "Epl", 512, 400, false, null, null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var flagsResponse = await client.PatchAsJsonAsync(
            $"/api/printers/{printerId}/operational-flags",
            new UpdatePrinterOperationalFlagsRequestDto(
                IsCoverOpen: null,
                IsPaperOut: true,
                IsOffline: null,
                HasError: null,
                IsPaperNearEnd: null,
                TargetState: null));
        flagsResponse.EnsureSuccessStatusCode();

        await using var stream = environment.DocumentStream.Subscribe(printerId, CancellationToken.None).GetAsyncEnumerator();
        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId);
        var channel = await listener.AcceptClientAsync(CancellationToken.None);

        var payload = Encoding.ASCII.GetBytes("A10,20,0,2,1,1,N,\"HELLO\"\nP1\n");
        await channel.SendToServerAsync(payload, CancellationToken.None);
        await channel.CloseAsync(ChannelClosedReason.Completed);

        var hasEvent = await stream.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(hasEvent);

        var document = stream.Current.Document;
        var notReadyErrors = document.Commands
            .OfType<EplPrinterError>()
            .Where(error => error.Message?.StartsWith("Printer not ready:", StringComparison.Ordinal) == true)
            .ToList();

        Assert.Single(notReadyErrors);
        Assert.Contains("paper out", notReadyErrors[0].Message, StringComparison.Ordinal);
        Assert.Equal(0, notReadyErrors[0].LengthInBytes);
    }
}
