using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Printify.TestServices;
using Printify.TestServices.Printing;
using Printify.Web.Contracts.Auth.AnonymousSession.Response;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Contracts.Auth.Responses;
using Printify.Web.Contracts.Printers.Requests;
using Printify.Web.Contracts.Users.Requests;

namespace Printify.Web.Tests.EscPos;

public sealed partial class EscPosTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    private sealed record ChunkStrategy(string Name, int[] ChunkPattern, int[] DelayPattern);
    private enum CompletionMode
    {
        AdvanceIdleTimeout,
        CloseChannel
    }

    private static readonly ChunkStrategy[] ChunkStrategies =
    {
        new("SingleChunk", new[] { int.MaxValue }, Array.Empty<int>()),
        new("SingleByte", new[] { 1 }, new[] { 60 }),
        new("Increasing1234", new[] { 1, 2, 3, 4 }, new[] { 40, 70, 100, 130 }),
        new("Decreasing4321", new[] { 4, 3, 2, 1 }, new[] { 120, 90, 60, 30 }),
        new("Alternating12", new[] { 1, 2 }, new[] { 65, 95 }),
        new("Alternating21", new[] { 2, 1 }, new[] { 85, 55 }),
        new("Triplet3", new[] { 3 }, new[] { 110 }),
        new("Mixed231", new[] { 2, 3, 1 }, new[] { 75, 115, 55 }),
        new("Mixed3211", new[] { 3, 2, 1, 1 }, new[] { 90, 70, 50, 50 }),
        new("LargeThenSmall", new[] { 5, 1 }, new[] { 100, 40 }),
        new("SmallThenLarge", new[] { 1, 5 }, new[] { 45, 105 }),
        new("PrimePattern", new[] { 2, 3, 5 }, new[] { 70, 100, 130 })
    };

    public EscPosTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

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
            new Bell(1)
        ]);
    }

    private async Task RunScenarioAsync(EscPosScenario scenario)
    {
        foreach (var strategy in ChunkStrategies)
        {
            await RunScenarioAsync(scenario, $"escpos-strategy-{strategy.Name}", strategy);
        }
    }

    private async Task RunScenarioAsync(EscPosScenario scenario, ChunkStrategy strategy)
    {
        await RunScenarioAsync(scenario, $"escpos-strategy-{strategy.Name}", strategy);
    }

    private async Task RunScenarioAsync(
        EscPosScenario scenario,
        string userPrefix,
        ChunkStrategy strategy)
    {
        await RunScenarioAsync(scenario, userPrefix, strategy, CompletionMode.AdvanceIdleTimeout);
        await RunScenarioAsync(scenario, userPrefix, strategy, CompletionMode.CloseChannel);
    }

    private async Task RunScenarioAsync(
        EscPosScenario scenario,
        string userPrefix,
        ChunkStrategy strategy,
        CompletionMode completionMode)
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        await AuthenticateAsync(environment, $"{userPrefix}-{completionMode}");

        var printerId = Guid.NewGuid();
        var channel = await CreatePrinterAsync(environment, printerId, $"EscPos Test Printer {userPrefix}-{completionMode}");

        await using var streamEnumerator = environment.DocumentStream
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

        Assert.True(await streamEnumerator.MoveNextAsync());
        var documentEvent = streamEnumerator.Current;

        DocumentAssertions.Equal(documentEvent.Document, Protocol.EscPos, scenario.ExpectedElements);
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
        client.DefaultRequestHeaders.Remove("X-Forwarded-For");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "127.0.0.1");

        var anonymousResponse = await client.PostAsync("/api/auth/anonymous", new StringContent(string.Empty));
        anonymousResponse.EnsureSuccessStatusCode();
        var sessionDto = await anonymousResponse.Content.ReadFromJsonAsync<AnonymousSessionDto>();
        Assert.NotNull(sessionDto);

        var userId = Guid.NewGuid();
        var createUserResponse = await client.PostAsJsonAsync("/api/users", new CreateUserRequestDto(userId, displayName));
        createUserResponse.EnsureSuccessStatusCode();

        await using (var scope = environment.CreateScope())
        {
            var jwtGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
            var anonymousToken = jwtGenerator.GenerateToken(null, sessionDto!.Id);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", anonymousToken);
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(userId));
        loginResponse.EnsureSuccessStatusCode();

        var loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginDto);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.AccessToken);
    }

    private static async Task SendWithChunkStrategyAsync(
        TestServiceContext.ControllerTestContext environment,
        TestPrinterChannel channel,
        byte[] payload,
        ChunkStrategy strategy)
    {
        if (payload.Length == 0)
        {
            return;
        }

        var clockFactory = Assert.IsType<TestClockFactory>(environment.ClockFactory);
        var chunkPattern = strategy.ChunkPattern;
        var delayPattern = strategy.DelayPattern;

        var offset = 0;
        var iteration = 0;

        while (offset < payload.Length)
        {
            var chunkSizePattern = chunkPattern.Length == 0 ? payload.Length : chunkPattern[iteration % chunkPattern.Length];
            var chunkSize = Math.Min(chunkSizePattern, payload.Length - offset);
            await channel.WriteAsync(payload.AsMemory(offset, chunkSize), CancellationToken.None);
            offset += chunkSize;

            if (offset < payload.Length && delayPattern.Length > 0)
            {
                var delay = delayPattern[iteration % delayPattern.Length];
                var boundedDelay = Math.Min(delay, Math.Max(10, PrinterConstants.ListenerIdleTimeoutMs / 2));
                if (boundedDelay > 0)
                {
                    clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(boundedDelay));
                }
            }

            iteration++;
        }
    }

    private static void AdvanceBeyondIdle(TestServiceContext.ControllerTestContext environment, int additionalMs)
    {
        var clockFactory = Assert.IsType<TestClockFactory>(environment.ClockFactory);
        clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(additionalMs));
    }
}

