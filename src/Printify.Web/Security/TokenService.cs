using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Printify.Web.Security;

internal static class TokenService
{
    internal const int DefaultExpirySeconds = 3600;
    private const string TokenSecret = "printify-dev-secret";

    internal static string IssueToken(string username, DateTimeOffset now, out long expiresAt)
    {
        expiresAt = now.AddSeconds(DefaultExpirySeconds).ToUnixTimeSeconds();
        return IssueToken(username, expiresAt);
    }

    internal static bool TryExtractUsername(HttpContext context, out string username)
    {
        username = string.Empty;
        if (!context.Request.Headers.TryGetValue("Authorization", out var headerValues))
        {
            return false;
        }

        var header = headerValues.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = header[prefix.Length..].Trim();
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        return TryValidateToken(token, out username);
    }

    internal static bool TryValidateToken(string token, out string username)
    {
        username = string.Empty;
        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        }
        catch
        {
            return false;
        }

        var parts = decoded.Split('|');
        if (parts.Length != 3)
        {
            return false;
        }

        username = parts[0];
        if (!long.TryParse(parts[1], out var expiresAt))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAt)
        {
            return false;
        }

        var data = $"{username}|{expiresAt}";
        var expectedSignature = ComputeSignature(data);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(parts[2]));
    }

    private static string IssueToken(string username, long expiresAt)
    {
        var data = $"{username}|{expiresAt}";
        var signature = ComputeSignature(data);
        var payload = $"{data}|{signature}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    private static string ComputeSignature(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data + TokenSecret);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}