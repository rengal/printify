using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Printify.Application.Interfaces;
using Printify.Infrastructure.Config;

namespace Printify.Infrastructure.Security;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions opts;
    private readonly SymmetricSecurityKey signingKey;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        opts = options.Value ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(opts.SecretKey))
            throw new InvalidOperationException("JwtOptions.SecretKey must be configured.");

        var keyBytes = Encoding.UTF8.GetBytes(opts.SecretKey);
        if (keyBytes.Length < 32)
            throw new InvalidOperationException("JwtOptions.SecretKey should be at least 32 bytes for HS256.");

        signingKey = new SymmetricSecurityKey(keyBytes);
    }

    public string GenerateToken(Guid? userId, Guid? sessionId)
    {
        if (userId is null && sessionId is null)
            throw new ArgumentException("Either userId or sessionId must be provided.");

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D")),
            new(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        if (userId.HasValue)
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString("D")));

        if (sessionId.HasValue)
            claims.Add(new Claim("sessionId", sessionId.Value.ToString()));

        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires = now.AddSeconds(opts.ExpiresInSeconds);

        var token = new JwtSecurityToken(
            issuer: opts.Issuer,
            audience: opts.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
