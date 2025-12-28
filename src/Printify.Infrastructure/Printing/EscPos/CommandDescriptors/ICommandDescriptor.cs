namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

public interface ICommandDescriptor
{
    ReadOnlyMemory<byte> Prefix { get; }

    /// <summary>Minimum total length (prefix + parameters + payload) needed before TryMatch can succeed.</summary>
    int MinLength { get; }

    /// <summary>
    /// Optional: if this command encodes its total length within its own header (e.g., GS ( k),
    /// compute it once length fields are available.
    /// </summary>
    /// <returns>
    /// Exact total length (prefix + parameters + payload),
    /// or null if not yet determinable.
    /// </returns>
    int? TryGetExactLength(ReadOnlySpan<byte> buffer);

    /// <summary>
    /// Called once full length is available to construct the element.
    /// </summary>
    MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state);
}
