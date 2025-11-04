using System;
using System.Threading;
using System.Threading.Tasks;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.TestServices.Printing;

/// <summary>
/// In-memory listener used for integration tests. Channel acceptance is triggered explicitly via <see cref="AcceptClientAsync"/>.
/// </summary>
public sealed class TestPrinterListener : IPrinterListener
{
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
        Status = PrinterListenerStatus.Listening;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Status = PrinterListenerStatus.Idle;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        disposed = true;
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
