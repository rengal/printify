namespace Printify.Listener.Tests;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Printify.Contracts;
using Printify.Contracts.Elements;
using Printify.Contracts.Service;
using Printify.TestServcies;
using Printify.TestServcies.Tokenizers;

public sealed class ListenerServiceTests
{
    [Fact]
    public async Task StartAsync_ThenStopAsync_NoClients_Completes()
    {
        var listenerOptions = new ListenerOptions
        {
            Port = 0,
            IdleTimeoutSeconds = 1
        };

        var tokenizer = new TestTokenizer();

        await using var context = TestServices.CreateListenerContext(services =>
        {
            services.AddSingleton(_ => tokenizer);
            services.AddSingleton<ITokenizer>(_ => tokenizer);
            services.AddSingleton<IOptions<ListenerOptions>>(_ => Options.Create(listenerOptions));
        });

        var service = context.ListenerService;

        await service.StartAsync(CancellationToken.None);
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ClientSendingData_IsForwardedToTokenizerSession()
    {
        var listenerOptions = new ListenerOptions
        {
            Port = GetFreePort(),
            IdleTimeoutSeconds = 1
        };

        var tokenizer = new TestTokenizer();

        await using var context = TestServices.CreateListenerContext(services =>
        {
            services.AddSingleton(_ => tokenizer);
            services.AddSingleton<ITokenizer>(_ => tokenizer);
            services.AddSingleton<IOptions<ListenerOptions>>(_ => Options.Create(listenerOptions));
        });

        var service = context.ListenerService;

        await service.StartAsync(CancellationToken.None);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, listenerOptions.Port);

            var payload = new byte[] { 1, 2, 3, 4 };
            await client.GetStream().WriteAsync(payload);

            client.Close();

            Assert.True(tokenizer.SessionCreated.Wait(TimeSpan.FromSeconds(2)));

            var session = tokenizer.LastSession ?? throw new InvalidOperationException("Session not created.");

            Assert.True(session.FeedReceived.Wait(TimeSpan.FromSeconds(2)));
            Assert.Equal(payload, session.ReceivedBytes);

            Assert.True(session.Completed.Wait(TimeSpan.FromSeconds(2)));
            Assert.Equal(CompletionReason.ClientDisconnected, session.LastCompletionReason);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
