namespace Printify.Listener;

using System;
using Printify.Contracts.Service;

/// <summary>
/// Configuration for the TCP listener and default session options.
/// </summary>
public sealed class ListenerOptions
{
    /// <summary>
    /// TCP port to listen on for incoming ESC/POS clients.
    /// </summary>
    public int Port { get; init; } = 9100;

    /// <summary>
    /// Idle timeout to consider session completed if no data is received (seconds).
    /// </summary>
    public int IdleTimeoutSeconds { get; init; } = 30;
}
