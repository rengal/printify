using Printify.Domain.Printers;

namespace Printify.Domain.Mapping;

public static class ProtocolMapper
{
    public static Protocol ParseProtocol(string protocol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        if (protocol.ToLower() == "escpos")
        {
            return Protocol.EscPos;
        }

        throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Protocol is not supported.");
    }

    public static string ToString(Protocol protocol)
    {
        return protocol switch
        {
            Protocol.EscPos => "escpos",
            _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Unsupported protocol value.")
        };
    }
}
