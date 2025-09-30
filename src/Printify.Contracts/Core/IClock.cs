namespace Printify.Contracts.Core;

/// <summary>
/// Clock abstraction used by tokenizer sessions and tests to control time in a deterministic manner.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Start the clock. Implementations may initialize or reset internal counters here.
    /// </summary>
    void Start();

    /// <summary>
    /// Current elapsed milliseconds since <see cref="Start"/> was called.
    /// </summary>
    long ElapsedMs { get; }

    /// <summary>
    /// Advance the clock by the specified duration. Tests use this to simulate time passage.
    /// </summary>
    void Advance(TimeSpan delta);
}
