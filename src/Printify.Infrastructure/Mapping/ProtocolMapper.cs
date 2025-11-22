using Printify.Domain.Printers;

namespace Printify.Infrastructure.Mapping;

internal static class ProtocolMapper
{
    internal static Protocol ParseProtocol(string protocol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        if (protocol.ToLower() == "escpos")
        {
            return Protocol.EscPos;
        }

        throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Protocol is not supported.");
    }

    internal static string ToString(Protocol protocol)
    {
        return protocol switch
        {
            Protocol.EscPos => "escpos",
            _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Unsupported protocol value.")
        };
    }
}
