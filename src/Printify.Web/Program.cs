using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Printify.Infrastructure.Persistence;
using Printify.Web.Extensions;
using Printify.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Trust X-Forwarded-For / X-Forwarded-Proto from any local reverse proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Clear default restrictions so any loopback or private-network proxy is trusted.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
});

var app = builder.Build();

var jwtSecret = app.Configuration["Jwt:SecretKey"] ?? string.Empty;
if (jwtSecret.StartsWith("your-secret-key", StringComparison.OrdinalIgnoreCase) ||
    jwtSecret.Length < 32)
{
    Console.Error.WriteLine("FATAL: Jwt:SecretKey is not configured. Set a strong secret of at least 32 characters in appsettings.Production.json or via environment variable.");
    Environment.Exit(1);
}

var htmlRoot = Path.Combine(builder.Environment.ContentRootPath, "html");
if (!builder.Environment.IsEnvironment("Test") && Directory.Exists(htmlRoot))
{
    var defaultFiles = new DefaultFilesOptions
    {
        FileProvider = new PhysicalFileProvider(htmlRoot)
    };
    defaultFiles.DefaultFileNames.Clear();
    defaultFiles.DefaultFileNames.Add("index.html");

    app.UseDefaultFiles(defaultFiles);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(htmlRoot),
        RequestPath = "",
        OnPrepareResponse = ctx =>
        {
            if (ctx.File.Name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                ctx.File.Name.EndsWith(".woff", StringComparison.OrdinalIgnoreCase) ||
                ctx.File.Name.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=300";
            }
        }
    });
}

// Ensure SQLite schema exists on first run and apply migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
    db.Database.Migrate();
}

app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program;
