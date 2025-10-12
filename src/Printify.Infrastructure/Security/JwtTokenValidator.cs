using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Printify.Application.Interfaces;
using Printify.Infrastructure.Config;

namespace Printify.Infrastructure.Security;

public sealed class JwtTokenValidator : ITokenValidator
{
    private readonly TokenValidationParameters _parameters;

    public JwtTokenValidator(IOptions<JwtOptions> options)
    {
        var opts = options.Value;
        _parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = opts.Issuer,
            ValidateAudience = true,
            ValidAudience = opts.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    }

    public ClaimsPrincipal Validate(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, _parameters, out _);
        return principal;
    }
}
