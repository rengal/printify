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

namespace Printify.Web.Tests;

public sealed class AuthControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Login_WithEmptyPayload_ReturnsBadRequest()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);

        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await environment.Client.PostAsync("/api/auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnsupportedContentType_ReturnsUnsupportedMediaType()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);

        using var content = new StringContent("<login />", Encoding.UTF8, "application/xml");
        var response = await environment.Client.PostAsync("/api/auth/login", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNullRequest_ReturnsBadRequest()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);

        var response = await environment.Client.PostAsync(
            "/api/auth/login",
            new StringContent("null", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithRandomUserId_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);

        var response = await environment.Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);

        var response = await environment.Client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithMalformedBearerToken_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);

        environment.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "totally-invalid-token");

        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await environment.Client.PostAsync("/api/auth/logout", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenUserAddedAfterFailure_AllowsAuthenticatedMe()
    {
        const string displayName = "auth-tests-user";
        Guid userid = Guid.NewGuid();
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // 1. Create anonymous session via API to obtain session token
        var anonymousResponse = await client.PostAsJsonAsync("/api/auth/anonymous", new { });
        anonymousResponse.EnsureSuccessStatusCode();
        var session = await anonymousResponse.Content.ReadFromJsonAsync<AnonymousSessionDto>();
        Assert.NotNull(session);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.Id.ToString());

        // 2. Initial login attempt should fail because the user does not exist yet
        var failedLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(userid));
        Assert.Equal(HttpStatusCode.Unauthorized, failedLogin.StatusCode);

        // 3. Register the missing user via API
        var registerResponse = await client.PostAsJsonAsync("/api/users", new CreateUserRequestDto(userid, displayName));
        registerResponse.EnsureSuccessStatusCode();

        // 4. Retry login and expect success
        var successfulLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(userid));
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

    private static async Task AuthenticateUserAsync(TestServiceContext.ControllerTestContext environment, Guid userId, string displayName)
    {
        var client = environment.Client;

        var anonymousResponse = await client.PostAsJsonAsync("/api/auth/anonymous", new { });
        anonymousResponse.EnsureSuccessStatusCode();
        var session = await anonymousResponse.Content.ReadFromJsonAsync<AnonymousSessionDto>();
        Assert.NotNull(session);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.Id.ToString());

        var registerResponse = await client.PostAsJsonAsync("/api/users", new CreateUserRequestDto(userId, displayName));
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(userId));
        loginResponse.EnsureSuccessStatusCode();

        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.AccessToken);
    }
}
