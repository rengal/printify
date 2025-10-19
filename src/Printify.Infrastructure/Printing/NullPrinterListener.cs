using System;
using System.Threading;
using System.Threading.Tasks;
using Printify.Application.Printing;

namespace Printify.Infrastructure.Printing;

public sealed class NullPrinterListener : IPrinterListener
{
    private bool isRunning;

    public event Func<IPrinterListener, PrinterChannelAcceptedEventArgs, ValueTask>? ChannelAccepted;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        isRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        isRunning = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        isRunning = false;
        return ValueTask.CompletedTask;
    }
}
