using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts.Services;
using Printify.Contracts.Users;
using Printify.TestServices;

namespace Printify.Documents.Tests;

public sealed class UsersTests
{
    [Fact]
    public async Task UpdateUserAsync_WhenUserExists_RefreshesMutableFields()
    {
        await using var context = TestServiceContext.Create();
        // Resolve command and query services so we validate both write and read stacks.
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();

        // Seed a user to exercise the update path.
        var userId = await commandService.CreateUserAsync(new SaveUserRequest("Initial", "192.168.0.1"));

        // Act: perform the update with new mutable values.
        var updated = await commandService.UpdateUserAsync(userId, new SaveUserRequest("Updated", "192.168.0.2"));

        Assert.True(updated);
        var stored = await queryService.GetUserAsync(userId);
        Assert.NotNull(stored);
        Assert.Equal("Updated", stored!.DisplayName);
        Assert.Equal("192.168.0.2", stored.CreatedFromIp);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenUserMissing_ReturnsFalse()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();

        // Verify missing identifiers do not create records implicitly.
        var updated = await commandService.UpdateUserAsync(1987, new SaveUserRequest("Ghost", "10.0.0.1"));

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesUser()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();

        // Create a user so the delete call exercises the positive case.
        var userId = await commandService.CreateUserAsync(new SaveUserRequest("Removable", "10.1.1.1"));

        var deleted = await commandService.DeleteUserAsync(userId);

        Assert.True(deleted);
        var stored = await queryService.GetUserAsync(userId);
        Assert.Null(stored);
    }
}