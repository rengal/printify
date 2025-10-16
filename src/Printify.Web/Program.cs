using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Auth.Login;
using Printify.Application.Interfaces;
using Printify.Application.Pipeline;
using Printify.Domain.Config;
using Printify.Infrastructure.Config;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Repositories;
using Printify.Infrastructure.Security;
using Printify.Web.Extensions;
using Printify.Web.Middleware;
using ListenerOptions = Printify.Domain.Config.ListenerOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("config.json", optional: false, reloadOnChange: true);

builder.Services.Configure<ListenerOptions>(builder.Configuration.GetSection("Listener"));
builder.Services.Configure<Page>(builder.Configuration.GetSection("Page"));
builder.Services.Configure<Storage>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<BufferOptions>(builder.Configuration.GetSection("Buffer"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<RepositoryOptions>(builder.Configuration.GetSection("Repository"));

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<LoginCommand>());
//builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdentityGuardBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddDbContext<PrintifyDbContext>((serviceProvider, options) =>
{
    var repositoryOptions = serviceProvider
        .GetRequiredService<IOptions<RepositoryOptions>>()
        .Value;
    options.UseSqlite(repositoryOptions.ConnectionString);
});
builder.Services.AddScoped<SqliteConnectionManager>();
builder.Services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();
builder.Services.AddScoped<IAnonymousSessionRepository, AnonymousSessionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();

public partial class Program;
