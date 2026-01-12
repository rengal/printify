namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// Interface for command descriptors that parse specific printer protocol commands.
/// Each protocol (ESC/POS, EPL, etc.) provides its own parser state type.
/// </summary>
/// <typeparam name="TState">The parser state type for this protocol.</typeparam>
public interface ICommandDescriptor<TState>
    where TState : class
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
    /// </summary>
    int? TryGetExactLength(ReadOnlySpan<byte> buffer);

    /// <summary>
    /// Attempts to parse the command from the buffer.
    /// </summary>
    /// <param name="buffer">The buffer containing the command data.</param>
    /// <param name="state">The parser state for this protocol.</param>
    /// <returns>A match result indicating success, failure, or need for more data.</returns>
    MatchResult TryParse(ReadOnlySpan<byte> buffer, TState state);
}
