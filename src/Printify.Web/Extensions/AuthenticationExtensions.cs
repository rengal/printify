using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Printify.Web.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection("Jwt");

        // Log JWT configuration values for debugging
        var secretKey = jwt["SecretKey"];
        var issuer = jwt["Issuer"];
        var audience = jwt["Audience"];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[JWT] Authentication Failed: {context.Exception.Message}");
                        System.Diagnostics.Debug.WriteLine($"[JWT] Exception Type: {context.Exception.GetType().Name}");
                        if (context.Exception.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[JWT] Inner Exception: {context.Exception.InnerException.Message}");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        System.Diagnostics.Debug.WriteLine("[JWT] Token validated successfully");
                        System.Diagnostics.Debug.WriteLine($"[JWT] Claims count: {context.Principal?.Claims?.Count() ?? 0}");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
                        System.Diagnostics.Debug.WriteLine($"[JWT] Token received (first 20 chars): {token?.Substring(0, Math.Min(20, token?.Length ?? 0))}...");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[JWT] Challenge triggered: {context.Error}, {context.ErrorDescription}");
                        return Task.CompletedTask;
                    }
                };

                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });

        services.AddAuthorization();
        return services;
    }
}