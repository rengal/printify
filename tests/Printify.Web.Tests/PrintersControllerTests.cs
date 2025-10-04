using System.Collections.Generic;
using System.Net;
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
        var scope = factory.Services.CreateScope();
        var commandService = scope.ServiceProvider.GetRequiredService<IResourceCommandService>();
        var ownerId = await commandService.CreateUserAsync(new SaveUserRequest("Owner", "127.0.0.1"));

        var client = factory.CreateClient();
        var request = new SavePrinterRequest(ownerId, "Kitchen", "escpos", 384, null, "127.0.0.1");
        var createResponse = await client.PostAsJsonAsync("/api/printers", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<Printer>();
        Assert.NotNull(created);
        Assert.Equal(request.DisplayName, created!.DisplayName);
        Assert.Equal(request.OwnerUserId, created.OwnerUserId);
        Assert.True(created.Id > 0);

        var getResponse = await client.GetAsync($"/api/printers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<Printer>();
        Assert.NotNull(fetched);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task List_WithUserIdFilter_ReturnsOwnedPrinters()
    {
        using var factory = new TestWebApplicationFactory();
        var scope = factory.Services.CreateScope();
        var commandService = scope.ServiceProvider.GetRequiredService<IResourceCommandService>();
        var ownerA = await commandService.CreateUserAsync(new SaveUserRequest("OwnerA", "127.0.0.1"));
        var ownerB = await commandService.CreateUserAsync(new SaveUserRequest("OwnerB", "127.0.0.2"));

        await commandService.CreatePrinterAsync(new SavePrinterRequest(ownerA, "Kitchen", "escpos", 384, null, "127.0.0.1"));
        await commandService.CreatePrinterAsync(new SavePrinterRequest(ownerA, "Bar", "escpos", 384, null, "127.0.0.1"));
        await commandService.CreatePrinterAsync(new SavePrinterRequest(ownerB, "Register", "escpos", 384, null, "127.0.0.2"));

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/printers?userId={ownerA}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var printers = await response.Content.ReadFromJsonAsync<IReadOnlyList<Printer>>();
        Assert.NotNull(printers);
        Assert.Equal(2, printers!.Count);
        Assert.All(printers, printer => Assert.Equal(ownerA, printer.OwnerUserId));
    }

    [Fact]
    public async Task List_WithInvalidUserId_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/printers?userId=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}