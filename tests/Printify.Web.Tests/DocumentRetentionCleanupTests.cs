using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Printify.Domain.Config;
using Printify.Domain.Workspaces;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Persistence.Entities.Documents;
using Printify.Infrastructure.Persistence.Entities.Printers;
using Printify.Infrastructure.Persistence.Entities.Workspaces;
using Printify.Infrastructure.Retention;
using Printify.TestServices;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;

namespace Printify.Web.Tests;

public sealed class DocumentRetentionCleanupTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory = factory;

    [Fact]
    public async Task RunOnceAsync_DeletesExpiredDocuments_AndOnlyUnreferencedMedia()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var now = DateTimeOffset.UtcNow;
        var retainedWorkspaceId = Guid.NewGuid();
        var foreverWorkspaceId = Guid.NewGuid();
        var retainedPrinterId = Guid.NewGuid();
        var foreverPrinterId = Guid.NewGuid();
        var expiredDocumentId = Guid.NewGuid();
        var activeDocumentId = Guid.NewGuid();
        var foreverDocumentId = Guid.NewGuid();
        var sharedMediaId = Guid.NewGuid();
        var expiredOnlyMediaId = Guid.NewGuid();
        var foreverMediaId = Guid.NewGuid();

        string sharedMediaPath;
        string expiredOnlyMediaPath;
        string foreverMediaPath;

        await using (var scope = environment.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IOptions<Storage>>().Value;

            sharedMediaPath = CreateMediaFile(storage.MediaRootPath, sharedMediaId);
            expiredOnlyMediaPath = CreateMediaFile(storage.MediaRootPath, expiredOnlyMediaId);
            foreverMediaPath = CreateMediaFile(storage.MediaRootPath, foreverMediaId);

            dbContext.Workspaces.AddRange(
                CreateWorkspace(retainedWorkspaceId, "retained", documentRetentionDays: 1),
                CreateWorkspace(foreverWorkspaceId, "forever", documentRetentionDays: 0));
            dbContext.Printers.AddRange(
                CreatePrinter(retainedPrinterId, retainedWorkspaceId, port: 45101),
                CreatePrinter(foreverPrinterId, foreverWorkspaceId, port: 45102));
            dbContext.DocumentMedia.AddRange(
                CreateMedia(sharedMediaId, retainedWorkspaceId, sharedMediaPath),
                CreateMedia(expiredOnlyMediaId, retainedWorkspaceId, expiredOnlyMediaPath),
                CreateMedia(foreverMediaId, foreverWorkspaceId, foreverMediaPath));
            dbContext.Documents.AddRange(
                CreateDocument(expiredDocumentId, retainedPrinterId, now.AddDays(-2)),
                CreateDocument(activeDocumentId, retainedPrinterId, now),
                CreateDocument(foreverDocumentId, foreverPrinterId, now.AddDays(-365)));
            dbContext.Set<DocumentElementEntity>().AddRange(
                CreateElement(expiredDocumentId, sequence: 0, sharedMediaId),
                CreateElement(expiredDocumentId, sequence: 1, expiredOnlyMediaId),
                CreateElement(activeDocumentId, sequence: 0, sharedMediaId),
                CreateElement(foreverDocumentId, sequence: 0, foreverMediaId));

            await dbContext.SaveChangesAsync();
        }

        await using (var scope = environment.CreateScope())
        {
            var cleanup = scope.ServiceProvider.GetRequiredService<DocumentRetentionCleanupService>();

            var summary = await cleanup.GetSummaryAsync(now, retainedWorkspaceId, CancellationToken.None);
            Assert.Equal(1, summary.ExpiredDocuments);
            Assert.Equal(1, summary.RetentionMediaFiles);

            var result = await cleanup.RunOnceAsync(now, CancellationToken.None);

            Assert.Equal(1, result.DeletedDocuments);
            Assert.Equal(1, result.DeletedMedia);
        }

        await using (var scope = environment.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();

            Assert.Null(await dbContext.Documents.FindAsync(expiredDocumentId));
            Assert.NotNull(await dbContext.Documents.FindAsync(activeDocumentId));
            Assert.NotNull(await dbContext.Documents.FindAsync(foreverDocumentId));

            var retainedPrinter = await dbContext.Printers.FindAsync(retainedPrinterId);
            Assert.NotNull(retainedPrinter);
            Assert.Equal(now.ToUnixTimeMilliseconds(), retainedPrinter.LastDocumentReceivedAt?.ToUnixTimeMilliseconds());

            Assert.NotNull(await dbContext.DocumentMedia.FindAsync(sharedMediaId));
            Assert.Null(await dbContext.DocumentMedia.FindAsync(expiredOnlyMediaId));
            Assert.NotNull(await dbContext.DocumentMedia.FindAsync(foreverMediaId));
        }

        Assert.True(File.Exists(ToFullMediaPath(environment, sharedMediaPath)));
        Assert.False(File.Exists(ToFullMediaPath(environment, expiredOnlyMediaPath)));
        Assert.True(File.Exists(ToFullMediaPath(environment, foreverMediaPath)));
    }

    [Fact]
    public async Task RunOnceAsync_WithMaxDocuments_DeletesOnlyRequestedDocuments()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var now = DateTimeOffset.UtcNow;
        var workspaceId = Guid.NewGuid();
        var printerId = Guid.NewGuid();
        var firstExpiredDocumentId = Guid.NewGuid();
        var secondExpiredDocumentId = Guid.NewGuid();

        await using (var scope = environment.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();

            dbContext.Workspaces.Add(CreateWorkspace(workspaceId, "limited", documentRetentionDays: 1));
            dbContext.Printers.Add(CreatePrinter(printerId, workspaceId, port: 45103));
            dbContext.Documents.AddRange(
                CreateDocument(firstExpiredDocumentId, printerId, now.AddDays(-3)),
                CreateDocument(secondExpiredDocumentId, printerId, now.AddDays(-2)));

            await dbContext.SaveChangesAsync();
        }

        await using (var scope = environment.CreateScope())
        {
            var cleanup = scope.ServiceProvider.GetRequiredService<DocumentRetentionCleanupService>();

            var result = await cleanup.RunOnceAsync(
                now,
                workspaceId,
                maxDocuments: 1,
                cancellationToken: CancellationToken.None);

            Assert.Equal(1, result.DeletedDocuments);
            Assert.Equal(0, result.DeletedMedia);
        }

        await using (var scope = environment.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();

            Assert.Null(await dbContext.Documents.FindAsync(firstExpiredDocumentId));
            Assert.NotNull(await dbContext.Documents.FindAsync(secondExpiredDocumentId));
        }
    }

    [Fact]
    public async Task RetentionCleanupEndpoints_ForNormalWorkspace_ReturnForbidden()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var summaryResponse = await environment.Client.GetAsync("/api/workspaces/retention/cleanup-summary");
        var runResponse = await environment.Client.PostAsJsonAsync(
            "/api/workspaces/retention/cleanup",
            new RunDocumentRetentionCleanupRequestDto(10));

        Assert.Equal(HttpStatusCode.Forbidden, summaryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, runResponse.StatusCode);
    }

    [Fact]
    public async Task RunRetentionCleanupEndpoint_ForAdminWorkspace_DeletesUnreferencedMediaFile()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var now = DateTimeOffset.UtcNow;
        var (workspaceId, _) = await AuthHelper.CreateWorkspaceAndLoginReturningToken(environment);
        var printerId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        string mediaPath;

        await using (var scope = environment.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IOptions<Storage>>().Value;
            var workspace = await dbContext.Workspaces.FindAsync(workspaceId);
            Assert.NotNull(workspace);

            // Only admins can run manual retention; use one-day retention so the seeded document is expired.
            workspace.Role = WorkspaceRole.Admin.ToString();
            workspace.DocumentRetentionDays = 1;

            mediaPath = CreateMediaFile(storage.MediaRootPath, mediaId);
            dbContext.Printers.Add(CreatePrinter(printerId, workspaceId, port: 45104));
            dbContext.DocumentMedia.Add(CreateMedia(mediaId, workspaceId, mediaPath));
            dbContext.Documents.Add(CreateDocument(documentId, printerId, now.AddDays(-2)));
            dbContext.Set<DocumentElementEntity>().Add(CreateElement(documentId, sequence: 0, mediaId));

            await dbContext.SaveChangesAsync();
        }

        var summaryResponse = await environment.Client.GetAsync("/api/workspaces/retention/cleanup-summary");
        summaryResponse.EnsureSuccessStatusCode();
        var summary = await summaryResponse.Content.ReadFromJsonAsync<DocumentRetentionCleanupSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(1, summary.ExpiredDocuments);
        Assert.Equal(1, summary.RetentionMediaFiles);

        var runResponse = await environment.Client.PostAsJsonAsync(
            "/api/workspaces/retention/cleanup",
            new RunDocumentRetentionCleanupRequestDto(10));
        runResponse.EnsureSuccessStatusCode();
        var result = await runResponse.Content.ReadFromJsonAsync<DocumentRetentionCleanupResultDto>();
        Assert.NotNull(result);
        Assert.Equal(1, result.DeletedDocuments);
        Assert.Equal(1, result.DeletedMedia);

        await using (var scope = environment.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();

            Assert.Null(await dbContext.Documents.FindAsync(documentId));
            Assert.Null(await dbContext.DocumentMedia.FindAsync(mediaId));
            Assert.Empty(dbContext.Set<DocumentElementEntity>().Where(element => element.DocumentId == documentId));
        }

        Assert.False(File.Exists(ToFullMediaPath(environment, mediaPath)));
    }

    private static WorkspaceEntity CreateWorkspace(Guid id, string name, int documentRetentionDays)
    {
        return new WorkspaceEntity
        {
            Id = id,
            Name = name,
            Token = $"{name}-token",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedFromIp = "127.0.0.1",
            Role = "Normal",
            DocumentRetentionDays = documentRetentionDays
        };
    }

    private static PrinterEntity CreatePrinter(Guid id, Guid workspaceId, int port)
    {
        return new PrinterEntity
        {
            Id = id,
            OwnerWorkspaceId = workspaceId,
            DisplayName = id.ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedFromIp = "127.0.0.1",
            Settings = new PrinterSettingsEntity
            {
                Protocol = "EscPos",
                WidthInDots = 512,
                ListenTcpPortNumber = port
            }
        };
    }

    private static DocumentEntity CreateDocument(Guid id, Guid printerId, DateTimeOffset createdAt)
    {
        return new DocumentEntity
        {
            Id = id,
            PrintJobId = Guid.NewGuid(),
            PrinterId = printerId,
            Version = 1,
            CreatedAt = createdAt,
            Protocol = "EscPos",
            WidthInDots = 512
        };
    }

    private static DocumentElementEntity CreateElement(Guid documentId, int sequence, Guid mediaId)
    {
        return new DocumentElementEntity
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Sequence = sequence,
            ElementType = "RasterImage",
            Payload = "{}",
            CommandRaw = string.Empty,
            MediaId = mediaId
        };
    }

    private static DocumentMediaEntity CreateMedia(Guid id, Guid workspaceId, string fileName)
    {
        return new DocumentMediaEntity
        {
            Id = id,
            OwnerWorkspaceId = workspaceId,
            CreatedAt = DateTimeOffset.UtcNow,
            ContentType = "image/png",
            Length = 4,
            Checksum = id.ToString("N"),
            FileName = fileName,
            Url = $"/api/media/{id:D}"
        };
    }

    private static string CreateMediaFile(string rootPath, Guid mediaId)
    {
        var fileName = Path.Combine(mediaId.ToString("N")[..2], mediaId.ToString("N") + ".png");
        var fullPath = Path.Combine(rootPath, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, [1, 2, 3, 4]);
        return fileName;
    }

    private static string ToFullMediaPath(
        TestServiceContext.ControllerTestContext environment,
        string fileName)
    {
        using var scope = environment.Factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IOptions<Storage>>().Value;
        return Path.Combine(storage.MediaRootPath, fileName);
    }
}
