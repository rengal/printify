using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Printify.Domain.Printers;
using Printify.Domain.Services;
using Printify.Domain.Users;

namespace Printify.Web.Tests;

public sealed class PrintersControllerTests
{
    [Fact]
    public async Task Create_Then_Get_ReturnsPersistedPrinter()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var auth = await LoginAsync(client, "Owner");
        Assert.NotNull(auth);

        using var scope = factory.Services.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IResourceQueryService>();
        var user = await queryService.FindUserByNameAsync("Owner");
        Assert.NotNull(user);

        var createRequest = new
        {
            displayName = "Kitchen",
            protocol = "escpos",
            widthInDots = 384,
            heightInDots = (int?)null
        };

        var createResponse = await client.PostAsJsonAsync("/api/printers", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<Printer>();
        Assert.NotNull(created);
        Assert.Equal("Kitchen", created!.DisplayName);
        Assert.Equal(user!.Id, created.OwnerUserId);
        Assert.True(created.Id > 0);

        var getResponse = await client.GetAsync($"/api/printers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<Printer>();
        Assert.NotNull(fetched);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task List_ReturnsPrintersOwnedByTokenUserAndSession()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        // First temporary printer before login.
        var tempResponse = await client.PostAsJsonAsync("/api/printers", new { displayName = "Temp", protocol = "escpos", widthInDots = 384, heightInDots = (int?)null });
        Assert.Equal(HttpStatusCode.Created, tempResponse.StatusCode);
        var temporaryPrinter = await tempResponse.Content.ReadFromJsonAsync<Printer>();
        Assert.NotNull(temporaryPrinter);
        Assert.Null(temporaryPrinter!.OwnerUserId);

        // Login claims the session.
        var auth = await LoginAsync(client, "OwnerA");
        Assert.NotNull(auth);

        // Create printers owned by the user.
        await client.PostAsJsonAsync("/api/printers", new { displayName = "Kitchen", protocol = "escpos", widthInDots = 384, heightInDots = (int?)null });
        await client.PostAsJsonAsync("/api/printers", new { displayName = "Bar", protocol = "escpos", widthInDots = 384, heightInDots = (int?)null });

        using var otherScope = factory.Services.CreateScope();
        var commandService = otherScope.ServiceProvider.GetRequiredService<IResourceCommandService>();

        // Printer belonging to another session/user.
        var otherUserId = await commandService.CreateUserAsync(new SaveUserRequest("OwnerB", "127.0.0.2"));
        var sessionService = otherScope.ServiceProvider.GetRequiredService<ISessionService>();
        var otherSession = await sessionService.CreateAsync("127.0.0.2", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));
        await commandService.CreatePrinterAsync(new SavePrinterRequest(otherUserId, otherSession.Id, "Register", "escpos", 384, null, "127.0.0.2"));

        var response = await client.GetAsync("/api/printers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<PrinterListResponse>();
        Assert.NotNull(list);
        Assert.Single(list!.Temporary);
        Assert.Equal(temporaryPrinter.Id, list.Temporary.Single().Id);
        Assert.Equal(2, list.UserClaimed.Count);
    }

    [Fact]
    public async Task ResolveTemporary_AssignsPrintersToLoggedInUser()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var tempResponse = await client.PostAsJsonAsync("/api/printers", new { displayName = "Temp", protocol = "escpos", widthInDots = 384, heightInDots = (int?)null });
        var temporaryPrinter = await tempResponse.Content.ReadFromJsonAsync<Printer>();
        Assert.NotNull(temporaryPrinter);

        await LoginAsync(client, "Resolver");

        var resolveResponse = await client.PostAsJsonAsync("/api/printers/resolveTemporary", new { printerIds = new[] { temporaryPrinter!.Id } });
        Assert.Equal(HttpStatusCode.NoContent, resolveResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/printers");
        var list = await listResponse.Content.ReadFromJsonAsync<PrinterListResponse>();
        Assert.NotNull(list);
        Assert.Empty(list!.Temporary);
        Assert.Single(list.UserClaimed);
        Assert.Equal(temporaryPrinter.Id, list.UserClaimed.Single().Id);
    }

    [Fact]
    public async Task List_WithoutSessionCookie_CreatesSession()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/printers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, value => value.StartsWith("session_id", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<UserResponse?> LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserResponse>();
    }

    private sealed record LoginRequest(string Username);
    private sealed record UserResponse(long Id, string Name);
    private sealed record PrinterListResponse(IReadOnlyList<Printer> Temporary, IReadOnlyList<Printer> UserClaimed);
}
