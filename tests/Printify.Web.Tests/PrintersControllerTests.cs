using System.Net;
using System.Net.Http.Json;
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
        var createRequest = new CreatePrinterRequestDto(printerId, "Receipt Printer", "EscPos", 512, null, 9100, false, null, null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var updateBody = new UpdatePrinterRequestDto("Updated Printer", "EscPos", 576, null, 9101, true, 1024m, 4096);
        var updateResponse = await client.PutAsJsonAsync($"/api/printers/{printerId}", updateBody);
        updateResponse.EnsureSuccessStatusCode();

        var updatedPrinter = await updateResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(updatedPrinter);
        Assert.Equal("Updated Printer", updatedPrinter.DisplayName);
        Assert.Equal(576, updatedPrinter.WidthInDots);
        Assert.Equal(9101, updatedPrinter.TcpListenPort);
        Assert.True(updatedPrinter.EmulateBufferCapacity);
        Assert.Equal(1024m, updatedPrinter.BufferDrainRate);
        Assert.Equal(4096, updatedPrinter.BufferMaxCapacity);
        Assert.False(updatedPrinter.IsPinned);

        var fetchResponse = await client.GetAsync($"/api/printers/{printerId}");
        fetchResponse.EnsureSuccessStatusCode();
        var fetchedPrinter = await fetchResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(fetchedPrinter);
        Assert.Equal("Updated Printer", fetchedPrinter.DisplayName);
        Assert.Equal(9101, fetchedPrinter.TcpListenPort);
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
        var createRequest = new CreatePrinterRequestDto(printerId, "Pin Printer", "EscPos", 512, null, 9104, true, 2048m, 8192);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var pinResponse = await client.PostAsJsonAsync($"/api/printers/{printerId}/pin", new PinPrinterRequestDto(true));
        pinResponse.EnsureSuccessStatusCode();
        var pinnedPrinter = await pinResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(pinnedPrinter);
        Assert.True(pinnedPrinter.IsPinned);
        Assert.Equal(9104, pinnedPrinter.TcpListenPort);
        Assert.True(pinnedPrinter.EmulateBufferCapacity);
        Assert.Equal(2048m, pinnedPrinter.BufferDrainRate);
        Assert.Equal(8192, pinnedPrinter.BufferMaxCapacity);

        var fetchResponse = await client.GetAsync($"/api/printers/{printerId}");
        fetchResponse.EnsureSuccessStatusCode();
        var fetchedPrinter = await fetchResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(fetchedPrinter);
        Assert.True(fetchedPrinter.IsPinned);
        Assert.Equal(9104, fetchedPrinter.TcpListenPort);
        Assert.True(fetchedPrinter.EmulateBufferCapacity);
        Assert.Equal(2048m, fetchedPrinter.BufferDrainRate);
        Assert.Equal(8192, fetchedPrinter.BufferMaxCapacity);

        var unpinResponse = await client.PostAsJsonAsync($"/api/printers/{printerId}/pin", new PinPrinterRequestDto(false));
        unpinResponse.EnsureSuccessStatusCode();
        var unpinnedPrinter = await unpinResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(unpinnedPrinter);
        Assert.False(unpinnedPrinter.IsPinned);
        Assert.Equal(9104, unpinnedPrinter.TcpListenPort);
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
        var createRequest = new CreatePrinterRequestDto(printerId, "Temp Printer", "EscPos", 512, null, 9102, false, null, null);
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
        var createRequest = new CreatePrinterRequestDto(printerId, "Shared Printer", "EscPos", 512, null, 9103, true, 1024, 4096);
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
        var createRequest = new CreatePrinterRequestDto(printerId, "Listener Printer", "EscPos", 384, null, 9105, false, null, null);
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
   
}



