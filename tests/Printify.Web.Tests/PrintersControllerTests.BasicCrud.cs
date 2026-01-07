using System.Net;
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

public sealed partial class PrintersControllerTests
{
    [Fact]
    public async Task UpdatePrinter_WithValidRequest_UpdatesPrinter()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Receipt Printer"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var updateBody = new UpdatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Updated Printer"),
            new PrinterSettingsRequestDto("EscPos", 576, null, true, 1024m, 4096));
        var updateResponse = await client.PutAsJsonAsync($"/api/printers/{printerId}", updateBody);
        updateResponse.EnsureSuccessStatusCode();

        var updatedPrinter = await updateResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(updatedPrinter);
        Assert.Equal("Updated Printer", updatedPrinter.Printer.DisplayName);
        Assert.Equal(576, updatedPrinter.Settings.WidthInDots);
        Assert.True(updatedPrinter.Settings.TcpListenPort > 0);
        Assert.True(updatedPrinter.Settings.EmulateBufferCapacity);
        Assert.Equal(1024m, updatedPrinter.Settings.BufferDrainRate);
        Assert.Equal(4096, updatedPrinter.Settings.BufferMaxCapacity);
        Assert.False(updatedPrinter.Printer.IsPinned);

        var fetchResponse = await client.GetAsync($"/api/printers/{printerId}");
        fetchResponse.EnsureSuccessStatusCode();
        var fetchedPrinter = await fetchResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(fetchedPrinter);
        Assert.Equal("Updated Printer", fetchedPrinter.Printer.DisplayName);
        Assert.True(fetchedPrinter.Settings.TcpListenPort > 0);
        Assert.True(fetchedPrinter.Settings.EmulateBufferCapacity);
        Assert.Equal(1024m, fetchedPrinter.Settings.BufferDrainRate);
        Assert.Equal(4096, fetchedPrinter.Settings.BufferMaxCapacity);
        Assert.False(fetchedPrinter.Printer.IsPinned);
    }

    [Fact]
    public async Task PinPrinter_TogglesPinnedState()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Pin Printer"),
            new PrinterSettingsRequestDto("EscPos", 512, null, true, 2048m, 8192));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var pinResponse = await client.PostAsJsonAsync($"/api/printers/{printerId}/pin", new PinPrinterRequestDto(true));
        pinResponse.EnsureSuccessStatusCode();
        var pinnedPrinter = await pinResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(pinnedPrinter);
        Assert.True(pinnedPrinter.Printer.IsPinned);
        Assert.True(pinnedPrinter.Settings.TcpListenPort > 0);
        Assert.True(pinnedPrinter.Settings.EmulateBufferCapacity);
        Assert.Equal(2048m, pinnedPrinter.Settings.BufferDrainRate);
        Assert.Equal(8192, pinnedPrinter.Settings.BufferMaxCapacity);

        var fetchResponse = await client.GetAsync($"/api/printers/{printerId}");
        fetchResponse.EnsureSuccessStatusCode();
        var fetchedPrinter = await fetchResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(fetchedPrinter);
        Assert.True(fetchedPrinter.Printer.IsPinned);
        Assert.True(fetchedPrinter.Settings.TcpListenPort > 0);
        Assert.True(fetchedPrinter.Settings.EmulateBufferCapacity);
        Assert.Equal(2048m, fetchedPrinter.Settings.BufferDrainRate);
        Assert.Equal(8192, fetchedPrinter.Settings.BufferMaxCapacity);

        var unpinResponse = await client.PostAsJsonAsync($"/api/printers/{printerId}/pin", new PinPrinterRequestDto(false));
        unpinResponse.EnsureSuccessStatusCode();
        var unpinnedPrinter = await unpinResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(unpinnedPrinter);
        Assert.False(unpinnedPrinter.Printer.IsPinned);
        Assert.True(unpinnedPrinter.Settings.TcpListenPort > 0);
        Assert.True(unpinnedPrinter.Settings.EmulateBufferCapacity);
        Assert.Equal(2048m, unpinnedPrinter.Settings.BufferDrainRate);
        Assert.Equal(8192, unpinnedPrinter.Settings.BufferMaxCapacity);
    }

    [Fact]
    public async Task DeletePrinter_RemovesPrinter()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Temp Printer"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
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
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerId, "Shared Printer"),
            new PrinterSettingsRequestDto("EscPos", 512, null, true, 1024, 4096));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var deleteResponse = await client.DeleteAsync($"/api/printers/{printerId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }
}
