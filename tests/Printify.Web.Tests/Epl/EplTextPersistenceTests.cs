using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;
using Printify.Domain.Layout;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.TestServices;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Auth.Requests;
using System.Net.Http.Json;
using System.Text;

namespace Printify.Web.Tests.Epl;

/// <summary>
/// Minimal test to debug EPL text persistence issue.
/// Tests the full cycle: parse -> save to DB -> read from DB -> render
/// </summary>
public sealed class EplTextPersistenceTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task EplText_SavedAndRetrieved_HasCorrectText()
    {
        // Arrange
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        await AuthenticateAsync(environment);

        var printerId = Guid.NewGuid();

        // Create a simple EPL document with text
        var textBytes = Encoding.GetEncoding(437).GetBytes("Hello");
        var command = new ScalableText(10, 20, 0, 2, 1, 1, 'N', textBytes)
        {
            LengthInBytes = 25
        };

        var document = new Document(
            Id: Guid.NewGuid(),
            PrintJobId: Guid.NewGuid(),
            PrinterId: printerId,
            Timestamp: DateTimeOffset.UtcNow,
            Protocol: Protocol.Epl,
            ClientAddress: null,
            BytesReceived: 25,
            BytesSent: 0,
            Commands: [command],
            ErrorMessages: null);

        // Act - Save document to database
        Console.WriteLine($"[DEBUG] Before save - TextBytesHex: '{Convert.ToHexString(command.TextBytes)}'");

        var scope = environment.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        await repository.AddAsync(document, CancellationToken.None);
        Console.WriteLine($"[DEBUG] Document saved to database with ID: {document.Id}");

        // Act - Retrieve document from database
        var retrievedDocument = await repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.NotNull(retrievedDocument);
        Console.WriteLine($"[DEBUG] Document retrieved from database");

        var retrievedCommand = Assert.Single(retrievedDocument.Commands);
        var retrievedTextCommand = Assert.IsType<ScalableText>(retrievedCommand);

        Console.WriteLine($"[DEBUG] After retrieval - TextBytes: '{Convert.ToHexString(retrievedTextCommand.TextBytes)}'");
        Console.WriteLine($"[DEBUG] After retrieval - TextBytes length: {retrievedTextCommand.TextBytes.Length}");

        var decodedText = Encoding.GetEncoding(437).GetString(retrievedTextCommand.TextBytes);
        Console.WriteLine($"[DEBUG] After retrieval - Decoded text: '{decodedText}'");

        // Assert
        Assert.Equal(textBytes, retrievedTextCommand.TextBytes);
        Assert.Equal("Hello", decodedText);
    }

    [Fact]
    public async Task EplText_SavedAndRetrieved_RendersCorrectly()
    {
        // Arrange
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        await AuthenticateAsync(environment);

        var printerId = Guid.NewGuid();

        // Create a simple EPL document with text
        var textBytes = Encoding.GetEncoding(437).GetBytes("Hello");
        var command = new ScalableText(10, 20, 0, 2, 1, 1, 'N', textBytes)
        {
            LengthInBytes = 25
        };

        var document = new Document(
            Id: Guid.NewGuid(),
            PrintJobId: Guid.NewGuid(),
            PrinterId: printerId,
            Timestamp: DateTimeOffset.UtcNow,
            Protocol: Protocol.Epl,
            ClientAddress: null,
            BytesReceived: 25,
            BytesSent: 0,
            Commands: [command],
            ErrorMessages: null);

        var scope = environment.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

        // Act - Save and retrieve
        await repository.AddAsync(document, CancellationToken.None);

        var retrievedDocument = await repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.NotNull(retrievedDocument);

        // Act - Render the retrieved document
        var rendererFactory = scope.ServiceProvider.GetRequiredService<IRendererFactory>();
        var renderer = rendererFactory.GetRenderer(Protocol.Epl);
        var canvas = renderer.Render(retrievedDocument);

        // Assert - Check canvas has text element with correct text
        var textElements = canvas.Items.OfType<TextElement>().ToList();
        var textElement = Assert.Single(textElements);

        Console.WriteLine($"[DEBUG] Canvas text element text: '{textElement.Text}'");

        Assert.Equal("Hello", textElement.Text);
    }

    private static async Task AuthenticateAsync(TestServiceContext.ControllerTestContext environment)
    {
        var client = environment.Client;
        var workspaceId = Guid.NewGuid();
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, "Test"));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceResponseDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<Printify.Web.Contracts.Workspaces.Responses.WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponseDto);
        var token = workspaceResponseDto.Token;

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(token));
        loginResponse.EnsureSuccessStatusCode();
        var loginResponseDto = await loginResponse.Content.ReadFromJsonAsync<Printify.Web.Contracts.Auth.Responses.LoginResponseDto>();
        Assert.NotNull(loginResponseDto);
        var accessToken = loginResponseDto.AccessToken;

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }
}
