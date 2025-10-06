using System.Net;
using System.Net.Http.Json;
using Printify.Contracts.Users;

namespace Printify.Web.Tests;

public sealed class UsersControllerTests
{
    [Fact]
    public async Task Create_Then_Get_ReturnsPersistedUser()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new SaveUserRequest("Alice", "127.0.0.1");
        var createResponse = await client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(created);
        Assert.Equal(request.DisplayName, created!.DisplayName);
        Assert.Equal(request.CreatedFromIp, created.CreatedFromIp);
        Assert.True(created.Id > 0);

        var getResponse = await client.GetAsync($"/api/users/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(fetched);
        Assert.Equal(created, fetched);
    }
}
