using System;
using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts.Printers;
using Printify.Contracts.Services;
using Printify.Contracts.Users;
using Printify.TestServices;

namespace Printify.Documents.Tests;

public sealed class PrintersTests
{
    [Fact]
    public async Task UpdatePrinterAsync_WhenPrinterExists_RefreshesMutableFields()
    {
        await using var context = TestServiceContext.Create();
        // Resolve command and query services so we test the full stack.
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();
        var sessionService = context.Provider.GetRequiredService<ISessionService>();

        // Create a user and printer to update.
        var ownerId = await commandService.CreateUserAsync(new SaveUserRequest("Owner", "127.0.0.1"));
        var session = await sessionService.CreateAsync("127.0.0.1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));
        var printerId = await commandService.CreatePrinterAsync(new SavePrinterRequest(ownerId, session.Id, "Front", "escpos", 384, null, "127.0.0.1"));

        var updated = await commandService.UpdatePrinterAsync(printerId, new SavePrinterRequest(ownerId, session.Id, "Front Wide", "escpos", 512, 800, "127.0.0.2"));

        Assert.True(updated);
        var stored = await queryService.GetPrinterAsync(printerId);
        Assert.NotNull(stored);
        Assert.Equal("Front Wide", stored!.DisplayName);
        Assert.Equal(512, stored.WidthInDots);
        Assert.Equal(800, stored.HeightInDots);
        Assert.Equal("127.0.0.2", stored.CreatedFromIp);
    }

    [Fact]
    public async Task UpdatePrinterAsync_WhenPrinterMissing_ReturnsFalse()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var sessionService = context.Provider.GetRequiredService<ISessionService>();
        var ownerId = await commandService.CreateUserAsync(new SaveUserRequest("Owner", "127.0.0.1"));
        var session = await sessionService.CreateAsync("127.0.0.1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));

        // Use a bogus identifier to ensure the call short-circuits.
        var updated = await commandService.UpdatePrinterAsync(4096, new SavePrinterRequest(ownerId, session.Id, "Ghost", "escpos", 384, null, "127.0.0.1"));

        Assert.False(updated);
    }

    [Fact]
    public async Task DeletePrinterAsync_RemovesPrinter()
    {
        await using var context = TestServiceContext.Create();
        var commandService = context.Provider.GetRequiredService<IResourceCommandService>();
        var queryService = context.Provider.GetRequiredService<IResourceQueryService>();
        var sessionService = context.Provider.GetRequiredService<ISessionService>();

        var ownerId = await commandService.CreateUserAsync(new SaveUserRequest("Owner", "127.0.0.1"));
        // Seed a printer so the delete call exercises the storage removal path.
        var session = await sessionService.CreateAsync("127.0.0.1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));
        var printerId = await commandService.CreatePrinterAsync(new SavePrinterRequest(ownerId, session.Id, "Front", "escpos", 384, null, "127.0.0.1"));

        var deleted = await commandService.DeletePrinterAsync(printerId);

        Assert.True(deleted);
        var stored = await queryService.GetPrinterAsync(printerId);
        Assert.Null(stored);
    }
}
