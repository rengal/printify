using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Printify.Domain.Workspaces;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Persistence.Entities.Documents;
using Printify.Infrastructure.Persistence.Entities.Printers;
using Printify.TestServices;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;

namespace Printify.Web.Tests;

public sealed class AdminWorkspaceStatisticsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory = factory;

    [Fact]
    public async Task GetAdminStatistics_ForNormalWorkspace_ReturnsForbidden()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);

        await AuthHelper.CreateWorkspaceAndLogin(environment);

        var response = await environment.Client.GetAsync("/api/workspaces/admin-statistics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminStatistics_ForAdminWorkspace_ReturnsGlobalStatistics()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        var client = environment.Client;
        var now = DateTimeOffset.UtcNow;
        var admin = await CreateWorkspaceAsync(client, "admin-workspace");
        var firstWorkspace = await CreateWorkspaceAsync(client, "first-workspace");
        var secondWorkspace = await CreateWorkspaceAsync(client, "second-workspace");

        await using (var scope = environment.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
            var adminEntity = await dbContext.Workspaces.FindAsync(admin.Id);
            Assert.NotNull(adminEntity);
            adminEntity.Role = WorkspaceRole.Admin.ToString();

            SeedWorkspaceData(dbContext, firstWorkspace.Id, now, printerPort: 45201, documentCount: 2, mediaCount: 2);
            SeedWorkspaceData(dbContext, secondWorkspace.Id, now.AddDays(-8), printerPort: 45202, documentCount: 1, mediaCount: 1);

            await dbContext.SaveChangesAsync();
        }

        await AuthHelper.Login(client, admin.Token);

        var response = await client.GetAsync("/api/workspaces/admin-statistics");
        response.EnsureSuccessStatusCode();
        var statistics = await response.Content.ReadFromJsonAsync<AdminWorkspaceStatisticsDto>();
        Assert.NotNull(statistics);

        Assert.Equal(3, statistics.TotalWorkspaces);
        Assert.Equal(2, statistics.TotalPrinters);
        Assert.Equal(3, statistics.TotalDocuments);
        Assert.Equal(3, statistics.TotalMedia);
        Assert.Equal(600, statistics.TotalMediaBytes);
        Assert.Equal(2, statistics.DocumentsLast24h);
        Assert.Equal(2, statistics.DocumentsLast7d);
        Assert.Equal(2, statistics.MediaLast24h);
        Assert.Equal(2, statistics.MediaLast7d);
        Assert.Equal(1, statistics.ActiveWorkspacesLast24h);
        Assert.Equal(1, statistics.ActiveWorkspacesLast7d);
        Assert.Contains(statistics.Workspaces, row =>
            row.WorkspaceId == firstWorkspace.Id
            && row.DocumentCount == 2
            && row.MediaCount == 2
            && row.MediaBytes == 400);
    }

    private static async Task<WorkspaceResponseDto> CreateWorkspaceAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto(Guid.NewGuid(), name));
        response.EnsureSuccessStatusCode();
        var workspace = await response.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspace);
        return workspace;
    }

    private static void SeedWorkspaceData(
        PrintifyDbContext dbContext,
        Guid workspaceId,
        DateTimeOffset documentTimestamp,
        int printerPort,
        int documentCount,
        int mediaCount)
    {
        var printerId = Guid.NewGuid();
        dbContext.Printers.Add(new PrinterEntity
        {
            Id = printerId,
            OwnerWorkspaceId = workspaceId,
            DisplayName = $"printer-{printerPort}",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedFromIp = "127.0.0.1",
            LastDocumentReceivedAt = documentTimestamp,
            Settings = new PrinterSettingsEntity
            {
                Protocol = "EscPos",
                WidthInDots = 512,
                ListenTcpPortNumber = printerPort
            }
        });

        for (var i = 0; i < documentCount; i++)
        {
            var documentId = Guid.NewGuid();
            dbContext.Documents.Add(new DocumentEntity
            {
                Id = documentId,
                PrintJobId = Guid.NewGuid(),
                PrinterId = printerId,
                Version = 1,
                CreatedAt = documentTimestamp,
                Protocol = "EscPos",
                WidthInDots = 512
            });
        }

        for (var i = 0; i < mediaCount; i++)
        {
            dbContext.DocumentMedia.Add(new DocumentMediaEntity
            {
                Id = Guid.NewGuid(),
                OwnerWorkspaceId = workspaceId,
                CreatedAt = documentTimestamp,
                ContentType = "image/png",
                Length = 200,
                Checksum = Guid.NewGuid().ToString("N"),
                FileName = $"{Guid.NewGuid():N}.png",
                Url = $"/api/media/{Guid.NewGuid():D}"
            });
        }
    }
}
