using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Application.Printing.Events;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Printify.Tests.Shared.Document;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Workspaces.Requests;
using Printify.Web.Contracts.Workspaces.Responses;

namespace Printify.Web.Tests.EscPos;

public class EscPosTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    protected const byte Esc = 0x1B;
    protected const byte Gs = 0x1D;
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(1);

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
        var channel = await CreatePrinterAsync(environment, printerId, "EscPos Idle Printer");

        await using var streamEnumerator = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();
        var nextEventTask = streamEnumerator.MoveNextAsync().AsTask();

        await channel.WriteAsync(new byte[] { 0x07 }, CancellationToken.None);

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

        Assert.True(nextEventTask.Result);
        var documentEvent = streamEnumerator.Current;
        DocumentAssertions.Equal(documentEvent.Document, Protocol.EscPos,
        [
            new Bell()
        ]);
    }

    protected async Task RunScenarioAsync(EscPosScenario scenario)
    {
        foreach (var strategy in ChunkStrategies)
        {
            await RunScenarioAsync(scenario, $"escpos-strategy-{strategy.Name}", strategy);
        }
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
            await CreatePrinterAsync(environment, printerId, $"EscPos Test Printer {userPrefix}-{completionMode}");

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

        DocumentAssertions.Equal(documentEvent.Document, Protocol.EscPos,
            scenario.ExpectedFinalizedElements ?? scenario.ExpectedElements);
    }

    private static async Task<TestPrinterChannel> CreatePrinterAsync(
        TestServiceContext.ControllerTestContext environment,
        Guid printerId,
        string displayName)
    {
        var client = environment.Client;

        var request = new CreatePrinterRequestDto(
            printerId,
            displayName,
            "EscPos",
            512,
            null,
            9106,
            false,
            null,
            null);

        var response = await client.PostAsJsonAsync("/api/printers", request);
        response.EnsureSuccessStatusCode();

        if (!TestPrinterListenerFactory.TryGetListener(printerId, out var listener))
        {
            throw new InvalidOperationException($"Listener for printer {printerId} was not registered.");
        }

        return await listener.AcceptClientAsync();
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
            await channel.WriteAsync(step.Buffer, CancellationToken.None);
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

