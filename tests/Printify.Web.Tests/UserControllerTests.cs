using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.TestServices;
using Xunit;

namespace Printify.Web.Tests;

public sealed class UserControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_OnBaseRoute_ReturnsSuccess()
    {
        await using var environment = TestServiceContext.CreateForAuthControllerTest(factory);

        var response = await environment.Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
