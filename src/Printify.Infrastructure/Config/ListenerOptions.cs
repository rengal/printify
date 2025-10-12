namespace Printify.Infrastructure.Config;

/// <summary>
/// Configuration for the TCP listener and default session options.
/// </summary>
public sealed class ListenerOptions
{
    /// <summary>
    /// Idle timeout to consider session completed if no data is received (seconds).
    /// </summary>
    public int IdleTimeoutSeconds { get; init; } = 2;
}
