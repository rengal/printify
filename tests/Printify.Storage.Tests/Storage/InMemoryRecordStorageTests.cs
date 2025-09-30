using Printify.Contracts.Documents.Elements;

namespace Printify.Storage.Tests.Storage;

using System;
using System.Threading.Tasks;
using Contracts;
using Contracts.Documents;
using TestServices;

public sealed class InMemoryRecordStorageTests
{
    /// <summary>
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

    /// <summary>
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

    /// <summary>
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

    private static Document CreateDocument(DateTimeOffset timestamp, string? sourceIp = null)
    {
        return new Document(
            0,
            timestamp,
            Protocol.EscPos,
            sourceIp,
            [new TextLine(0, "Sample Text")]);
    }
}
