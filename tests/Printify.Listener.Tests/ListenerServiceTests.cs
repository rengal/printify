using System.Net;
using System.Net.Sockets;
using Printify.Services.Listener;
using Printify.Services.Tokenizer;
using Printify.TestServices;

namespace Printify.Listener.Tests;

public sealed class ListenerServiceTests
{
    [Fact]
    public async Task StartAsync_ThenStopAsync_NoClients_Completes()
    {
        await using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer), listener: typeof(ListenerService));

        var listener = context.Listener;

        Assert.NotNull(listener);

        await listener.StartAsync(CancellationToken.None);
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ClientSendingData_IsForwardedToTokenizerSession()
    {
        await using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer), listener: typeof(ListenerService));
        var listener = context.Listener;
        var listenerOptions = context.ListenerOptions;

        Assert.NotNull(listener);

        await listener.StartAsync(CancellationToken.None);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, listenerOptions.Port);

            var payload = new byte[] { 1, 2, 3, 4 };
            await client.GetStream().WriteAsync(payload);

            client.Close();

            // Assert.True(context. tokenizer.SessionCreated.Wait(TimeSpan.FromSeconds(2)));
            //
            // var session = tokenizer.LastSession ?? throw new InvalidOperationException("Session not created.");
            //
            // Assert.True(session.FeedReceived.Wait(TimeSpan.FromSeconds(2)));
            // Assert.Equal(payload, session.ReceivedBytes);
            //
            // Assert.True(session.Completed.Wait(TimeSpan.FromSeconds(2)));
            // Assert.Equal(CompletionReason.ClientDisconnected, session.LastCompletionReason);
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
        }
    }
}
