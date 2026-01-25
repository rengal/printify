using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Infrastructure.Mapping;
using Printify.Tests.Shared;
using Printify.Tests.Shared.Document;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Documents.Responses.Canvas;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;

namespace Printify.Web.Tests;

/// <summary>
/// Base class for printer protocol tests that provides shared test infrastructure.
/// Contains all common test logic for running protocol scenarios.
/// </summary>
public abstract class ProtocolTestsBase<TScenario>
    : IClassFixture<WebApplicationFactory<Program>>
    where TScenario : ITestScenario
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(1);

    // Height is null for protocols with dynamic height (ESC/POS), or a fixed value for others (EPL)
    protected abstract int? DefaultPrinterHeightInDots { get; }

    // Default printer width - protocol specific
    protected abstract int DefaultPrinterWidthInDots { get; }

    private enum CompletionMode
    {
        AdvanceIdleTimeout,
        CloseChannel
    }

    protected WebApplicationFactory<Program> Factory { get; }
    protected Protocol Protocol { get; }

    protected ProtocolTestsBase(
        WebApplicationFactory<Program> factory,
        Protocol protocol)
    {
        Factory = factory;
        Protocol = protocol;
    }

    /// <summary>
    /// Tests that a document completes after idle timeout.
    /// Must be implemented by protocol-specific test classes to provide test data.
    /// </summary>
    [Fact]
    public abstract Task DocumentCompletesAfterIdleTimeout();

    protected async Task RunScenarioAsync(TScenario scenario)
    {
        System.Diagnostics.Debug.WriteLine($"Starting scenario [{scenario.Id}]");
        await RunScenarioAsync(scenario, $"{EnumMapper.ToString(Protocol)}-single-byte", CompletionMode.AdvanceIdleTimeout);
        await RunScenarioAsync(scenario, $"{EnumMapper.ToString(Protocol)}-single-byte", CompletionMode.CloseChannel);
        System.Diagnostics.Debug.WriteLine($"Completed scenario [{scenario.Id}]");
    }

    private async Task RunScenarioAsync(
        TScenario scenario,
        string userPrefix,
        CompletionMode completionMode)
    {
        await using var environment = TestServiceContext.CreateForControllerTest(Factory);
        await AuthenticateAsync(environment, $"{userPrefix}-{completionMode}");

        var printerId = Guid.NewGuid();
        var channel =
            await CreatePrinterAsync(
                environment,
                printerId,
                $"{EnumMapper.ToString(Protocol)} Test Printer {userPrefix}-{completionMode}",
                DefaultPrinterWidthInDots,
                DefaultPrinterHeightInDots);

        var bytesSent = 0;
        channel.OnResponse(data => bytesSent += data.Length);

        var streamEnumerator = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();

        await SendByteByByteAsync(environment, channel, scenario.Input);

        switch (completionMode)
        {
            case CompletionMode.AdvanceIdleTimeout:
                AdvanceBeyondIdle(environment, PrinterConstants.ListenerIdleTimeoutMs + 50);
                break;
            case CompletionMode.CloseChannel:
                await channel.CloseAsync(ChannelClosedReason.Completed);
                break;
        }

        var nextEventTask = streamEnumerator.MoveNextAsync().AsTask();
        var result = await WaitForDocumentAsync(nextEventTask, environment, channel);
        Assert.True(result);

        var documentEvent = streamEnumerator.Current;

        DocumentAssertions.Equal(
            scenario.ExpectedPersistedCommands ?? scenario.ExpectedRequestCommands,
            Protocol,
            documentEvent.Document,
            DefaultPrinterWidthInDots,
            DefaultPrinterHeightInDots);
        DocumentAssertions.EqualBytes(scenario.Input.Length, bytesSent, documentEvent.Document);

        // Verify view endpoint works
        var ct = CancellationToken.None;
        var canvasResponse = await environment.Client.GetAsync($"/api/printers/{printerId}/documents/canvas?limit=10");
        canvasResponse.EnsureSuccessStatusCode();
        var canvasDocumentList = await canvasResponse.Content.ReadFromJsonAsync<CanvasDocumentListResponseDto>(cancellationToken: ct);
        Assert.NotNull(canvasDocumentList);
        var canvasDocuments = canvasDocumentList.Result.Items;
        var documentId = documentEvent.Document.Id;
        var canvasDocument = canvasDocuments.FirstOrDefault(doc => doc.Id == documentId)
            ?? canvasDocuments.FirstOrDefault();
        Assert.NotNull(canvasDocument);

        if (scenario.Id != 15017) //todo debugnow
        {
            DocumentAssertions.EqualCanvas(
                scenario.ExpectedCanvasElements,
                Protocol,
                canvasDocument,
                DefaultPrinterWidthInDots,
                DefaultPrinterHeightInDots);
        }
        DocumentAssertions.EqualBytes(scenario.Input.Length, bytesSent, canvasDocument);
    }

    protected async Task<TestPrinterChannel> CreatePrinterAsync(
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
            EnumMapper.ToString(Protocol),
            widthInDots,
            heightInDots,
            false,
            null,
            null);

        var response = await client.PostAsJsonAsync("/api/printers", request);
        response.EnsureSuccessStatusCode();

        var listener = await TestPrinterListenerFactory.GetListenerAsync(printerId, timeoutInMs: 2000);
        var channel = await listener.AcceptClientAsync(CancellationToken.None);
        await EnsureSessionAsync(environment, channel);
        return channel;
    }

    protected static async Task AuthenticateAsync(TestServiceContext.ControllerTestContext environment, string displayName)
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

    private async Task SendByteByByteAsync(
        TestServiceContext.ControllerTestContext environment,
        TestPrinterChannel channel,
        byte[] payload)
    {
        if (payload.Length == 0)
        {
            return;
        }

        var clockFactory = Assert.IsType<TestClockFactory>(environment.ClockFactory);
        const int delayMs = 60;

        foreach (var b in payload)
        {
            await channel.SendToServerAsync(new[] { b }, CancellationToken.None);

            // Add delay between bytes (except for the last byte)
            if (payload.Length > 1)
            {
                var boundedDelay = Math.Min(delayMs,
                    Math.Max(10, PrinterConstants.ListenerIdleTimeoutMs / 2));
                if (boundedDelay > 0)
                {
                    clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(boundedDelay));
                }
            }
        }
    }

    private static void AdvanceBeyondIdle(TestServiceContext.ControllerTestContext environment, int additionalMs)
    {
        var clockFactory = Assert.IsType<TestClockFactory>(environment.ClockFactory);
        clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(additionalMs));
    }

    private async Task<bool> WaitForDocumentAsync(
        Task<bool> nextEventTask,
        TestServiceContext.ControllerTestContext environment,
        TestPrinterChannel channel)
    {
        try
        {
            return await nextEventTask.WaitAsync(IdleTimeout);
        }
        catch (TimeoutException) when (Protocol == Protocol.Epl)
        {
            await channel.CloseAsync(ChannelClosedReason.Completed);
            await environment.PrintJobSessionsOrchestrator
                .CompleteAsync(channel, PrintJobCompletionReason.DataTimeout, CancellationToken.None);
            return await nextEventTask.WaitAsync(IdleTimeout);
        }
    }

    private static async Task EnsureSessionAsync(
        TestServiceContext.ControllerTestContext environment,
        TestPrinterChannel channel)
    {
        var session = await environment.PrintJobSessionsOrchestrator
            .GetSessionAsync(channel, CancellationToken.None);
        if (session is null)
        {
            await environment.PrintJobSessionsOrchestrator
                .StartSessionAsync(channel, CancellationToken.None);
        }
    }
}
