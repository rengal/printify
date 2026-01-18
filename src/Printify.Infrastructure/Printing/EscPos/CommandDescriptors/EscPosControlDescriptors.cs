using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Command: ESC @ - reset printer.
/// ASCII: ESC @.
/// HEX: 1B 40.
/// </summary>
public sealed class ResetPrinterDescriptor : ICommandDescriptor
{
    private const int FixedLength = 2;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x40 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        return MatchResult.Matched(new Initialize());
    }
}

/// <summary>
/// Command: DLE EOT n - real-time printer status.
/// ASCII: DLE EOT n.
/// HEX: 10 04 n.
/// </summary>
public sealed class GetPrinterStatusDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x10, 0x04 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        byte statusByte = buffer[2];

        // For status bytes 0x01-0x04, length is 3
        if (statusByte is >= 0x01 and <= 0x04)
            return 3;

        // For status bytes 0x07, 0x08, 0x12, need 4 bytes
        if (statusByte is 0x07 or 0x08 or 0x12)
            return buffer.Length >= 4 ? 4 : null;

        // Unknown status byte, cannot determine length
        return null;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        byte statusByte = buffer[2];

        // DLE EOT n for status bytes 0x01-0x04
        if (statusByte is >= 0x01 and <= 0x04)
        {
            var requestType = (StatusRequestType)statusByte;
            var element = new StatusRequest(requestType);
            return MatchResult.Matched(element);
        }

        // Extended status queries (4 bytes) - keep as GetPrinterStatus for now
        if (statusByte is 0x07 or 0x08 or 0x12)
        {
            var additionalStatusByte = buffer[3];
            var element = new GetPrinterStatus(statusByte, additionalStatusByte);
            return MatchResult.Matched(element);
        }

        var error = new PrinterError($"Invalid printer status byte: 0x{statusByte:X2}. Expected 0x01-0x04, 0x07, 0x08, or 0x12");
        return MatchResult.Matched(error);
    }
}

/// <summary>
/// Command: ESC p m t1 t2 - cash drawer pulse.
/// ASCII: ESC p m t1 t2.
/// HEX: 1B 70 0xMM 0xT1 0xT2.
/// </summary>
public sealed class PulseDescriptor : ICommandDescriptor
{
    private const int FixedLength = 5;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'p' };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < FixedLength)
            return MatchResult.NeedMore();

        int pin = buffer[2];
        int onTimeMs = buffer[3];
        int offTimeMs = buffer[4];

        var element = new Pulse(pin, onTimeMs, offTimeMs);
        return MatchResult.Matched(element);
    }
}

/// <summary>
/// Command: BEL - buzzer/beeper.
/// ASCII: BEL.
/// HEX: 07.
/// </summary>
public sealed class BelDescriptor : ICommandDescriptor
{
    private const int FixedLength = 1;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x07 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer) => MatchResult.Matched(new Bell());
}
