namespace Printify.Infrastructure.Cryptography;

using System.Security.Cryptography;

internal static class Sha256Checksum
{
    internal static string ComputeLowerHex(ReadOnlySpan<byte> content)
    {
        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
