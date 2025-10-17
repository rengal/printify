using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Printify.Web.Tests;

public sealed class UserControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_OnBaseRoute_ReturnsNotFound()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
