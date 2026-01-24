namespace Printify.Domain.Printers;

public static class PrinterConstants
{
    // Dimension constraints
    public const int MinWidthInDots = 12;
    public const int MaxWidthInDots = 10000;
    public const int MinHeightInDots = 12;
    public const int MaxHeightInDots = 10000;

    // Name constraints
    public const int MaxNameLength = 100;

    // Tcp listener port constraints
    public const int MinTcpListenerPort = 3000;
    public const int MaxTcpListenerPort = 65535;

    // Listener timeout
    public const int ListenerIdleTimeoutMs = 2000;
}
