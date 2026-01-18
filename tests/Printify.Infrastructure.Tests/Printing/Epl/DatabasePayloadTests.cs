using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Printify.Infrastructure.Mapping.Epl;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Persistence.Entities.Documents;
using Printify.Infrastructure.Persistence.Entities.Documents.Epl;
using Printify.Infrastructure.Printing.Epl.Commands;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Printify.Infrastructure.Tests.Printing.Epl;

/// <summary>
/// Test to verify what's actually stored in the database Payload column.
/// This bypasses the repository to directly inspect the database.
/// </summary>
public sealed class DatabasePayloadTests
{
    [Fact]
    public async Task SaveToDatabase_VerifyPayloadColumn_HasTextBytesHex()
    {
        // Arrange
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        // Create in-memory SQLite database
        var connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PrintifyDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new PrintifyDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var documentId = Guid.NewGuid();
        var textBytes = Encoding.GetEncoding(437).GetBytes("Hello");

        // Create domain command
        var command = new ScalableText(10, 20, 0, 2, 1, 1, 'N', textBytes)
        {
            LengthInBytes = 25
        };

        // Convert to payload
        var payload = EplDocumentElementMapper.ToCommandPayload(command);
        var textPayload = Assert.IsType<ScalableTextElementPayload>(payload);

        Console.WriteLine($"[Step 1] Original TextBytesHex: '{textPayload.TextBytesHex}'");
        Assert.Equal("48656C6C6F", textPayload.TextBytesHex);

        // Serialize to JSON
        var json = JsonSerializer.Serialize(textPayload, serializerOptions);
        Console.WriteLine($"[Step 2] Serialized JSON: {json}");
        Assert.Contains("textBytesHex", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("48656C6C6F", json);

        // Create entity directly (since ToEntity is internal)
        var entity = new DocumentElementEntity
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Sequence = 0,
            ElementType = "eplScalableText",
            Payload = json,
            CommandRaw = string.Empty,
            LengthInBytes = 25
        };

        Console.WriteLine($"[Step 3] Entity.Payload: {entity.Payload}");
        Assert.Contains("textBytesHex", entity.Payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("48656C6C6F", entity.Payload);

        // Create a document entity to hold our element (required for FK constraint)
        var documentEntity = new DocumentEntity
        {
            Id = documentId,
            PrintJobId = Guid.NewGuid(),
            PrinterId = Guid.NewGuid(),
            Version = 1,
            CreatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Protocol = "Epl",
            BytesReceived = 25,
            BytesSent = 0,
            Elements = new List<DocumentElementEntity> { entity }
        };

        // Save to database
        context.Documents.Add(documentEntity);
        await context.SaveChangesAsync();

        Console.WriteLine($"[Step 4] Saved to database, Entity ID: {entity.Id}");

        // Query fresh from database
        var freshDocument = await context.Documents
            .AsNoTracking()
            .Include(d => d.Elements)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        Assert.NotNull(freshDocument);
        var freshEntity = Assert.Single(freshDocument.Elements);

        Console.WriteLine($"[Step 5] Fresh query, Payload: {freshEntity.Payload}");
        Assert.Contains("textBytesHex", freshEntity.Payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("48656C6C6F", freshEntity.Payload);

        // Deserialize from database payload
        var deserializedPayload = JsonSerializer.Deserialize<ScalableTextElementPayload>(freshEntity.Payload, serializerOptions);
        Assert.NotNull(deserializedPayload);
        Console.WriteLine($"[Step 6] Deserialized TextBytesHex: '{deserializedPayload.TextBytesHex}'");
        Assert.Equal("48656C6C6F", deserializedPayload.TextBytesHex);

        // Convert back to domain
        var roundtripCommand = EplDocumentElementMapper.ToDomain(deserializedPayload);
        var roundtripTextCommand = Assert.IsType<ScalableText>(roundtripCommand);

        Console.WriteLine($"[Step 7] Roundtrip TextBytes (hex): '{Convert.ToHexString(roundtripTextCommand.TextBytes)}'");
        Assert.Equal(textBytes, roundtripTextCommand.TextBytes);
    }
}
