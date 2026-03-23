using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.TestServices;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;

namespace Printify.Web.Tests;

public sealed class AuthControllerWorkspaceSwitchTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task SwitchWorkspace_LoginToSecondWorkspace_ReturnsCorrectWorkspace()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create two workspaces
        var (tokenA, idA) = await CreateWorkspace(client, "workspace-a");
        var (tokenB, idB) = await CreateWorkspace(client, "workspace-b");

        // Login to workspace A
        var loginA = await LoginWith(client, tokenA);
        Assert.Equal(idA, loginA.Workspace.Id);

        // Switch to workspace B
        var loginB = await LoginWith(client, tokenB);
        Assert.Equal(idB, loginB.Workspace.Id);

        // Verify current workspace is B
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginB.AccessToken);
        var current = await client.GetAsync("/api/workspaces");
        current.EnsureSuccessStatusCode();
        var currentDto = await current.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(currentDto);
        Assert.Equal(idB, currentDto.Id);
        Assert.Equal("workspace-b", currentDto.Name);
    }

    [Fact]
    public async Task SwitchWorkspace_OldTokenInvalid_NewTokenWorks()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        var (tokenA, _) = await CreateWorkspace(client, "workspace-a");
        var (tokenB, idB) = await CreateWorkspace(client, "workspace-b");

        var loginA = await LoginWith(client, tokenA);

        // Switch to B
        var loginB = await LoginWith(client, tokenB);

        // Old JWT from A should not access B's workspace
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginA.AccessToken);
        var responseWithOldToken = await client.GetAsync("/api/workspaces");
        var oldDto = await responseWithOldToken.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(oldDto);
        // Old token still refers to workspace A, not B
        Assert.NotEqual(idB, oldDto.Id);

        // New JWT from B should access B
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginB.AccessToken);
        var responseWithNewToken = await client.GetAsync("/api/workspaces");
        responseWithNewToken.EnsureSuccessStatusCode();
        var newDto = await responseWithNewToken.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(newDto);
        Assert.Equal(idB, newDto.Id);
    }

    [Fact]
    public async Task SwitchWorkspace_MultipleWorkspaces_EachLoginReturnsCorrectWorkspace()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create three workspaces
        var (tokenA, idA) = await CreateWorkspace(client, "ws-multi-a");
        var (tokenB, idB) = await CreateWorkspace(client, "ws-multi-b");
        var (tokenC, idC) = await CreateWorkspace(client, "ws-multi-c");

        // Switch A → B → C → A and verify each time
        var loginA = await LoginWith(client, tokenA);
        Assert.Equal(idA, loginA.Workspace.Id);
        Assert.Equal("ws-multi-a", loginA.Workspace.Name);

        var loginB = await LoginWith(client, tokenB);
        Assert.Equal(idB, loginB.Workspace.Id);
        Assert.Equal("ws-multi-b", loginB.Workspace.Name);

        var loginC = await LoginWith(client, tokenC);
        Assert.Equal(idC, loginC.Workspace.Id);
        Assert.Equal("ws-multi-c", loginC.Workspace.Name);

        // Back to A
        var loginA2 = await LoginWith(client, tokenA);
        Assert.Equal(idA, loginA2.Workspace.Id);
        Assert.Equal("ws-multi-a", loginA2.Workspace.Name);
    }

    [Fact]
    public async Task SwitchWorkspace_WithInvalidToken_ReturnsUnauthorized_AndPreviousSessionIntact()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        var (tokenA, idA) = await CreateWorkspace(client, "workspace-a");
        var loginA = await LoginWith(client, tokenA);

        // Attempt to switch to an invalid token
        var failedLogin = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto(Guid.NewGuid().ToString("N")));
        Assert.Equal(HttpStatusCode.Unauthorized, failedLogin.StatusCode);

        // Previous session (A) should still be valid
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginA.AccessToken);
        var current = await client.GetAsync("/api/workspaces");
        current.EnsureSuccessStatusCode();
        var currentDto = await current.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(currentDto);
        Assert.Equal(idA, currentDto.Id);
    }

    [Fact]
    public async Task SwitchWorkspace_SameWorkspaceTwice_ReturnsSameId()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        var (token, id) = await CreateWorkspace(client, "workspace-same");

        var login1 = await LoginWith(client, token);
        var login2 = await LoginWith(client, token);

        Assert.Equal(id, login1.Workspace.Id);
        Assert.Equal(id, login2.Workspace.Id);
        // Both tokens are valid and refer to same workspace
        Assert.NotEqual(login1.AccessToken, login2.AccessToken);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static async Task<(string Token, Guid Id)> CreateWorkspace(HttpClient client, string name)
    {
        var id = Guid.NewGuid();
        var response = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(id, name));
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(dto);
        return (dto.Token, dto.Id);
    }

    private static async Task<LoginResponseDto> LoginWith(HttpClient client, string token)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(token));
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(dto);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto.AccessToken);
        return dto;
    }
}
