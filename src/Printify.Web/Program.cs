using System.Text;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using Printify.Application.Interfaces;
using Printify.Application.Pipeline;
using Printify.Domain.Config;
using Printify.Infrastructure.Security;
using Printify.Web.Config;
using Printify.Web.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("config.json", optional: false, reloadOnChange: true);

builder.Services.Configure<ListenerOptions>(builder.Configuration.GetSection("Listener"));
builder.Services.Configure<Page>(builder.Configuration.GetSection("Page"));
builder.Services.Configure<Storage>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<BufferOptions>(builder.Configuration.GetSection("Buffer"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
//builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdentityGuardBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();

public partial class Program;
