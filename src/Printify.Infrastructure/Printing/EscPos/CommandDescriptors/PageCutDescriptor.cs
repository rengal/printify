namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Command: GS V m [n] - paper cut with mode.
/// ASCII: GS V m [n].
/// HEX: 1D 56 0xMM [0xNN].
/// </summary>
public sealed class PageCutDescriptor : ICommandDescriptor
{
    private const int MinimumLength = 3;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x56 };
    
    public int MinLength => 3;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < MinimumLength)
        {
            return null;
        }

        var m = buffer[2];
        return GetCommandLength(m);
    }

    private static int? GetCommandLength(byte mode)
    {
        return mode switch
        {
            // Function A: Cuts the paper (no feed parameter)
            0 or 48 => 3,
            
            // Function B: Feeds paper and cuts (has feed parameter n)
            65 or 66 => 4,
            
            // Function C: Sets cutting position (has feed parameter n)
            97 or 98 => 4,
            
            // Function D: Feeds, cuts, and feeds to print start (has feed parameter n)
            103 or 104 => 4,
            
            // Unknown/unsupported mode
            _ => null
        };
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        if (buffer.Length < MinimumLength)
        {
            return MatchResult.NeedMore();
        }

        // Mode byte (m) is at buffer[2]
        var modeValue = buffer[2];
        
        // Determine bytes to consume and cut mode
        var bytesToConsume = GetCommandLength(modeValue) ?? 3;
        
        // Extract feed parameter if present (4-byte commands have feed parameter at buffer[3])
        int? feedMotionUnits = bytesToConsume == 4 && buffer.Length >= 4 ? buffer[3] : null;
        
        // Map ESC/POS mode byte to PagecutMode enum
        var cutMode = modeValue switch
        {
            0 or 48 or 65 or 97 or 103 => Domain.Documents.Elements.PagecutMode.Full,
            1 or 49 or 66 or 98 or 104 => Domain.Documents.Elements.PagecutMode.Partial,
            _ => Domain.Documents.Elements.PagecutMode.Full // Default to full cut for unknown modes
        };

        // Create Pagecut element with the determined mode and feed parameter
        var element = new Domain.Documents.Elements.Pagecut(cutMode, feedMotionUnits);
        
        return MatchResult.Matched(bytesToConsume, element);
    }
}
