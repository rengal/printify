using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Application.Printing.Events;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Printify.Tests.Shared.Document;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Documents.Responses;
using Printify.Web.Contracts.Documents.Responses.Elements;
using Printify.Web.Contracts.Documents.Responses.View;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;
using Bell = Printify.Domain.Documents.Elements.Bell;

namespace Printify.Web.Tests.EscPos;

public class EscPosTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    protected const byte Esc = 0x1B;
    protected const byte Gs = 0x1D;
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(1);
    private const int DefaultPrinterWidthInDots = 512;
    private static readonly int? DefaultPrinterHeightInDots = null;

    private enum CompletionMode
    {
        AdvanceIdleTimeout,
        CloseChannel
    }

    private static readonly IReadOnlyList<EscPosChunkStrategy> ChunkStrategies = EscPosChunkStrategies.All;

    [Fact]
    public async Task DocumentCompletesAfterIdleTimeout()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        await AuthenticateAsync(environment, "escpos-idle-user");

        var printerId = Guid.NewGuid();
        var channel = await CreatePrinterAsync(
            environment,
            printerId,
            "EscPos Idle Printer",
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);

        await using var streamEnumerator = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();
        var nextEventTask = streamEnumerator.MoveNextAsync().AsTask();

        var bytesSent = 0;
        channel.OnResponse(data => bytesSent += data.Length);

        await channel.SendToServerAsync(new byte[] { 0x07 }, CancellationToken.None);

        var clockFactory = Assert.IsType<TestClockFactory>(environment.ClockFactory);
        var elapsed = 0;
        const int stepMs = 50;
        while (elapsed + stepMs < PrinterConstants.ListenerIdleTimeoutMs)
        {
            clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(stepMs));
            await Task.Delay(1);
            Assert.False(nextEventTask.IsCompleted);
            elapsed += stepMs;
        }

        clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(stepMs));
        await nextEventTask.WaitAsync(TimeSpan.FromMilliseconds(500));

        var expectedElements = new List<Element> { new Bell {LengthInBytes = 1} };

        Assert.True(nextEventTask.Result);
        var documentEvent = streamEnumerator.Current;
        DocumentAssertions.Equal(
            expectedElements,
            Protocol.EscPos,
            documentEvent.Document,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(expectedElements.Sum(element => element.LengthInBytes), bytesSent, documentEvent.Document);
    }

    protected async Task RunScenarioAsync(EscPosScenario scenario)
    {
        System.Diagnostics.Debug.WriteLine($"Starting scenario [{scenario.Id}]");
        var strategy = EscPosChunkStrategies.SingleByte;
        System.Diagnostics.Debug.WriteLine($"   chunkStrategy={strategy.Name}");
        await RunScenarioAsync(scenario, $"escpos-strategy-{strategy.Name}", strategy);
        System.Diagnostics.Debug.WriteLine($"Completed scenario [{scenario.Id}]");
    }

    private async Task RunScenarioAsync(
        EscPosScenario scenario,
        string userPrefix,
        EscPosChunkStrategy strategy)
    {
        await RunScenarioAsync(scenario, userPrefix, strategy, CompletionMode.AdvanceIdleTimeout);
        await RunScenarioAsync(scenario, userPrefix, strategy, CompletionMode.CloseChannel);
    }

    private async Task RunScenarioAsync(
        EscPosScenario scenario,
        string userPrefix,
        EscPosChunkStrategy strategy,
        CompletionMode completionMode)
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        await AuthenticateAsync(environment, $"{userPrefix}-{completionMode}");

        var printerId = Guid.NewGuid();
        var channel =
            await CreatePrinterAsync(
                environment,
                printerId,
                $"EscPos Test Printer {userPrefix}-{completionMode}",
                DefaultPrinterWidthInDots,
                DefaultPrinterHeightInDots);

        var bytesSent = 0;
        channel.OnResponse(data => bytesSent += data.Length);

        var streamEnumerator = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();

        await SendWithChunkStrategyAsync(environment, channel, scenario.Input, strategy);

        switch (completionMode)
        {
            case CompletionMode.AdvanceIdleTimeout:
                AdvanceBeyondIdle(environment, PrinterConstants.ListenerIdleTimeoutMs + 50);
                break;
            case CompletionMode.CloseChannel:
                await channel.CloseAsync(ChannelClosedReason.Completed);
                break;
        }

        var result = await streamEnumerator.MoveNextAsync().AsTask().WaitAsync(IdleTimeout);
        Assert.True(result);

        var documentEvent = streamEnumerator.Current;

        DocumentAssertions.Equal(
            scenario.ExpectedPersistedElements ?? scenario.ExpectedRequestElements,
            Protocol.EscPos,
            documentEvent.Document,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(scenario.Input.Length, bytesSent, documentEvent.Document);

        // Request document via web call and verify
        var documentId = documentEvent.Document.Id;
        var response = await environment.Client.GetAsync($"/api/printers/{printerId}/documents/{documentId}");
        response.EnsureSuccessStatusCode();

        // Read response content as string
        var responseContent = await response.Content.ReadAsStringAsync();

        // Parse JSON with custom options
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var retrievedDocument = System.Text.Json.JsonSerializer.Deserialize<DocumentDto>(responseContent, options);

        Assert.NotNull(retrievedDocument);

        DocumentAssertions.Equal(
            scenario.ExpectedPersistedElements ?? scenario.ExpectedRequestElements,
            Protocol.EscPos,
            retrievedDocument,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(scenario.Input.Length, bytesSent, retrievedDocument);

        // Verify RasterImage elements have accessible Media URLs
        foreach (var element in retrievedDocument.Elements)
        {
            if (element is RasterImageDto rasterImage)
            {
                Assert.False(string.IsNullOrWhiteSpace(rasterImage.Media.Url));

                // Verify the media URL is accessible
                var mediaResponse = await environment.Client.GetAsync(rasterImage.Media.Url);
                mediaResponse.EnsureSuccessStatusCode();

                // Verify content type is an image
                var contentType = mediaResponse.Content.Headers.ContentType?.MediaType;
                Assert.NotNull(contentType);
                Assert.True(contentType.StartsWith("image/"),
                    $"Expected image content type, but got {contentType}");

                var responseLength = mediaResponse.Content.Headers.ContentLength;
                Assert.NotNull(responseLength);
                Assert.Equal(rasterImage.Media.Length, responseLength.Value);

                var payload = await mediaResponse.Content.ReadAsByteArrayAsync();
                Assert.Equal(responseLength.Value, payload.LongLength);
                Assert.Equal(rasterImage.Media.Length, payload.LongLength);

                // Verify SHA-256 checksum is valid
                if (!string.IsNullOrWhiteSpace(rasterImage.Media.Sha256))
                {
                    var computedChecksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
                    Assert.Equal(rasterImage.Media.Sha256, computedChecksum);

                    var eTag = mediaResponse.Headers.ETag?.Tag;
                    Assert.NotNull(eTag);
                    Assert.StartsWith("\"sha256:", eTag, StringComparison.OrdinalIgnoreCase);
                    var eTagValue = eTag.Trim('"');
                    var eTagChecksum = eTagValue.Split(':', 2).ElementAtOrDefault(1);
                    Assert.False(string.IsNullOrWhiteSpace(eTagChecksum));
                    Assert.Equal(computedChecksum, eTagChecksum.ToLowerInvariant());
                }
            }
        }

        var ct = CancellationToken.None;
        var viewResponse = await environment.Client.GetAsync($"/api/printers/{printerId}/documents/view?limit=10");
        viewResponse.EnsureSuccessStatusCode();
        var viewDocumentList = await viewResponse.Content.ReadFromJsonAsync<ViewDocumentListResponseDto>(cancellationToken: ct);
        Assert.NotNull(viewDocumentList);
        var viewDocuments = viewDocumentList.Result.Items;
        var viewDocument = viewDocuments.FirstOrDefault(doc => doc.Id == documentId)
            ?? viewDocuments.FirstOrDefault();
        Assert.NotNull(viewDocument);

        DocumentAssertions.EqualView(
            scenario.ExpectedViewElements,
            Protocol.EscPos,
            viewDocument,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(scenario.Input.Length, bytesSent, viewDocument);

    }

    private static async Task<TestPrinterChannel> CreatePrinterAsync(
        TestServiceContext.ControllerTestContext environment,
        Guid printerId,
        string displayName,
        int widthInDots,
        int? heightInDots)
    {
        var client = environment.Client;

        var request = new CreatePrinterRequestDto(
            printerId,
            displayName,
            "EscPos",
            widthInDots,
            heightInDots,
            false,
            null,
            null);

        var response = await client.PostAsJsonAsync("/api/printers", request);
        response.EnsureSuccessStatusCode();

        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId, timeoutInMs: 2000);
        return await listener.AcceptClientAsync(CancellationToken.None);
    }

    private static async Task AuthenticateAsync(TestServiceContext.ControllerTestContext environment, string displayName)
    {
        var client = environment.Client;

        // Create new workspace
        var workspaceId = Guid.NewGuid();
        var createWorkspaceResponse = await client.PostAsJsonAsync("/api/workspaces", new CreateWorkspaceRequestDto(workspaceId, displayName));
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspaceResponseDto = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceResponseDto>();
        Assert.NotNull(workspaceResponseDto);
        Assert.Equal(workspaceId, workspaceResponseDto.Id);
        var token = workspaceResponseDto.Token;

        // Login to workspace using token and get jwt access token
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(token));
        loginResponse.EnsureSuccessStatusCode();
        var loginResponseDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginResponseDto);
        Assert.NotNull(loginResponseDto.Workspace);
        Assert.Equal(workspaceId, loginResponseDto.Workspace.Id);
        var accessToken = loginResponseDto.AccessToken;

        // Set jwt access token for further requests
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static async Task SendWithChunkStrategyAsync(
        TestServiceContext.ControllerTestContext environment,
        TestPrinterChannel channel,
        byte[] payload,
        EscPosChunkStrategy strategy)
    {
        if (payload.Length == 0)
        {
            return;
        }

        var clockFactory = Assert.IsType<TestClockFactory>(environment.ClockFactory);
        var remaining = payload.Length;

        foreach (var step in EscPosScenarioChunker.EnumerateChunks(payload, strategy))
        {
            await channel.SendToServerAsync(step.Buffer, CancellationToken.None);
            remaining -= step.Buffer.Length;

            if (remaining <= 0)
            {
                continue;
            }

            if (step.DelayAfterMilliseconds <= 0)
            {
                continue;
            }

            var boundedDelay = Math.Min(step.DelayAfterMilliseconds,
                Math.Max(10, PrinterConstants.ListenerIdleTimeoutMs / 2));
            if (boundedDelay > 0)
            {
                clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(boundedDelay));
            }
        }
    }

    private static void AdvanceBeyondIdle(TestServiceContext.ControllerTestContext environment, int additionalMs)
    {
        var clockFactory = Assert.IsType<TestClockFactory>(environment.ClockFactory);
        clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(additionalMs));
    }
}
