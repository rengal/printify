using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Printify.Domain.Workspaces;
using Printify.Infrastructure.Persistence;
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
    public async Task DeletePrinter_WithDifferentWorkspace_ReturnsForbidden()
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
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task AdminWorkspace_CanReadForeignPrinterButCannotMutateIt()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var foreignPrinterId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(
            new PrinterRequestDto(foreignPrinterId, "Foreign Printer"),
            new PrinterSettingsRequestDto("EscPos", 512, null, true, 1024, 4096));
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var (adminWorkspaceId, _) = await AuthHelper.CreateWorkspaceAndLoginReturningToken(environment);
        await using (var scope = environment.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
            var adminWorkspace = await dbContext.Workspaces.FindAsync(adminWorkspaceId);
            Assert.NotNull(adminWorkspace);
            adminWorkspace.Role = WorkspaceRole.Admin.ToString();
            await dbContext.SaveChangesAsync();
        }

        var getResponse = await client.GetAsync($"/api/printers/{foreignPrinterId}");
        getResponse.EnsureSuccessStatusCode();
        var printer = await getResponse.Content.ReadFromJsonAsync<PrinterResponseDto>();
        Assert.NotNull(printer);
        Assert.Equal(foreignPrinterId, printer.Printer.Id);
        Assert.Equal("Foreign Printer", printer.Printer.DisplayName);
        Assert.NotEqual(adminWorkspaceId, printer.Printer.OwnerWorkspaceId);
        Assert.False(string.IsNullOrWhiteSpace(printer.Printer.OwnerWorkspaceName));

        var pinResponse = await client.PostAsJsonAsync(
            $"/api/printers/{foreignPrinterId}/pin",
            new PinPrinterRequestDto(true));
        Assert.Equal(HttpStatusCode.Forbidden, pinResponse.StatusCode);
    }

    [Fact]
    public async Task ListPrinters_NormalWorkspace_ReturnsOnlyOwnPrinters()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Workspace A (Normal) creates a printer.
        var (workspaceAId, _) = await AuthHelper.CreateWorkspaceAndLoginReturningToken(environment);
        var printerAId = Guid.NewGuid();
        var createA = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerAId, "Printer A"),
            new PrinterSettingsRequestDto("EscPos", 512, null, false, null, null));
        (await client.PostAsJsonAsync("/api/printers", createA)).EnsureSuccessStatusCode();

        // Workspace B (a separate Normal workspace) creates its own printer; the
        // helper re-points the client's bearer token to B.
        var (workspaceBId, _) = await AuthHelper.CreateWorkspaceAndLoginReturningToken(environment);
        var printerBId = Guid.NewGuid();
        var createB = new CreatePrinterRequestDto(
            new PrinterRequestDto(printerBId, "Printer B"),
            new PrinterSettingsRequestDto("EscPos", 384, null, false, null, null));
        (await client.PostAsJsonAsync("/api/printers", createB)).EnsureSuccessStatusCode();

        // As B (Normal), GET /api/printers must return ONLY B's own printer — never A's.
        var listResponse = await client.GetAsync("/api/printers");
        listResponse.EnsureSuccessStatusCode();
        var printers = await listResponse.Content.ReadFromJsonAsync<PrinterResponseDto[]>();
        Assert.NotNull(printers);

        Assert.All(printers, p => Assert.Equal(workspaceBId, p.Printer.OwnerWorkspaceId));
        Assert.Contains(printers, p => p.Printer.Id == printerBId);
        Assert.DoesNotContain(printers, p => p.Printer.Id == printerAId);
        Assert.NotEqual(workspaceAId, workspaceBId);
        Assert.Single(printers);
    }
}
