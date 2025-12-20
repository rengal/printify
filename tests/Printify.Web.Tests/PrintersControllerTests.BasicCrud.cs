using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;

namespace Printify.Web.Tests;

public sealed partial class PrintersControllerTests
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
}
