using Printify.TestServices;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Printify.Web.Tests;

internal class AuthHelper
{
    public static async Task CreateWorkspaceAndLogin(TestServiceContext.ControllerTestContext environment)
    {
        var client = environment.Client;

        var workspaceName = "workspace_" + Guid.NewGuid().ToString("N");

        // Create new workspace
        var workspaceId = Guid.NewGuid();
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequestDto(workspaceId, workspaceName));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceResponseDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponseDto);
        Assert.Equal(workspaceId, workspaceResponseDto.Id);
        var token = workspaceResponseDto.Token;

        // Login to workspace using token and get jwt access token
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(token));
        loginResponse.EnsureSuccessStatusCode();
        var loginResponseDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginResponseDto);
        Assert.NotNull(loginResponseDto.Workspace);
        Assert.Equal(workspaceId, loginResponseDto.Workspace.Id);
        var accessToken = loginResponseDto.AccessToken;

        // Set jwt access token for further requests
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static async Task Login(HttpClient client, string token)
    {
        // Login to workspace using token and get jwt access token
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(token));
        loginResponse.EnsureSuccessStatusCode();
        var loginResponseDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginResponseDto);
        var accessToken = loginResponseDto.AccessToken;

        // Set jwt access token for further requests
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
