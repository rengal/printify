namespace Printify.Domain.Printing;

/// <summary>
/// Minimal base type for all protocol commands.
/// Commands are protocol-specific instructions parsed from raw printer bytes.
/// </summary>
public abstract record Command
{
    /// <summary>
    /// Raw command bytes as received from the printer.
    /// </summary>
    public byte[] RawBytes { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Length of the command in bytes.
    /// </summary>
    public int LengthInBytes { get; init; }
}
