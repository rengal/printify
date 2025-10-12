namespace Printify.Infrastructure.Config;

public sealed class JwtOptions
{
    public string SecretKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public long ExpiresInSeconds { get; set; } = 3600 * 24 * 100; // 100 days
}
