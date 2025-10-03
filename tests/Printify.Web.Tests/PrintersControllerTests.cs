using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts.Documents.Services;
using Printify.Contracts.Printers;
using Printify.Contracts.Users;

namespace Printify.Web.Tests;

public sealed class PrintersControllerTests
{
    [Fact]
    public async Task Create_Then_Get_ReturnsPersistedPrinter()
    {
        using var factory = new TestWebApplicationFactory();
        var scope = factory.Services.CreateScope();
        var commandService = scope.ServiceProvider.GetRequiredService<IResouceCommandService>();
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
}
