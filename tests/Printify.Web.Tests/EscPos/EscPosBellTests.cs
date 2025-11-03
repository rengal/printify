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

public sealed class EscPosBellTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
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

        return await WaitForChannelAsync(environment.PrinterListenerOrchestrator, printerId, TimeSpan.FromSeconds(2));
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

    private static async Task<TestPrinterChannel> WaitForChannelAsync(
        IPrinterListenerOrchestrator orchestrator,
        Guid printerId,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var channel = orchestrator.GetActiveChannels(printerId).OfType<TestPrinterChannel>().FirstOrDefault();
            if (channel is not null)
            {
                return channel;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"No active channel registered for printer {printerId}.");
    }
}
