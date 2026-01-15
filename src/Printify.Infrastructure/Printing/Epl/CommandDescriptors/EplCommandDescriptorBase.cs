using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.Epl.CommandDescriptors;

/// <summary>
/// Base class for EPL command descriptors with default implementations.
/// EPL commands are newline-terminated, so exact length cannot be determined early.
/// </summary>
public abstract class EplCommandDescriptor : ICommandDescriptor
{
    /// <summary>
    /// Command prefix bytes used for trie matching.
    /// </summary>
    public abstract ReadOnlyMemory<byte> Prefix { get; }

    /// <summary>
    /// Minimum length required to match this command.
    /// </summary>
    public abstract int MinLength { get; }

    /// <summary>
    /// EPL commands are newline-terminated, so exact length cannot be determined early.
    /// Always returns null.
    /// </summary>
    public virtual int? TryGetExactLength(ReadOnlySpan<byte> buffer) => null;

    /// <summary>
    /// Attempts to parse the command from the buffer.
    /// </summary>
    public abstract MatchResult TryParse(ReadOnlySpan<byte> buffer);
}
