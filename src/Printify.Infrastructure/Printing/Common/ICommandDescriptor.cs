namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// Interface for command descriptors that parse specific printer protocol commands.
/// Descriptors are pure functions - they parse bytes and return elements without modifying state.
/// State modification (e.g., updating encoding, label dimensions) happens in the parser when elements are emitted.
/// </summary>
public interface ICommandDescriptor
{
    /// <summary>
    /// Command prefix bytes used for trie matching.
    /// </summary>
    ReadOnlyMemory<byte> Prefix { get; }

    /// <summary>
    /// Minimum length required to match this command.
    /// </summary>
    int MinLength { get; }

    /// <summary>
    /// Attempts to determine the exact length of the command from the buffer.
    /// Returns null if length cannot be determined yet.
    /// Default implementation returns null (exact length cannot be determined early).
    /// </summary>
    virtual int? TryGetExactLength(ReadOnlySpan<byte> buffer) => null;

    /// <summary>
    /// Attempts to parse the command from the buffer.
    /// This is a pure function - it does not modify any state.
    /// </summary>
    /// <param name="buffer">The buffer containing the command data.</param>
    /// <returns>A match result indicating success, failure, or need for more data.</returns>
    MatchResult TryParse(ReadOnlySpan<byte> buffer);
}
