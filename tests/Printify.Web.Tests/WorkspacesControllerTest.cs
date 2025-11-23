using Microsoft.AspNetCore.Mvc.Testing;
using Printify.TestServices;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;
using System.Net;
using System.Net.Http.Json;

namespace Printify.Web.Tests;

public sealed class WorkspacesControllerTest(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CreateWorkspace_WithValidRequest_ReturnsCreatedWorkspace()
    {
        const string ownerName = "owner-name";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create new workspace
        var workspaceId = Guid.NewGuid();
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, ownerName));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceResponseDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponseDto);
        Assert.Equal(workspaceId, workspaceResponseDto.Id);
        Assert.Equal(ownerName, workspaceResponseDto.OwnerName);
    }


    [Fact]
    public async Task CreateWorkspace_WithSameIdTwice_ReturnsExistingWorkspace()
    {
        const string ownerName = "idempotent-workspace";
        var workspaceId = Guid.NewGuid();

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        var request = new CreateWorkspaceRequestDto(workspaceId, ownerName);

        // First creation
        var firstResponse = await client.PostAsJsonAsync("/api/workspaces", request);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var firstWorkspace = await firstResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(firstWorkspace);
        Assert.Equal(workspaceId, firstWorkspace.Id);

        // Second creation with the same ID
        var secondResponse = await client.PostAsJsonAsync("/api/workspaces", request);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var secondWorkspace = await secondResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(secondWorkspace);
        Assert.Equal(firstWorkspace.Id, secondWorkspace.Id);
    }
}
