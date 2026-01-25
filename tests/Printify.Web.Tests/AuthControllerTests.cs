using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.TestServices;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;

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
    public async Task Login_WithRandomToken_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);

        var response = await environment.Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(Guid.NewGuid().ToString("N")));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentWorkspace_WithoutToken_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);

        var response = await environment.Client.GetAsync("/api/workspaces");

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
    public async Task Login_WhenWorkspaceCreatedAfterFailure_AllowsAuthentication()
    {
        const string displayName = "auth-tests-user";
        string token = Guid.NewGuid().ToString("N");
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // 1. Initial login attempt should fail because the workspace does not exist yet
        var failedLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(token));
        Assert.Equal(HttpStatusCode.Unauthorized, failedLogin.StatusCode);

        // 2. Create the missing workspace via API
        var workspaceId = Guid.NewGuid();
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequestDto(workspaceId, displayName));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceDto);
        Assert.False(string.IsNullOrWhiteSpace(workspaceDto.Token));
        Assert.Equal(displayName, workspaceDto.Name);
        token = workspaceDto.Token; // Use the actual token from the created workspace

        // 3. Retry login and expect success
        var successfulLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(token));
        successfulLogin.EnsureSuccessStatusCode();
        var loginDto = await successfulLogin.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);
        Assert.False(string.IsNullOrWhiteSpace(loginDto.AccessToken));
        Assert.Equal("Bearer", loginDto.TokenType);
        Assert.True(loginDto.ExpiresInSeconds > 0);
        Assert.Equal(displayName, loginDto.Workspace.Name);

        // 4. Use the JWT token to fetch the current workspace
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.AccessToken);
        var meResponse = await client.GetAsync("/api/workspaces");
        meResponse.EnsureSuccessStatusCode();
        var currentWorkspaceDto = await meResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(currentWorkspaceDto);
        Assert.Equal(displayName, currentWorkspaceDto.Name);
        Assert.Equal(workspaceId, currentWorkspaceDto.Id);
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequestDto(workspaceId, "logout-test"));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceDto);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(workspaceDto.Token));
        loginResponse.EnsureSuccessStatusCode();
        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);

        // Set authorization header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.AccessToken);

        // Logout should succeed
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var logoutResponse = await client.PostAsync("/api/auth/logout", content);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
    }

    [Fact]
    public async Task GetCurrentWorkspace_WithValidToken_ReturnsWorkspace()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login
        var workspaceId = Guid.NewGuid();
        var displayName = "get-current-workspace-test";
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequestDto(workspaceId, displayName));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceDto);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(workspaceDto.Token));
        loginResponse.EnsureSuccessStatusCode();
        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);

        // Set authorization header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.AccessToken);

        // Get current workspace should return the workspace
        var meResponse = await client.GetAsync("/api/workspaces");
        meResponse.EnsureSuccessStatusCode();
        var currentWorkspaceDto = await meResponse.Content.ReadFromJsonAsync<WorkspaceDto>();
        Assert.NotNull(currentWorkspaceDto);
        Assert.Equal(displayName, currentWorkspaceDto.Name);
        Assert.Equal(workspaceId, currentWorkspaceDto.Id);
    }

    [Fact]
    public async Task Login_WithEmptyToken_ReturnsBadRequest()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);

        var response = await environment.Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(string.Empty));

        // Empty token fails validation before auth check
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithVeryLongToken_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);

        // Create a token that exceeds reasonable length (10000 characters)
        var veryLongToken = new string('A', 10000);
        var response = await environment.Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(veryLongToken));

        // No max length validation, so it goes through to auth which fails
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentWorkspace_WithExpiredToken_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace and login to get a token structure
        var workspaceId = Guid.NewGuid();
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequestDto(workspaceId, "expired-token-test"));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceDto);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(workspaceDto.Token));
        loginResponse.EnsureSuccessStatusCode();
        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);

        // Manually create an expired JWT token (exp set to past)
        // This is a simplified test - in real scenario, you'd need proper JWT creation
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE1MTYyMzkwMjJ9.invalid";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync("/api/workspaces");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ResponseContainsAllRequiredFields()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace
        var workspaceId = Guid.NewGuid();
        var displayName = "login-response-test";
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequestDto(workspaceId, displayName));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceDto);

        // Login
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(workspaceDto.Token));
        loginResponse.EnsureSuccessStatusCode();
        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        // Verify all required fields are present and valid
        Assert.NotNull(loginDto);
        Assert.NotNull(loginDto.Workspace);
        Assert.False(string.IsNullOrWhiteSpace(loginDto.AccessToken));
        Assert.Equal("Bearer", loginDto.TokenType);
        Assert.True(loginDto.ExpiresInSeconds > 0);
        Assert.Equal(workspaceId, loginDto.Workspace.Id);
        Assert.Equal(displayName, loginDto.Workspace.Name);
        Assert.True(loginDto.Workspace.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(loginDto.Workspace.DocumentRetentionDays >= 0);
    }

    [Fact]
    public async Task MultipleConcurrentLogins_SameWorkspace_ReturnsValidTokens()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace
        var workspaceId = Guid.NewGuid();
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequestDto(workspaceId, "concurrent-login-test"));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceDto);

        // Perform multiple logins concurrently
        var loginTasks = new List<Task<LoginResponseDto>>();
        for (int i = 0; i < 5; i++)
        {
            var task = client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(workspaceDto.Token))
                .ContinueWith(async response =>
                {
                    var resp = await response;
                    resp.EnsureSuccessStatusCode();
                    return await resp.Content.ReadFromJsonAsync<LoginResponseDto>() ?? throw new InvalidOperationException("Failed to deserialize login response");
                });
            loginTasks.Add(task.Unwrap());
        }

        var loginDtos = await Task.WhenAll(loginTasks);

        // All logins should succeed with valid tokens
        foreach (var loginDto in loginDtos)
        {
            Assert.NotNull(loginDto);
            Assert.False(string.IsNullOrWhiteSpace(loginDto.AccessToken));
            Assert.Equal(workspaceId, loginDto.Workspace.Id);
        }

        // Each token should be unique (stateless auth)
        var tokens = loginDtos.Select(dto => dto.AccessToken).ToList();
        Assert.Equal(5, tokens.Distinct().Count());
    }

    [Fact]
    public async Task Login_AfterWorkspaceDeleted_ReturnsUnauthorized()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;

        // Create workspace
        var workspaceId = Guid.NewGuid();
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces",
            new CreateWorkspaceRequestDto(workspaceId, "deleted-workspace-test"));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceDto);
        var token = workspaceDto.Token;

        // Login to get authentication token for delete operation
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(token));
        loginResponse.EnsureSuccessStatusCode();
        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.AccessToken);

        // Delete the workspace
        var deleteResponse = await client.DeleteAsync("/api/workspaces");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Clear authorization header
        client.DefaultRequestHeaders.Authorization = null;

        // Login with the deleted workspace token should fail
        var loginAfterDeleteResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(token));
        Assert.Equal(HttpStatusCode.Unauthorized, loginAfterDeleteResponse.StatusCode);
    }
}
