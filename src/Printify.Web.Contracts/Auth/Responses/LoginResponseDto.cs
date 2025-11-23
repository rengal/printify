using Printify.Web.Contracts.Workspaces.Responses;

namespace Printify.Web.Contracts.Auth.Responses;

public sealed record LoginResponseDto(
    string AccessToken,
    string TokenType,
    long ExpiresInSeconds,
    WorkspaceDto Workspace);
