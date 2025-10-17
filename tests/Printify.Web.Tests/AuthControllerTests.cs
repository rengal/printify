using Xunit;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Domain.AnonymousSessions;
using Printify.Domain.Users;
using Printify.TestServices;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Users.Responses;
using Xunit.Sdk;

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
        var loginRequest = new LoginRequestDto(displayName);

        await using var environment = TestServiceContext.CreateForAuthControllerTest(this.factory);
        var client = environment.Client;

        string anonymousToken;
        await using (var scope = environment.CreateScope())
        {
            var sessionRepository = scope.ServiceProvider.GetRequiredService<IAnonymousSessionRepository>();
            var jwtGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

            var session = AnonymousSession.Create("127.0.0.1");
            await sessionRepository.AddAsync(session, CancellationToken.None);

            anonymousToken = jwtGenerator.GenerateToken(null, session.Id);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", anonymousToken);

        var failedResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);

        await using (var scope = environment.CreateScope())
        {
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            await userRepository.AddAsync(
                new User(Guid.NewGuid(), displayName, DateTimeOffset.UtcNow, "127.0.0.1", false),
                CancellationToken.None);
        }

        var successfulResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        if (!successfulResponse.IsSuccessStatusCode)
        {
            var error = await successfulResponse.Content.ReadAsStringAsync();
            throw new XunitException($"Expected OK but received {(int)successfulResponse.StatusCode}: {error}");
        }

        var loginDto = await successfulResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);
        Assert.False(string.IsNullOrWhiteSpace(loginDto!.AccessToken));
        Assert.Equal(displayName, loginDto.User.Name);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.AccessToken);

        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();

        var userDto = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(userDto);
        Assert.Equal(displayName, userDto!.Name);
    }
}

