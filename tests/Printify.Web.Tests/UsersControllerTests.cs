using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Domain.AnonymousSessions;
using Printify.Domain.Users;
using Printify.Infrastructure.Persistence;
using Printify.TestServices;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Users.Requests;
using Printify.Web.Contracts.Users.Responses;
using Xunit;

namespace Printify.Web.Tests;

public sealed class UsersControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory = factory;

    [Fact]
    public async Task CreateUser_WithValidRequest_ReturnsCreatedUser()
    {
        const string adminDisplayName = "admin-user";
        const string newDisplayName = "new-user";

        await using var environment = TestServiceContext.CreateForAuthControllerTest(this.factory);
        var client = environment.Client;

        var accessToken = await AuthenticateAsync(environment, adminDisplayName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequestDto(newDisplayName));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdUser = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(createdUser);
        Assert.Equal(newDisplayName, createdUser!.Name);

        var fetchResponse = await client.GetAsync($"/api/users/{createdUser.Id}");
        fetchResponse.EnsureSuccessStatusCode();

        var fetchedUser = await fetchResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(fetchedUser);
        Assert.Equal(createdUser.Id, fetchedUser!.Id);
        Assert.Equal(newDisplayName, fetchedUser.Name);
    }

    [Fact]
    public async Task ListUsers_ExcludesSoftDeletedUsers()
    {
        const string activeDisplayName = "list-active";
        const string deletedDisplayName = "list-deleted";

        await using var environment = TestServiceContext.CreateForAuthControllerTest(this.factory);
        await using (var scope = environment.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
            await context.Database.EnsureCreatedAsync();

            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            await repository.AddAsync(new User(Guid.NewGuid(), activeDisplayName, DateTimeOffset.UtcNow, "127.0.0.1", false), CancellationToken.None);
            await repository.AddAsync(new User(Guid.NewGuid(), deletedDisplayName, DateTimeOffset.UtcNow, "127.0.0.1", true), CancellationToken.None);
        }

        var response = await environment.Client.GetAsync("/api/users");
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<IReadOnlyList<UserDto>>();
        Assert.NotNull(users);
        Assert.Contains(users!, user => user.Name == activeDisplayName);
        Assert.DoesNotContain(users!, user => user.Name == deletedDisplayName);
    }

    private static async Task<string> AuthenticateAsync(TestServiceContext.AuthControllerTestContext environment, string displayName)
    {
        var client = environment.Client;
        string anonymousToken;

        await using (var scope = environment.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
            await context.Database.EnsureCreatedAsync();

            var sessionRepository = scope.ServiceProvider.GetRequiredService<IAnonymousSessionRepository>();
            var jwtGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var adminUser = new User(Guid.NewGuid(), displayName, DateTimeOffset.UtcNow, "127.0.0.1", false);
            await userRepository.AddAsync(adminUser, CancellationToken.None);

            var session = AnonymousSession.Create("127.0.0.1");
            await sessionRepository.AddAsync(session, CancellationToken.None);

            anonymousToken = jwtGenerator.GenerateToken(null, session.Id);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", anonymousToken);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(displayName));
        loginResponse.EnsureSuccessStatusCode();

        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);

        return loginDto!.AccessToken;
    }
}
