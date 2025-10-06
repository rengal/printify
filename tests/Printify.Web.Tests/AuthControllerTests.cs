using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts.Services;
using Printify.Contracts.Users;

namespace Printify.Web.Tests;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Login_CreatesUserAndReturnsToken()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("TestUser"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.Equal("TestUser", auth.User.Name);

        using var scope = factory.Services.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IResourceQueryService>();
        var user = await queryService.FindUserByNameAsync("TestUser");
        Assert.NotNull(user);
        Assert.Equal(auth.User.Id, user!.Id);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUser()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("TokenUser"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Authorization", $"Bearer {auth!.Token}");
        var meResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var user = await meResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal("TokenUser", user!.Name);
    }

    [Fact]
    public async Task Me_WithInvalidToken_ReturnsUnauthorized()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Authorization", "Bearer invalid-token");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record LoginRequest(string Username);
    private sealed record AuthResponse(string Token, int ExpiresIn, UserResponse User);
    private sealed record UserResponse(long Id, string Name);
}