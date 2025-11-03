using System;
using System.Threading;
using System.Threading.Tasks;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.TestServices.Printing;

/// <summary>
/// In-memory listener that immediately exposes a <see cref="TestPrinterChannel"/> when started.
/// </summary>
public sealed class TestPrinterListener(Printer printer) : IPrinterListener
{
    private bool disposed;

    public event Func<IPrinterListener, PrinterChannelAcceptedEventArgs, ValueTask>? ChannelAccepted;

    public Guid PrinterId => printer.Id;

    public PrinterListenerStatus Status { get; private set; } = PrinterListenerStatus.Idle;

    public bool IsDisposed => disposed;

    public TestPrinterChannel? LastChannel { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Status = PrinterListenerStatus.Listening;

        var channel = new TestPrinterChannel(printer, $"memory://{printer.Id:N}");
        LastChannel = channel;

        if (ChannelAccepted is not null)
        {
            await ChannelAccepted.Invoke(this, new PrinterChannelAcceptedEventArgs(printer.Id, channel))
                .ConfigureAwait(false);
        }
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
        LastChannel = null;
        return ValueTask.CompletedTask;
    }
}
