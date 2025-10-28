using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.Tcp;

namespace Printify.Infrastructure.Tests.Channel;

public sealed class TcpPrinterChannelTests
{
    [Fact]
    public async Task CreateConnected_WithActiveListener_EstablishesBidirectionalChannel()
    {
        var port = GetFreeTcpPort();
        var printer = new Printer(
            Guid.NewGuid(),
            null,
            null,
            "test-printer",
            "escpos",
            512,
            null,
            DateTimeOffset.UtcNow,
            "127.0.0.1",
            port,
            IsPinned: false,
            IsDeleted: false);

        await using var listener = new TcpPrinterListener(printer, NullLogger<TcpPrinterListener>.Instance);
        await listener.StartAsync(CancellationToken.None);

        var acceptedChannelSource = new TaskCompletionSource<IPrinterChannel>(TaskCreationOptions.RunContinuationsAsynchronously);
        listener.ChannelAccepted += (_, args) =>
        {
            acceptedChannelSource.TrySetResult(args.Channel);
            return ValueTask.CompletedTask;
        };

        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        await using var serverChannel = await acceptedChannelSource.Task.WaitAsync(TimeSpan.FromSeconds(3));

        var dataReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        serverChannel.DataReceived += (_, args) =>
        {
            dataReceived.TrySetResult(args.Buffer.ToArray());
            return ValueTask.CompletedTask;
        };

        var payload = new byte[] { 1, 2, 3, 4 };
        var stream = client.GetStream();
        await stream.WriteAsync(payload, CancellationToken.None);

        var received = await dataReceived.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(payload, received);

        var channelClosed = new TaskCompletionSource<ChannelClosedReason>(TaskCreationOptions.RunContinuationsAsynchronously);
        serverChannel.Closed += (_, args) =>
        {
            channelClosed.TrySetResult(args.Reason);
            return ValueTask.CompletedTask;
        };

        client.Dispose();
        var reason = await channelClosed.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(ChannelClosedReason.Completed, reason);
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
