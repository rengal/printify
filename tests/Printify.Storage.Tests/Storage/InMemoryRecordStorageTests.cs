using Printify.Contracts.Documents.Elements;

namespace Printify.Storage.Tests.Storage;

using System;
using System.Threading.Tasks;
using Contracts;
using Contracts.Documents;
using Printify.Contracts.Printers;
using TestServices;

public sealed class InMemoryRecordStorageTests
{
    /// Scenario: Adding consecutive documents should assign incremental identifiers and allow round-tripping via GetDocumentAsync.
    /// </summary>
    [Fact]
    public async Task AddDocumentAsync_AssignsSequentialIdentifiers()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;
        var firstDocument = CreateDocument(DateTimeOffset.UnixEpoch.AddMinutes(1));
        var secondDocument = CreateDocument(DateTimeOffset.UnixEpoch.AddMinutes(2));

        var firstId = await storage.AddDocumentAsync(firstDocument);
        var secondId = await storage.AddDocumentAsync(secondDocument);

        Assert.Equal(1, firstId);
        Assert.Equal(2, secondId);

        var firstRoundTrip = await storage.GetDocumentAsync(firstId);
        var secondRoundTrip = await storage.GetDocumentAsync(secondId);

        Assert.NotNull(firstRoundTrip);
        Assert.NotNull(secondRoundTrip);
        Assert.Equal(firstId, firstRoundTrip.Id);
        Assert.Equal(secondId, secondRoundTrip.Id);
        Assert.Equal(firstDocument.Elements.Count, firstRoundTrip.Elements.Count);
        Assert.Equal(secondDocument.Timestamp, secondRoundTrip.Timestamp);
    }

    /// Scenario: Listing with limit should return newest documents first and honor beforeId for a subsequent page.
    /// </summary>
    [Fact]
    public async Task ListDocumentsAsync_ReturnsNewestFirstAndSupportsBeforeId()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;

        await storage.AddDocumentAsync(CreateDocument(DateTimeOffset.UnixEpoch.AddMinutes(1)));
        await storage.AddDocumentAsync(CreateDocument(DateTimeOffset.UnixEpoch.AddMinutes(2)));
        await storage.AddDocumentAsync(CreateDocument(DateTimeOffset.UnixEpoch.AddMinutes(3)));

        var firstPage = await storage.ListDocumentsAsync(2);
        var secondPage = await storage.ListDocumentsAsync(2, beforeId: firstPage.Last().Id);

        Assert.Collection(
            firstPage,
            d => Assert.Equal(3, d.Id),
            d => Assert.Equal(2, d.Id));

        Assert.Collection(
            secondPage,
            d => Assert.Equal(1, d.Id));
    }

    /// Scenario: Applying a source IP filter should restrict results to matching documents only.
    /// </summary>
    [Fact]
    public async Task ListDocumentsAsync_FiltersBySourceIp()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;

        await storage.AddDocumentAsync(CreateDocument(DateTimeOffset.UnixEpoch.AddMinutes(1), sourceIp: "10.0.0.1"));
        await storage.AddDocumentAsync(CreateDocument(DateTimeOffset.UnixEpoch.AddMinutes(2), sourceIp: "10.0.0.2"));
        await storage.AddDocumentAsync(CreateDocument(DateTimeOffset.UnixEpoch.AddMinutes(3), sourceIp: "10.0.0.1"));

        var filtered = await storage.ListDocumentsAsync(5, sourceIp: "10.0.0.1");

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, d => Assert.Equal("10.0.0.1", d.SourceIp));
    }

    /// Scenario: Looking up a user by name should return the stored user or null when missing.
    /// </summary>
    [Fact]
    public async Task GetUserByNameAsync_FindsExistingUser()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;

        var userId = await storage.AddUserAsync(new Contracts.Users.User(0, "Lookup", DateTimeOffset.UnixEpoch, "10.0.0.1"));

        var found = await storage.GetUserByNameAsync("Lookup");
        var missing = await storage.GetUserByNameAsync("Unknown");

        Assert.NotNull(found);
        Assert.Equal(userId, found!.Id);
        Assert.Null(missing);
    }

    /// Scenario: Listing users should return every persisted entry.
    /// </summary>
    [Fact]
    public async Task ListUsersAsync_ReturnsAllUsers()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;

        await storage.AddUserAsync(new Contracts.Users.User(0, "First", DateTimeOffset.UnixEpoch, "10.0.0.1"));
        await storage.AddUserAsync(new Contracts.Users.User(0, "Second", DateTimeOffset.UnixEpoch, "10.0.0.2"));

        var users = await storage.ListUsersAsync();

        Assert.Equal(2, users.Count);
        Assert.Contains(users, user => user.DisplayName == "First");
        Assert.Contains(users, user => user.DisplayName == "Second");
    }

    /// Scenario: Updating an existing user should overwrite mutable fields and preserve the identifier.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WhenUserExists_ReplacesStoredEntry()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;

        var userId = await storage.AddUserAsync(new Contracts.Users.User(0, "Initial", DateTimeOffset.UnixEpoch, "10.0.0.1"));
        var updatedUser = new Contracts.Users.User(userId, "Updated", DateTimeOffset.UnixEpoch, "10.0.0.2");

        var updated = await storage.UpdateUserAsync(updatedUser);

        Assert.True(updated);
        var reloaded = await storage.GetUserAsync(userId);
        Assert.NotNull(reloaded);
        Assert.Equal("Updated", reloaded!.DisplayName);
        Assert.Equal("10.0.0.2", reloaded.CreatedFromIp);
    }

    /// Scenario: Deleting a user should remove the record and return false if repeated.
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_RemovesExistingUser()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;

        var userId = await storage.AddUserAsync(new Contracts.Users.User(0, "ToRemove", DateTimeOffset.UnixEpoch, "10.0.0.1"));

        Assert.True(await storage.DeleteUserAsync(userId));
        Assert.Null(await storage.GetUserAsync(userId));
        Assert.False(await storage.DeleteUserAsync(userId));
    }

    /// Scenario: Listing printers without a filter should return every registered printer.
    /// </summary>
    [Fact]
    public async Task ListPrintersAsync_ReturnsAllWhenFilterOmitted()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;

        await storage.AddPrinterAsync(new Printer(0, 1, 100, "Front", "escpos", 384, null, DateTimeOffset.UnixEpoch, "10.0.0.1"));
        await storage.AddPrinterAsync(new Printer(0, 2, 101, "Back", "escpos", 384, null, DateTimeOffset.UnixEpoch, "10.0.0.2"));

        var printers = await storage.ListPrintersAsync();

        Assert.Equal(2, printers.Count);
        Assert.Contains(printers, printer => printer.OwnerUserId == 1);
        Assert.Contains(printers, printer => printer.OwnerUserId == 2);
    }

    /// Scenario: Supplying an owner filter should narrow the result set.
    /// </summary>
    [Fact]
    public async Task ListPrintersAsync_FiltersByOwner()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;

        await storage.AddPrinterAsync(new Printer(0, 5, 200, "Front", "escpos", 384, null, DateTimeOffset.UnixEpoch, "10.0.0.1"));
        await storage.AddPrinterAsync(new Printer(0, 5, 201, "Back", "escpos", 384, null, DateTimeOffset.UnixEpoch, "10.0.0.1"));
        await storage.AddPrinterAsync(new Printer(0, 7, 202, "Spare", "escpos", 384, null, DateTimeOffset.UnixEpoch, "10.0.0.2"));

        var printers = await storage.ListPrintersAsync(5);

        Assert.Equal(2, printers.Count);
        Assert.All(printers, printer => Assert.Equal(5, printer.OwnerUserId));
    }

    /// Scenario: Updating a printer should refresh configuration while keeping the same identifier.
    /// </summary>
    [Fact]
    public async Task UpdatePrinterAsync_WhenPrinterExists_ReplacesStoredEntry()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;

        var printerId = await storage.AddPrinterAsync(new Printer(0, 7, 300, "Front", "escpos", 384, null, DateTimeOffset.UnixEpoch, "10.0.0.1"));
        var updatedPrinter = new Printer(printerId, 7, 300, "Front Wide", "escpos", 512, 800, DateTimeOffset.UnixEpoch, "10.0.0.2");

        var updated = await storage.UpdatePrinterAsync(updatedPrinter);

        Assert.True(updated);
        var reloaded = await storage.GetPrinterAsync(printerId);
        Assert.NotNull(reloaded);
        Assert.Equal("Front Wide", reloaded!.DisplayName);
        Assert.Equal(512, reloaded.WidthInDots);
        Assert.Equal(800, reloaded.HeightInDots);
        Assert.Equal("10.0.0.2", reloaded.CreatedFromIp);
    }

    /// Scenario: Deleting a printer should remove it and subsequent deletes should return false.
    /// </summary>
    [Fact]
    public async Task DeletePrinterAsync_RemovesExistingPrinter()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;

        var printerId = await storage.AddPrinterAsync(new Printer(0, 9, 400, "Front", "escpos", 384, null, DateTimeOffset.UnixEpoch, "10.0.0.1"));

        Assert.True(await storage.DeletePrinterAsync(printerId));
        Assert.Null(await storage.GetPrinterAsync(printerId));
        Assert.False(await storage.DeletePrinterAsync(printerId));
    }

    /// <summary>
    /// Scenario: Sessions can be created, updated, and removed.
    /// </summary>
    [Fact]
    public async Task SessionCrud_Works()
    {
        await using var context = TestServiceContext.Create();
        var storage = context.RecordStorage;

        var now = DateTimeOffset.UnixEpoch;
        var sessionId = await storage.AddSessionAsync(new Contracts.Sessions.Session(0, now, now, "127.0.0.1", null, now.AddHours(1)));
        var session = await storage.GetSessionAsync(sessionId);
        Assert.NotNull(session);

        var updated = session! with { ClaimedUserId = 42, LastActiveAt = now.AddMinutes(5) };
        Assert.True(await storage.UpdateSessionAsync(updated));

        var fetched = await storage.GetSessionAsync(sessionId);
        Assert.NotNull(fetched);
        Assert.Equal(42, fetched!.ClaimedUserId);

        Assert.True(await storage.DeleteSessionAsync(sessionId));
        Assert.Null(await storage.GetSessionAsync(sessionId));
    }

    private static Document CreateDocument(DateTimeOffset timestamp, string? sourceIp = null, long printerId = 1)
    {
        return new Document(
            0,
            printerId,
            timestamp,
            Protocol.EscPos,
            sourceIp,
            [new TextLine(0, "Sample Text")]);
    }
}


