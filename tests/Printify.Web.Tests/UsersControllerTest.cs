using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Domain.Users;
using Printify.Infrastructure.Persistence;
using Printify.TestServices;
using Printify.Web.Contracts.Users.Requests;
using Printify.Web.Contracts.Users.Responses;
using Xunit;

namespace Printify.Web.Tests;

public sealed class UsersControllerTest(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory = factory;

    [Fact]
    public async Task Get_OnBaseRoute_ReturnsSuccess()
    {
        await using var environment = TestServiceContext.CreateForAuthControllerTest(this.factory);

        var response = await environment.Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithValidRequest_ReturnsCreatedUser()
    {
        const string newDisplayName = "new-user";
        var userId = Guid.NewGuid();

        await using var environment = TestServiceContext.CreateForAuthControllerTest(this.factory);
        var client = environment.Client;

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequestDto(userId, newDisplayName));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var createdUser = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(createdUser);
        Assert.Equal(newDisplayName, createdUser!.Name);
        Assert.Equal(userId, createdUser.Id);

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

    [Fact]
    public async Task CreateUser_WithSameIdTwice_ReturnsExistingUser()
    {
        const string displayName = "idempotent-user";
        var userId = Guid.NewGuid();

        await using var environment = TestServiceContext.CreateForAuthControllerTest(this.factory);
        var client = environment.Client;

        var request = new CreateUserRequestDto(userId, displayName);

        var firstResponse = await client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var firstUser = await firstResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(firstUser);
        Assert.Equal(userId, firstUser!.Id);

        var secondResponse = await client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var secondUser = await secondResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(secondUser);
        Assert.Equal(firstUser.Id, secondUser!.Id);

        var listResponse = await client.GetAsync("/api/users");
        listResponse.EnsureSuccessStatusCode();

        var users = await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<UserDto>>();
        Assert.NotNull(users);
        var matchingCount = users!.Count(user => user.Id == userId);
        Assert.Equal(1, matchingCount);
    }
}
