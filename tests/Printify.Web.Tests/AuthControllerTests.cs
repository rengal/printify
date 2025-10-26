using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.TestServices;
using Printify.Web.Contracts.Auth.AnonymousSession.Response;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Users.Requests;
using Printify.Web.Contracts.Users.Responses;
using Xunit;

namespace Printify.Web.Tests;

public sealed class AuthControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory = factory;

    [Fact]
    public async Task Login_WithNullRequest_ReturnsBadRequest()
    {
        await using var environment = TestServiceContext.CreateForAuthControllerTest(this.factory);

        var response = await environment.Client.PostAsync(
            "/api/auth/login",
            new StringContent("null", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWhitespaceDisplayName_ReturnsServerError()
    {
        await using var environment = TestServiceContext.CreateForAuthControllerTest(this.factory);

        var response = await environment.Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("   "));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForAuthControllerTest(this.factory);

        var response = await environment.Client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithMalformedBearerToken_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForAuthControllerTest(this.factory);

        environment.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "totally-invalid-token");

        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await environment.Client.PostAsync("/api/auth/logout", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenUserAddedAfterFailure_AllowsAuthenticatedMe()
    {
        const string displayName = "auth-tests-user";
        await using var environment = TestServiceContext.CreateForAuthControllerTest(this.factory);
        var client = environment.Client;

        // 1. Create anonymous session via API to obtain session token
        var anonymousResponse = await client.PostAsJsonAsync("/api/auth/anonymous", new { });
        anonymousResponse.EnsureSuccessStatusCode();
        var session = await anonymousResponse.Content.ReadFromJsonAsync<AnonymousSessionDto>();
        Assert.NotNull(session);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.Id.ToString());

        // 2. Initial login attempt should fail because the user does not exist yet
        var failedLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(displayName));
        Assert.Equal(HttpStatusCode.Unauthorized, failedLogin.StatusCode);

        // 3. Register the missing user via API
        var registerResponse = await client.PostAsJsonAsync("/api/users", new CreateUserRequestDto(Guid.NewGuid(), displayName));
        registerResponse.EnsureSuccessStatusCode();

        // 4. Retry login and expect success
        var successfulLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(displayName));
        successfulLogin.EnsureSuccessStatusCode();
        var loginDto = await successfulLogin.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);
        Assert.False(string.IsNullOrWhiteSpace(loginDto!.AccessToken));
        Assert.Equal(displayName, loginDto.User.Name);

        // 5. Use the token to fetch the current user profile
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.AccessToken);
        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
        var userDto = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(userDto);
        Assert.Equal(displayName, userDto!.Name);
    }
}
