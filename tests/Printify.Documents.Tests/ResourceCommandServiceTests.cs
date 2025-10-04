using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;
using Printify.Contracts.Users;
using Printify.TestServices;

namespace Printify.Documents.Tests;

public sealed class ResourceCommandServiceTests
{
    [Fact]
    public async Task UpdateUserAsync_ChangesDisplayName()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();

        var userId = await commandService.CreateUserAsync(new SaveUserRequest("Alice", "10.0.0.1"));
        var updated = await commandService.UpdateUserAsync(userId, new SaveUserRequest("Alice Updated", "10.0.0.2"));

        Assert.True(updated);
        var user = await queryService.GetUserAsync(userId);
        Assert.NotNull(user);
        Assert.Equal("Alice Updated", user!.DisplayName);
        Assert.Equal("10.0.0.2", user.CreatedFromIp);
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesUser()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();

        var userId = await commandService.CreateUserAsync(new SaveUserRequest("Bob", "10.0.0.1"));
        var deleted = await commandService.DeleteUserAsync(userId);

        Assert.True(deleted);
        var user = await queryService.GetUserAsync(userId);
        Assert.Null(user);
        Assert.False(await commandService.DeleteUserAsync(userId));
    }

    [Fact]
    public async Task UpdatePrinterAsync_ChangesDimensions()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();

        var ownerId = await commandService.CreateUserAsync(new SaveUserRequest("Owner", "127.0.0.1"));
        var printerId = await commandService.CreatePrinterAsync(new SavePrinterRequest(ownerId, "Front", "escpos", 384, null, "127.0.0.1"));

        var updated = await commandService.UpdatePrinterAsync(printerId, new SavePrinterRequest(ownerId, "Front - Wide", "escpos", 512, 800, "127.0.0.2"));
        Assert.True(updated);

        var printer = await queryService.GetPrinterAsync(printerId);
        Assert.NotNull(printer);
        Assert.Equal("Front - Wide", printer!.DisplayName);
        Assert.Equal(512, printer.WidthInDots);
        Assert.Equal(800, printer.HeightInDots);
        Assert.Equal("127.0.0.2", printer.CreatedFromIp);
    }

    [Fact]
    public async Task DeletePrinterAsync_RemovesPrinter()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();

        var ownerId = await commandService.CreateUserAsync(new SaveUserRequest("Owner", "10.0.0.1"));
        var printerId = await commandService.CreatePrinterAsync(new SavePrinterRequest(ownerId, "Back", "escpos", 384, null, "10.0.0.1"));

        var deleted = await commandService.DeletePrinterAsync(printerId);
        Assert.True(deleted);
        var printer = await queryService.GetPrinterAsync(printerId);
        Assert.Null(printer);
        Assert.False(await commandService.DeletePrinterAsync(printerId));
    }
}
