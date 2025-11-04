namespace Printify.Domain.Core;

/// <summary>
/// Clock abstraction used by tokenizer sessions and tests to control time in a deterministic manner.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Start the clock. Implementations may initialize or reset internal counters here.
    /// </summary>
    void Restart();

    /// <summary>
    /// Current elapsed milliseconds since <see cref="Restart"/> was called.
    /// </summary>
    long ElapsedMs { get; }

    /// <summary>
    /// Advance the clock by the specified duration. Tests use this to simulate time passage.
    /// </summary>
    void Advance(TimeSpan delta);

    /// <summary>
    /// Creates a task that completes after the specified delay.
    /// </summary>
    /// <param name="delay">The time span to wait before completing the returned task.</param>
    /// <param name="ct">A cancellation token to observe while waiting.</param>
    /// <returns>A task that represents the time delay.</returns>
    Task DelayAsync(TimeSpan delay, CancellationToken ct = default);
}
