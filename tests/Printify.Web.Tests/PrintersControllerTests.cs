using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;
using Printify.Contracts.Users;

namespace Printify.Web.Tests;

public sealed class PrintersControllerTests
{
    [Fact]
    public async Task Create_Then_Get_ReturnsPersistedPrinter()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var auth = await LoginAsync(client, "Owner");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        using var scope = factory.Services.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IResourceQueryService>();
        var user = await queryService.FindUserByNameAsync("Owner");
        Assert.NotNull(user);

        var request = new SavePrinterRequest(user!.Id, "Kitchen", "escpos", 384, null, "127.0.0.1");
        var createResponse = await client.PostAsJsonAsync("/api/printers", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<Printer>();
        Assert.NotNull(created);
        Assert.Equal("Kitchen", created!.DisplayName);
        Assert.Equal(user.Id, created.OwnerUserId);
        Assert.True(created.Id > 0);

        var getResponse = await client.GetAsync($"/api/printers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<Printer>();
        Assert.NotNull(fetched);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task List_ReturnsPrintersOwnedByTokenUser()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var auth = await LoginAsync(client, "OwnerA");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        using var scope = factory.Services.CreateScope();
        var commandService = scope.ServiceProvider.GetRequiredService<IResourceCommandService>();
        var queryService = scope.ServiceProvider.GetRequiredService<IResourceQueryService>();

        var ownerA = await queryService.FindUserByNameAsync("OwnerA");
        Assert.NotNull(ownerA);
        var ownerBId = await commandService.CreateUserAsync(new SaveUserRequest("OwnerB", "127.0.0.2"));

        await commandService.CreatePrinterAsync(new SavePrinterRequest(ownerA!.Id, "Kitchen", "escpos", 384, null, "127.0.0.1"));
        await commandService.CreatePrinterAsync(new SavePrinterRequest(ownerA.Id, "Bar", "escpos", 384, null, "127.0.0.1"));
        await commandService.CreatePrinterAsync(new SavePrinterRequest(ownerBId, "Register", "escpos", 384, null, "127.0.0.2"));

        var response = await client.GetAsync("/api/printers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var printers = await response.Content.ReadFromJsonAsync<IReadOnlyList<Printer>>();
        Assert.NotNull(printers);
        Assert.Equal(2, printers!.Count);
        Assert.All(printers, printer => Assert.Equal(ownerA.Id, printer.OwnerUserId));
    }

    [Fact]
    public async Task List_WithoutAuthorization_ReturnsUnauthorized()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/printers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<AuthResponse> LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!;
    }

    private sealed record LoginRequest(string Username);
    private sealed record AuthResponse(string Token, int ExpiresIn, UserResponse User);
    private sealed record UserResponse(long Id, string Name);
}