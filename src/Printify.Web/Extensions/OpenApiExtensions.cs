using Microsoft.OpenApi.Models;

namespace Printify.Web.Extensions;

/// <summary>
/// Swashbuckle OpenAPI/Swagger wiring for the HTTP surface of the API.
/// Note: this documents the REST endpoints only. The actual printing path is raw TCP
/// to each printer's listener (see <c>Settings.TcpListenPort</c> / <c>Settings.PublicHost</c>
/// on the create-printer response) and is therefore out of scope for OpenAPI — the
/// end-to-end flow is described in <c>doc/api-endpoints.md</c>.
/// </summary>
public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Several DTOs share a short type name across Request/Response namespaces
            // (e.g. PrinterDto, PrinterSettingsDto). Default schemaId uses the short name
            // and would collide — qualify by full name instead.
            options.CustomSchemaIds(type => type.FullName);

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Printify Web API",
                Version = "v1",
                Description = "Virtual printer management API. Printing happens over raw TCP to each "
                    + "printer's listener (Settings.TcpListenPort / Settings.PublicHost), not over HTTP."
            });

            // JWT bearer so the Swagger UI "Authorize" button works with the
            // AccessToken returned by POST /api/auth/login.
            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the AccessToken from POST /api/auth/login (without the 'Bearer ' prefix).",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            options.AddSecurityDefinition("Bearer", bearerScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [bearerScheme] = Array.Empty<string>()
            });
        });

        return services;
    }
}
