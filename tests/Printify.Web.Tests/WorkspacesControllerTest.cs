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
    public async Task CreateWorkspace_WithSameIDTwice_ReturnsExistingWorkspace()
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

    [Fact]
    public async Task UpdateWorkspace_WithValidRequest_UpdatesWorkspaceName()
    {
        const string ownerName = "owner-name";
        const string updatedName = "updated-name";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, ownerName));
        createResponse.EnsureSuccessStatusCode();
        var workspaceResponse = await createResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponse);

        await AuthHelper.Login(client, workspaceResponse.Token);

        // Update workspace name
        var updateRequest = new UpdateWorkspaceRequestDto(updatedName, null);
        var updateResponse = await client.PatchAsJsonAsync("/api/workspaces", updateRequest);
        updateResponse.EnsureSuccessStatusCode();
        var updatedWorkspace = await updateResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(updatedWorkspace);
        Assert.Equal(updatedName, updatedWorkspace.OwnerName);

        // Verify update persisted
        var fetchResponse = await client.GetAsync("/api/workspaces");
        fetchResponse.EnsureSuccessStatusCode();
        var fetchedWorkspace = await fetchResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(fetchedWorkspace);
        Assert.Equal(updatedName, fetchedWorkspace.OwnerName);
    }

    [Fact]
    public async Task UpdateWorkspace_WithInvalidDocumentRetentionDays_ReturnsBadRequest()
    {
        const string ownerName = "owner-name";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, ownerName));
        createResponse.EnsureSuccessStatusCode();
        var workspaceResponse = await createResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponse);

        await AuthHelper.Login(client, workspaceResponse.Token);

        // Try to update with invalid retention days (0, below minimum)
        var updateRequest = new UpdateWorkspaceRequestDto(null, 0);
        var updateResponse = await client.PatchAsJsonAsync("/api/workspaces", updateRequest);
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);

        // Try to update with invalid retention days (366, above maximum)
        updateRequest = new UpdateWorkspaceRequestDto(null, 366);
        updateResponse = await client.PatchAsJsonAsync("/api/workspaces", updateRequest);
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkspace_RemovesWorkspace()
    {
        const string ownerName = "owner-name";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, ownerName));
        createResponse.EnsureSuccessStatusCode();
        var workspaceResponse = await createResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponse);

        await AuthHelper.Login(client, workspaceResponse.Token);

        // Delete workspace
        var deleteResponse = await client.DeleteAsync("/api/workspaces");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify workspace is deleted
        var fetchResponse = await client.GetAsync("/api/workspaces");
        Assert.Equal(HttpStatusCode.Unauthorized, fetchResponse.StatusCode);
    }
}
