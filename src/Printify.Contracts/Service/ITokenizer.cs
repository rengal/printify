namespace Printify.Contracts.Service;

/// <summary>
/// Tokenizer service that converts protocol byte streams into document elements.
/// </summary>
public interface ITokenizer
{
    /// <summary>
    /// Identifier for the protocol handled by this tokenizer (e.g., "escpos").
    /// </summary>
    string Protocol { get; }

    /// <summary>
    /// Creates a stateful tokenizer session.
    /// </summary>
    /// <param name="options">Optional session settings that influence buffering, limits, and drain rate.</param>
    /// <param name="clock">
    /// Optional clock that drives time-based behaviors. Provide a manual clock in tests to advance time
    /// deterministically; omit the argument to use the default stopwatch-backed clock in production.
    /// </param>
    ITokenizerSession CreateSession(TokenizerSessionOptions? options = null, IClock? clock = null);
}
