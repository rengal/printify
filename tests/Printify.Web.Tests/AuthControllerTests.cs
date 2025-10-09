using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Printify.Domain.Services;

namespace Printify.Web.Tests;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Login_CreatesUserAndClaimsSession()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("TestUser"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, value => value.StartsWith("session_id", StringComparison.OrdinalIgnoreCase));

        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal("TestUser", user!.Name);

        using var scope = factory.Services.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IResourceQueryService>();
        var stored = await queryService.FindUserByNameAsync("TestUser");
        Assert.NotNull(stored);
        Assert.Equal(user.Id, stored!.Id);
    }

    [Fact]
    public async Task Me_WithClaimedSession_ReturnsUser()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("TokenUser"));
        var meResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var user = await meResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal("TokenUser", user!.Name);
    }

    [Fact]
    public async Task Me_WithoutSession_ReturnsUnauthorized()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record LoginRequest(string Username);
    private sealed record UserResponse(long Id, string Name);
}
