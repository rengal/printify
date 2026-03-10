using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Checks whether an IP address is covered by a whitelist of CIDR ranges or exact IPs.
/// Entries are newline- or comma-separated; blank lines and # comments are ignored.
/// Supports IPv4 and IPv6 (including IPv4-mapped IPv6 addresses like ::ffff:1.2.3.4).
/// </summary>
internal static class IpWhitelistMatcher
{
    internal static bool IsAllowed(string clientAddress, string whitelistEntries)
    {
        // Strip port if present (e.g. "1.2.3.4:12345" or "[::1]:12345").
        var clientIp = ParseClientIp(clientAddress);
        if (clientIp is null)
            return false;

        // Normalise to IPv4 when possible (unwrap IPv4-mapped IPv6).
        if (clientIp.IsIPv4MappedToIPv6)
            clientIp = clientIp.MapToIPv4();

        foreach (var line in SplitEntries(whitelistEntries))
        {
            if (MatchesEntry(clientIp, line))
                return true;
        }

        return false;
    }

    private static IPAddress? ParseClientIp(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        // "host:port" or "[ipv6]:port"
        var hostPart = address;
        var lastColon = address.LastIndexOf(':');
        if (lastColon > 0)
        {
            // IPv6 literal has more than one colon — only strip port for IPv4.
            var withoutPort = address[..lastColon];
            if (IPAddress.TryParse(withoutPort, out var v4) && v4.AddressFamily == AddressFamily.InterNetwork)
                hostPart = withoutPort;
            else if (address.StartsWith('[') && address.Contains("]:"))
                hostPart = address[1..address.IndexOf(']')];
        }

        return IPAddress.TryParse(hostPart, out var ip) ? ip : null;
    }

    private static IEnumerable<string> SplitEntries(string entries)
    {
        foreach (var part in entries.Split(['\n', '\r', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var trimmed = part.TrimStart();
            if (!trimmed.StartsWith('#') && trimmed.Length > 0)
                yield return trimmed;
        }
    }

    private static bool MatchesEntry(IPAddress clientIp, string entry)
    {
        // CIDR notation
        var slashIndex = entry.IndexOf('/');
        if (slashIndex >= 0)
        {
            var networkPart = entry[..slashIndex];
            if (!int.TryParse(entry[(slashIndex + 1)..], out var prefixLen))
                return false;

            if (!IPAddress.TryParse(networkPart, out var networkAddr))
                return false;

            return IsInCidr(clientIp, networkAddr, prefixLen);
        }

        // Exact match
        return IPAddress.TryParse(entry, out var exact) && clientIp.Equals(exact);
    }

    private static bool IsInCidr(IPAddress address, IPAddress network, int prefixLength)
    {
        if (address.AddressFamily != network.AddressFamily)
        {
            // Allow matching IPv4 client against IPv4-mapped IPv6 network and vice-versa.
            if (address.AddressFamily == AddressFamily.InterNetwork && network.AddressFamily == AddressFamily.InterNetworkV6)
                address = address.MapToIPv6();
            else if (address.AddressFamily == AddressFamily.InterNetworkV6 && network.AddressFamily == AddressFamily.InterNetwork)
                network = network.MapToIPv6();
            else
                return false;
        }

        var addrBytes = address.GetAddressBytes();
        var netBytes = network.GetAddressBytes();

        if (addrBytes.Length != netBytes.Length)
            return false;

        var totalBits = addrBytes.Length * 8;
        if (prefixLength < 0 || prefixLength > totalBits)
            return false;

        // Compare only the prefix bits.
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (addrBytes[i] != netBytes[i])
                return false;
        }

        if (remainingBits > 0)
        {
            var mask = (byte)(0xFF << (8 - remainingBits));
            if ((addrBytes[fullBytes] & mask) != (netBytes[fullBytes] & mask))
                return false;
        }

        return true;
    }
}
