using Mediator.Net;
using Mediator.Net.MicrosoftDependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Printify.Application.Features.Auth.Login;
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
using Printify.Infrastructure.Printing.Epl;
using Printify.Infrastructure.Printing.Epl.Renderers;
using Printify.Infrastructure.Printing.EscPos;
using Printify.Infrastructure.Printing.EscPos.Renderers;
using Printify.Infrastructure.Printing.Finalization;
using Printify.Infrastructure.Printing.Factories;
using Printify.Infrastructure.Retention;
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
        services.Configure<DocumentCleanupOptions>(configuration.GetSection("DocumentCleanup"));
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
        services.AddSingleton<DocumentRetentionCleanupService>();

        // Database
        services.AddDbContext<PrintifyDbContext>((serviceProvider, options) =>
        {
            var storageOptions = serviceProvider
                .GetRequiredService<IOptions<Storage>>()
                .Value;

            var dbRoot = ResolvePath(storageOptions.DatabasePath, "db");
            var filePath = Path.Combine(dbRoot, "printify.db");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            // The database runs in WAL mode, where Microsoft.Data.Sqlite recommends against shared cache.
            // Private cache + WAL lets readers (printers, SSE) run concurrently with retention writes.
            var connectionString = $"Data Source={filePath}";

            options.UseSqlite(connectionString);
            // Sets busy_timeout/WAL on every connection so a long retention write does not hang readers.
            options.AddInterceptors(new SqlitePragmaConnectionInterceptor());
        });

        services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();

        // Infrastructure services
        services.AddSingleton<MediaService>();
        services.AddSingleton<IMediaService>(sp => sp.GetRequiredService<MediaService>());
        services.AddSingleton<IEscPosBarcodeService>(sp => sp.GetRequiredService<MediaService>());
        services.AddSingleton<IMediaStorage, FileSystemMediaStorage>();
        services.AddSingleton<EscPosCommandTrieProvider>();
        services.AddSingleton<EplCommandTrieProvider>();
        services.AddSingleton<IPrinterBufferCoordinator, PrinterBufferCoordinator>();
        services.AddSingleton<IPrinterStatusStream, PrinterStatusStream>();
        services.AddSingleton<IPrinterRuntimeStatusStore, PrinterRuntimeStatusStore>();

        // Repositories
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IAdminWorkspaceStatisticsRepository, AdminWorkspaceStatisticsRepository>();
        services.AddScoped<IPrinterRepository, PrinterRepository>();
        services.AddScoped<IPrintJobRepository, PrintJobRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IProtocolDocumentFinalizer, EscPosDocumentFinalizer>();
        services.AddScoped<IProtocolDocumentFinalizer, EplDocumentFinalizer>();
        services.AddScoped<IDocumentFinalizationCoordinator, DocumentFinalizationCoordinator>();

        // Printer listeners
        services.AddSingleton<ITcpConnectionLog, TcpConnectionLog>();
        services.AddSingleton<IPrintJobSessionFactory, PrintJobSessionFactory>();
        services.AddSingleton<IPrintJobSessionsOrchestrator, PrintJobSessionsOrchestrator>();
        services.AddSingleton<IPrinterListenerOrchestrator, PrinterListenerOrchestrator>();
        services.AddSingleton<IPrinterListenerFactory, PrinterListenerFactory>();
        services.AddSingleton<IPrinterDocumentStream, PrinterDocumentStream>();

        // Renderer factory for protocol-specific canvas rendering
        services.AddSingleton<EscPosRenderer>();
        services.AddSingleton<EplRenderer>();
        services.AddSingleton<IRendererFactory, RendererFactory>();

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
