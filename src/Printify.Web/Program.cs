using Microsoft.EntityFrameworkCore;
using Printify.Web.Middleware;
using System.Text;
using Printify.Infrastructure.Persistence;
using Printify.Web.Extensions;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Services.AddControllers();
builder.Services.AddServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

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
        RequestPath = ""
    });
}

// Ensure SQLite schema exists on first run and apply migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program;
