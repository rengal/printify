using System.Net;
using System.Net.Http.Json;
using Printify.Contracts.Resources;

namespace Printify.Web.Tests;

public sealed class UsersControllerTests
{
    [Fact]
    public async Task Create_ReturnsNotImplemented()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new SaveUserRequest("Alice", "127.0.0.1"));

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsNotImplemented()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/1");

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }
}
