namespace Printify.Domain.Printers;

// Listener states
public enum PrinterListenerStatus
{
    Idle,
    OpeningPort,
    Listening,
    Failed
}
