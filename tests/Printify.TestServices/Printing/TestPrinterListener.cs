using System.Collections.Concurrent;
using System.Net.Sockets;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.TestServices.Printing;

/// <summary>
/// In-memory listener used for integration tests. Channel acceptance is triggered explicitly via <see cref="AcceptClientAsync"/>.
/// </summary>
public sealed class TestPrinterListener : IPrinterListener
{
    private static readonly ConcurrentDictionary<int, Guid> UsedPorts = new();

    private readonly Printer printer;
    private bool disposed;

    public TestPrinterListener(Printer printer)
    {
        this.printer = printer ?? throw new ArgumentNullException(nameof(printer));
    }

    public event Func<IPrinterListener, PrinterChannelAcceptedEventArgs, ValueTask>? ChannelAccepted;

    public Guid PrinterId => printer.Id;

    public PrinterListenerStatus Status { get; private set; } = PrinterListenerStatus.Idle;

    public bool IsDisposed => disposed;

    public TestPrinterChannel? LastChannel { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Simulate OS port binding: fail if another listener already claimed this port.
        if (!UsedPorts.TryAdd(printer.ListenTcpPortNumber, printer.Id))
        {
            Status = PrinterListenerStatus.Failed;
            throw new SocketException((int)SocketError.AddressAlreadyInUse);
        }

        Status = PrinterListenerStatus.Listening;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Status = PrinterListenerStatus.Idle;
        UsedPorts.TryRemove(new KeyValuePair<int, Guid>(printer.ListenTcpPortNumber, printer.Id));
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        disposed = true;
        UsedPorts.TryRemove(new KeyValuePair<int, Guid>(printer.ListenTcpPortNumber, printer.Id));
        TestPrinterListenerFactory.Unregister(printer.Id);
        LastChannel = null;
        return ValueTask.CompletedTask;
    }

    public async Task<TestPrinterChannel> AcceptClientAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Status != PrinterListenerStatus.Listening)
        {
            throw new InvalidOperationException("Listener must be in Listening state before accepting a client.");
        }

        var channel = new TestPrinterChannel(printer, $"memory://{printer.Id:N}");
        LastChannel = channel;

        if (ChannelAccepted is not null)
        {
            await ChannelAccepted.Invoke(this, new PrinterChannelAcceptedEventArgs(printer.Id, channel)).ConfigureAwait(false);
        }

        return channel;
    }
}
