namespace Printify.Domain.Printers;

// Listener states
public enum PrinterListenerStatus
{
    Unknown,
    Idle,
    OpeningPort,
    Listening,
    Failed
}
