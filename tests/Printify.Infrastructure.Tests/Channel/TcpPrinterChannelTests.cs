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
            true,
            1024,
            4096,
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

    [Fact]
    public async Task MultipleConnections_FirstDisconnect_DoesNotAffectSecond()
    {
        var port = GetFreeTcpPort();
        var printer = new Printer(
            Guid.NewGuid(),
            null,
            null,
            "multi-connection-printer",
            "escpos",
            512,
            null,
            DateTimeOffset.UtcNow,
            "127.0.0.1",
            port,
            true,
            1024,
            4096,
            IsPinned: false,
            IsDeleted: false);

        await using var listener = new TcpPrinterListener(printer, NullLogger<TcpPrinterListener>.Instance);
        await listener.StartAsync(CancellationToken.None);

        var firstChannelSource = new TaskCompletionSource<IPrinterChannel>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondChannelSource = new TaskCompletionSource<IPrinterChannel>(TaskCreationOptions.RunContinuationsAsynchronously);
        listener.ChannelAccepted += (_, args) =>
        {
            if (!firstChannelSource.Task.IsCompleted)
            {
                firstChannelSource.TrySetResult(args.Channel);
            }
            else
            {
                secondChannelSource.TrySetResult(args.Channel);
            }

            return ValueTask.CompletedTask;
        };

        var client1 = new TcpClient();
        await client1.ConnectAsync(IPAddress.Loopback, port);
        await using var firstChannel = await firstChannelSource.Task.WaitAsync(TimeSpan.FromSeconds(3));

        var firstClosed = new TaskCompletionSource<ChannelClosedReason>(TaskCreationOptions.RunContinuationsAsynchronously);
        firstChannel.Closed += (_, args) =>
        {
            firstClosed.TrySetResult(args.Reason);
            return ValueTask.CompletedTask;
        };

        var client2 = new TcpClient();
        await client2.ConnectAsync(IPAddress.Loopback, port);
        await using var secondChannel = await secondChannelSource.Task.WaitAsync(TimeSpan.FromSeconds(3));

        var secondClosed = new TaskCompletionSource<ChannelClosedReason>(TaskCreationOptions.RunContinuationsAsynchronously);
        secondChannel.Closed += (_, args) =>
        {
            secondClosed.TrySetResult(args.Reason);
            return ValueTask.CompletedTask;
        };

        var secondData = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        secondChannel.DataReceived += (_, args) =>
        {
            secondData.TrySetResult(args.Buffer.ToArray());
            return ValueTask.CompletedTask;
        };

        client1.Dispose();
        var firstReason = await firstClosed.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(ChannelClosedReason.Completed, firstReason);

        var payload = new byte[] { 9, 8, 7, 6 };
        var stream2 = client2.GetStream();
        await stream2.WriteAsync(payload, CancellationToken.None);

        var received = await secondData.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(payload, received);

        await Task.Delay(200);
        Assert.False(secondClosed.Task.IsCompleted, "Second channel unexpectedly closed after first disconnect.");

        client2.Dispose();
        await secondChannel.DisposeAsync();
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
