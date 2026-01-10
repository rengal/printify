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
        const string workspaceName = "workspace-name";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create new workspace
        var workspaceId = Guid.NewGuid();
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, workspaceName));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceResponseDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponseDto);
        Assert.Equal(workspaceId, workspaceResponseDto.Id);
        Assert.Equal(workspaceName, workspaceResponseDto.Name);
    }


    [Fact]
    public async Task CreateWorkspace_WithSameIDTwice_ReturnsExistingWorkspace()
    {
        const string workspaceName = "idempotent-workspace";
        var workspaceId = Guid.NewGuid();

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        var request = new CreateWorkspaceRequestDto(workspaceId, workspaceName);

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
    public async Task UpdateWorkspace_WithValidRequest_UpdatesName()
    {
        const string workspaceName = "workspace-name";
        const string updatedName = "updated-name";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, workspaceName));
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
        Assert.Equal(updatedName, updatedWorkspace.Name);

        // Verify update persisted
        var fetchResponse = await client.GetAsync("/api/workspaces");
        fetchResponse.EnsureSuccessStatusCode();
        var fetchedWorkspace = await fetchResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(fetchedWorkspace);
        Assert.Equal(updatedName, fetchedWorkspace.Name);
    }

    [Fact]
    public async Task UpdateWorkspace_WithInvalidDocumentRetentionDays_ReturnsBadRequest()
    {
        const string workspaceName = "workspace-name";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, workspaceName));
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
        const string workspaceName = "workspace-name";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, workspaceName));
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

    // GET /api/workspaces - Basic fetch

    [Fact]
    public async Task GetWorkspace_WithAuth_ReturnsWorkspace()
    {
        const string workspaceName = "test-workspace";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, workspaceName));
        createResponse.EnsureSuccessStatusCode();
        var workspaceResponse = await createResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponse);

        await AuthHelper.Login(client, workspaceResponse.Token);

        // Get workspace
        var getResponse = await client.GetAsync("/api/workspaces");
        getResponse.EnsureSuccessStatusCode();
        var workspaceDto = await getResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(workspaceDto);
        Assert.Equal(workspaceId, workspaceDto.Id);
        Assert.Equal(workspaceName, workspaceDto.Name);
    }

    // Unauthorized access tests

    [Fact]
    public async Task GetWorkspace_WithoutAuth_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        var getResponse = await client.GetAsync("/api/workspaces");
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkspace_WithoutAuth_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        var updateRequest = new UpdateWorkspaceRequestDto("new-name", 30);
        var updateResponse = await client.PatchAsJsonAsync("/api/workspaces", updateRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkspace_WithoutAuth_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        var deleteResponse = await client.DeleteAsync("/api/workspaces");
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }

    // UpdateWorkspace - DocumentRetentionDays

    [Fact]
    public async Task UpdateWorkspace_WithValidDocumentRetentionDays_UpdatesRetentionDays()
    {
        const string workspaceName = "test-workspace";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, workspaceName));
        createResponse.EnsureSuccessStatusCode();
        var workspaceResponse = await createResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponse);

        await AuthHelper.Login(client, workspaceResponse.Token);

        // Update document retention days
        var updateRequest = new UpdateWorkspaceRequestDto(null, 90);
        var updateResponse = await client.PatchAsJsonAsync("/api/workspaces", updateRequest);
        updateResponse.EnsureSuccessStatusCode();
        var updatedWorkspace = await updateResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(updatedWorkspace);

        // Verify update persisted
        var fetchResponse = await client.GetAsync("/api/workspaces");
        fetchResponse.EnsureSuccessStatusCode();
        var fetchedWorkspace = await fetchResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(fetchedWorkspace);
        Assert.Equal(workspaceName, fetchedWorkspace.Name);
    }

    [Fact]
    public async Task UpdateWorkspace_WithNameAndRetentionDays_UpdatesBoth()
    {
        const string workspaceName = "test-workspace";
        const string updatedName = "updated-workspace";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, workspaceName));
        createResponse.EnsureSuccessStatusCode();
        var workspaceResponse = await createResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponse);

        await AuthHelper.Login(client, workspaceResponse.Token);

        // Update both name and retention days
        var updateRequest = new UpdateWorkspaceRequestDto(updatedName, 60);
        var updateResponse = await client.PatchAsJsonAsync("/api/workspaces", updateRequest);
        updateResponse.EnsureSuccessStatusCode();
        var updatedWorkspace = await updateResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(updatedWorkspace);
        Assert.Equal(updatedName, updatedWorkspace.Name);

        // Verify update persisted
        var fetchResponse = await client.GetAsync("/api/workspaces");
        fetchResponse.EnsureSuccessStatusCode();
        var fetchedWorkspace = await fetchResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(fetchedWorkspace);
        Assert.Equal(updatedName, fetchedWorkspace.Name);
    }

    // Edge cases

    [Fact]
    public async Task UpdateWorkspace_WithNullFields_DoesNotChangeValues()
    {
        const string workspaceName = "test-workspace";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, workspaceName));
        createResponse.EnsureSuccessStatusCode();
        var workspaceResponse = await createResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponse);

        await AuthHelper.Login(client, workspaceResponse.Token);

        // Get initial state
        var initialGetResponse = await client.GetAsync("/api/workspaces");
        initialGetResponse.EnsureSuccessStatusCode();
        var initialWorkspace = await initialGetResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(initialWorkspace);

        // Update with null fields (no-op)
        var updateRequest = new UpdateWorkspaceRequestDto(null, null);
        var updateResponse = await client.PatchAsJsonAsync("/api/workspaces", updateRequest);
        updateResponse.EnsureSuccessStatusCode();
        var updatedWorkspace = await updateResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(updatedWorkspace);
        Assert.Equal(initialWorkspace.Name, updatedWorkspace.Name);
    }

    // Boundary value tests for DocumentRetentionDays

    [Fact]
    public async Task UpdateWorkspace_WithMinimumRetentionDays_AcceptsValue()
    {
        const string workspaceName = "test-workspace";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, workspaceName));
        createResponse.EnsureSuccessStatusCode();
        var workspaceResponse = await createResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponse);

        await AuthHelper.Login(client, workspaceResponse.Token);

        // Update with minimum valid retention days (1)
        var updateRequest = new UpdateWorkspaceRequestDto(null, 1);
        var updateResponse = await client.PatchAsJsonAsync("/api/workspaces", updateRequest);
        updateResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task UpdateWorkspace_WithMaximumRetentionDays_AcceptsValue()
    {
        const string workspaceName = "test-workspace";

        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, workspaceName));
        createResponse.EnsureSuccessStatusCode();
        var workspaceResponse = await createResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponse);

        await AuthHelper.Login(client, workspaceResponse.Token);

        // Update with maximum valid retention days (365)
        var updateRequest = new UpdateWorkspaceRequestDto(null, 365);
        var updateResponse = await client.PatchAsJsonAsync("/api/workspaces", updateRequest);
        updateResponse.EnsureSuccessStatusCode();
    }
}
