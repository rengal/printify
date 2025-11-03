using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.TestServices;
using Printify.Web.Contracts.Auth.AnonymousSession.Response;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Printers.Responses;
using Printify.Web.Contracts.Users.Requests;

namespace Printify.Web.Tests;

public sealed class PrintersControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task UpdatePrinter_WithValidRequest_UpdatesPrinter()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        await AuthenticateAsync(environment, "printer-owner");

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "Receipt Printer", "EscPos", 512, null, 9100, false, null, null);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var updateBody = new UpdatePrinterRequestDto("Updated Printer", "EscPos", 576, null, 9101, true, 1024m, 4096);
        var updateResponse = await client.PutAsJsonAsync($"/api/printers/{printerId}", updateBody);
        updateResponse.EnsureSuccessStatusCode();

        var updatedPrinter = await updateResponse.Content.ReadFromJsonAsync<PrinterDto>();
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
        var fetchedPrinter = await fetchResponse.Content.ReadFromJsonAsync<PrinterDto>();
        Assert.NotNull(fetchedPrinter);
        Assert.Equal("Updated Printer", fetchedPrinter!.DisplayName);
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
        await AuthenticateAsync(environment, "pinner");

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "Pin Printer", "EscPos", 512, null, 9104, true, 2048m, 8192);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        var pinResponse = await client.PostAsJsonAsync($"/api/printers/{printerId}/pin", new PinPrinterRequestDto(true));
        pinResponse.EnsureSuccessStatusCode();
        var pinnedPrinter = await pinResponse.Content.ReadFromJsonAsync<PrinterDto>();
        Assert.NotNull(pinnedPrinter);
        Assert.True(pinnedPrinter!.IsPinned);
        Assert.Equal(9104, pinnedPrinter.TcpListenPort);
        Assert.True(pinnedPrinter.EmulateBufferCapacity);
        Assert.Equal(2048m, pinnedPrinter.BufferDrainRate);
        Assert.Equal(8192, pinnedPrinter.BufferMaxCapacity);

        var fetchResponse = await client.GetAsync($"/api/printers/{printerId}");
        fetchResponse.EnsureSuccessStatusCode();
        var fetchedPrinter = await fetchResponse.Content.ReadFromJsonAsync<PrinterDto>();
        Assert.NotNull(fetchedPrinter);
        Assert.True(fetchedPrinter.IsPinned);
        Assert.Equal(9104, fetchedPrinter.TcpListenPort);
        Assert.True(fetchedPrinter.EmulateBufferCapacity);
        Assert.Equal(2048m, fetchedPrinter.BufferDrainRate);
        Assert.Equal(8192, fetchedPrinter.BufferMaxCapacity);

        var unpinResponse = await client.PostAsJsonAsync($"/api/printers/{printerId}/pin", new PinPrinterRequestDto(false));
        unpinResponse.EnsureSuccessStatusCode();
        var unpinnedPrinter = await unpinResponse.Content.ReadFromJsonAsync<PrinterDto>();
        Assert.NotNull(unpinnedPrinter);
        Assert.False(unpinnedPrinter!.IsPinned);
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
        await AuthenticateAsync(environment, "printer-owner-delete");

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
        await AuthenticateAsync(environment, "owner-a");

        var printerId = Guid.NewGuid();
        var createRequest = new CreatePrinterRequestDto(printerId, "Shared Printer", "EscPos", 512, null, 9103, true, 1024, 4096);
        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        createResponse.EnsureSuccessStatusCode();

        await AuthenticateAsync(environment, "owner-b");
        var deleteResponse = await client.DeleteAsync($"/api/printers/{printerId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    private static async Task AuthenticateAsync(TestServiceContext.ControllerTestContext environment, string displayName)
    {
        var client = environment.Client;
        client.DefaultRequestHeaders.Remove("X-Forwarded-For");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "127.0.0.1");

        // Create anonymous session via API to seed caller context.
        var anonymousResponse = await client.PostAsync("/api/auth/anonymous", new StringContent(string.Empty));
        if (!anonymousResponse.IsSuccessStatusCode)
        {
            var error = await anonymousResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to create anonymous session: {(int)anonymousResponse.StatusCode} {error}");
        }
        var sessionDto = await anonymousResponse.Content.ReadFromJsonAsync<AnonymousSessionDto>();
        Assert.NotNull(sessionDto);

        // Register the user through the public API so the login flow can succeed.
        var userId = Guid.NewGuid();
        var createUserResponse = await client.PostAsJsonAsync("/api/users", new CreateUserRequestDto(userId, displayName));
        if (!createUserResponse.IsSuccessStatusCode)
        {
            var error = await createUserResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to create user: {(int)createUserResponse.StatusCode} {error}");
        }

        // Issue a JWT for the anonymous session to authenticate the login request.
        await using (var scope = environment.CreateScope())
        {
            var jwtGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
            var anonymousToken = jwtGenerator.GenerateToken(null, sessionDto!.Id);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", anonymousToken);
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(userId));
        if (!loginResponse.IsSuccessStatusCode)
        {
            var error = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to login: {(int)loginResponse.StatusCode} {error}");
        }

        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.AccessToken);
    }
}
