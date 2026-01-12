using Mediator.Net;
using Mediator.Net.MicrosoftDependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Auth.Login;
using Printify.Application.Features.Printers.Documents.View;
using Printify.Application.Interfaces;
using Printify.Application.Pipeline;
using Printify.Application.Printing;
using Printify.Application.Services;
using Printify.Domain.Config;
using Printify.Domain.Services;
using Printify.Infrastructure.Clock;
using Printify.Infrastructure.Media;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Printing;
using Printify.Infrastructure.Printing.EscPos;
using Printify.Infrastructure.Printing.Factories;
using Printify.Infrastructure.Repositories;
using Printify.Infrastructure.Security;
using Printify.Web.Infrastructure;

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
        services.Configure<Storage>(configuration.GetSection("Storage"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<RepositoryOptions>(configuration.GetSection("Repository"));

        // Allow override of repository options (useful for tests)
        if (configureRepository != null)
        {
            services.PostConfigure(configureRepository);
        }

        // Application services
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddSingleton<IClockFactory, StopwatchClockFactory>();
        services.AddSingleton<HttpContextExtensions>();
        var mediatorBuilder = new MediatorBuilder()
            .RegisterHandlers(typeof(LoginCommand).Assembly)
            .ConfigureRequestPipe(pipe =>
                pipe.AddPipeSpecification(new TransactionRequestSpecification(pipe.DependencyScope)));
        services.RegisterMediator(mediatorBuilder);

        // Security
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Services
        services.AddSingleton<IGreetingService, GreetingService>();

        // Database
        services.AddDbContext<PrintifyDbContext>((serviceProvider, options) =>
        {
            var storageOptions = serviceProvider
                .GetRequiredService<IOptions<Storage>>()
                .Value;

            var dbRoot = ResolvePath(storageOptions.DatabasePath, "db");
            var filePath = Path.Combine(dbRoot, "printify.db");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var connectionString = $"Data Source={filePath};Cache=Shared";

            options.UseSqlite(connectionString);
        });

        services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();

        // Infrastructure services
        services.AddSingleton<IMediaService, MediaService>();
        services.AddSingleton<IMediaStorage, FileSystemMediaStorage>();
        services.AddSingleton<EscPosCommandTrieProvider>();
        services.AddSingleton<IPrinterBufferCoordinator, PrinterBufferCoordinator>();
        services.AddSingleton<IPrinterStatusStream, PrinterStatusStream>();
        services.AddSingleton<IPrinterRuntimeStatusStore, PrinterRuntimeStatusStore>();

        // Repositories
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IPrinterRepository, PrinterRepository>();
        services.AddScoped<IPrintJobRepository, PrintJobRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        // Printer listeners
        services.AddSingleton<IPrintJobSessionFactory, PrintJobSessionFactory>();
        services.AddSingleton<IPrintJobSessionsOrchestrator, PrintJobSessionsOrchestrator>();
        services.AddSingleton<IPrinterListenerOrchestrator, PrinterListenerOrchestrator>();
        services.AddSingleton<IPrinterListenerFactory, PrinterListenerFactory>();
        services.AddSingleton<IPrinterDocumentStream, PrinterDocumentStream>();
        services.AddSingleton<IViewDocumentConverter, EscPosViewDocumentConverter>();

        services.AddHostedService(provider =>
            (PrinterBufferCoordinator)provider.GetRequiredService<IPrinterBufferCoordinator>());
        services.AddHostedService<PrinterListenerBootstrapper>();

        return services;
    }

    private static string ResolvePath(string? configuredPath, string fallbackSubfolder)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var expanded = Environment.ExpandEnvironmentVariables(configuredPath);
            if (expanded.StartsWith("~", StringComparison.Ordinal))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var trimmed = expanded.TrimStart('~', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                expanded = Path.Combine(home, trimmed);
            }

            return Path.GetFullPath(expanded);
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Path.GetTempPath();
        }

        return Path.Combine(appData, "virtual-printer", fallbackSubfolder);
    }
}
