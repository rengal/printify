namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: FS p m n - print stored logo by identifier.
/// ASCII: FS p m n.
/// HEX: 1C 70 0xMM 0xNN.
public sealed class FsStoredLogoDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1C, (byte)'p' };
    public int MinLength => 4;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => buffer.Length >= MinLength ? MinLength : null;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("FsStoredLogoDescriptor is not wired yet.");
}
