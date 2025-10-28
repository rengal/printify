using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Auth.Login;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Application.Pipeline;
using Printify.Domain.Config;
using Printify.Infrastructure.Config;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Printing;
using Printify.Web.Infrastructure;
using Printify.Infrastructure.Repositories;
using Printify.Infrastructure.Security;
using ListenerOptions = Printify.Domain.Config.ListenerOptions;

namespace Printify.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RepositoryOptions>? configureRepository = null)
    {
        // Configuration
        services.Configure<ListenerOptions>(configuration.GetSection("Listener"));
        services.Configure<Page>(configuration.GetSection("Page"));
        services.Configure<Storage>(configuration.GetSection("Storage"));
        services.Configure<BufferOptions>(configuration.GetSection("Buffer"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<RepositoryOptions>(configuration.GetSection("Repository"));

        // Allow override of repository options (useful for tests)
        if (configureRepository != null)
        {
            services.PostConfigure(configureRepository);
        }

        // Application services
        services.AddHttpContextAccessor();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<LoginCommand>());
        //builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdentityGuardBehavior<,>)); //todo debugnow
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // Security
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Database
        services.AddDbContext<PrintifyDbContext>((serviceProvider, options) =>
        {
            var repositoryOptions = serviceProvider
                .GetRequiredService<IOptions<RepositoryOptions>>()
                .Value;
            options.UseSqlite(repositoryOptions.ConnectionString);
        });

        services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();

        // Repositories
        services.AddScoped<IAnonymousSessionRepository, AnonymousSessionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPrinterRepository, PrinterRepository>();

        // Printer listeners
        services.AddSingleton<IPrinterListenerOrchestrator, PrinterListenerOrchestrator>();
        services.AddSingleton<IPrinterListenerFactory, PrinterListenerFactory>();
        //services.AddHostedService<PrinterListenerBootstrapper>();

        return services;
    }
}
