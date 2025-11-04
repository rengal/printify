using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
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

public sealed class EscPosTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
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

        var payload = new byte[] { 0x07 };
        await channel.WriteAsync(payload, CancellationToken.None);

        var clockFactory = Assert.IsType<TestClockFactory>(environment.ClockFactory);
        var totalElapsed = 0;
        const int stepMs = 50;
        while (totalElapsed + stepMs < PrinterConstants.ListenerIdleTimeoutMs)
        {
            clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(stepMs));
            await Task.Delay(1);
            Assert.False(nextEventTask.IsCompleted, "Document should not complete before idle timeout.");
            totalElapsed += stepMs;
        }

        clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(stepMs));
        await nextEventTask.WaitAsync(TimeSpan.FromMilliseconds(500));
        Assert.True(nextEventTask.IsCompleted, "Task should complete after idle timeout");
        Assert.True(nextEventTask.Result);

        var documentEvent = streamEnumerator.Current;
        DocumentAssertions.Equal(documentEvent.Document, Protocol.EscPos,
        [
            new Bell(1)
        ]);
    }

    [Fact]
    public async Task BellByte_ProducesBellElement()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        await AuthenticateAsync(environment, "escpos-bell-user");

        var printerId = Guid.NewGuid();
        var channel = await CreatePrinterAsync(environment, printerId, "EscPos Bell Printer");

        await using var streamEnumerator = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();

        var payload = new byte[] { 0x07 };
        await channel.WriteAsync(payload, CancellationToken.None);
        await channel.CloseAsync(ChannelClosedReason.Completed);

        Assert.True(await streamEnumerator.MoveNextAsync());
        var documentEvent = streamEnumerator.Current;

        DocumentAssertions.Equal(documentEvent.Document, Protocol.EscPos,
        [
            new Bell(1)
        ]);
    }

    [Fact]
    public async Task BellChunks_WithRandomDelays_ProduceSingleDocument()
    {
        await using var environment = TestServiceContext.CreateForControllerTest(factory);
        await AuthenticateAsync(environment, "escpos-bell-random");

        var printerId = Guid.NewGuid();
        var channel = await CreatePrinterAsync(environment, printerId, "EscPos Bell Random");

        await using var streamEnumerator = environment.DocumentStream
            .Subscribe(printerId, CancellationToken.None)
            .GetAsyncEnumerator();

        var bellBytes = Enumerable.Repeat((byte)0x07, 10).ToArray();
        var random = new Random(2025);
        var clockFactory = Assert.IsType<TestClockFactory>(environment.ClockFactory);
        var maxDelay = Math.Max(1, PrinterConstants.ListenerIdleTimeoutMs / 4);

        var offset = 0;
        while (offset < bellBytes.Length)
        {
            var remaining = bellBytes.Length - offset;
            var chunkSize = random.Next(1, Math.Min(remaining, 4) + 1);
            await channel.WriteAsync(bellBytes.AsMemory(offset, chunkSize), CancellationToken.None);
            offset += chunkSize;

            if (offset < bellBytes.Length)
            {
                var delay = random.Next(0, maxDelay);
                if (delay > 0)
                {
                    clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(delay));
                }
            }
        }

        clockFactory.AdvanceAll(TimeSpan.FromMilliseconds(PrinterConstants.ListenerIdleTimeoutMs + 50));

        Assert.True(await streamEnumerator.MoveNextAsync());
        var documentEvent = streamEnumerator.Current;

        var expected = Enumerable.Range(1, bellBytes.Length)
            .Select(sequence => (Element)new Bell(sequence))
            .ToList();

        DocumentAssertions.Equal(documentEvent.Document, Protocol.EscPos, expected);
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
}
