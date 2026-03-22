using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Printify.Application.Interfaces;
using Printify.Domain.Config;
using Printify.Infrastructure.Persistence;
using Printify.Domain.Services;
using Printify.Application.Printing;
using Printify.TestServices.Printing;
using Printify.Web.Infrastructure;

namespace Printify.TestServices;

public sealed class TestServiceContext(ServiceProvider provider)
    : IAsyncDisposable, IDisposable
{
    public static ControllerTestContext CreateForControllerTest(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        // Named shared in-memory SQLite database, unique per test environment.
        // A single physical connection is kept open for the lifetime of the test to prevent
        // the in-memory database from being dropped when all connections close.
        // EF Core's scoped DbContext instances open their own connections to the same named file,
        // so we set Busy Timeout to let them retry briefly instead of immediately failing with
        // "database is locked" when SQLite function registration races between concurrent scopes.
        var dbId = Guid.NewGuid().ToString("N");
        var connectionString = $"Data Source=file:{dbId}?mode=memory&cache=shared;Pooling=False";
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA busy_timeout=5000;";
            cmd.ExecuteNonQuery();
        }

        var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            // Avoid static file providers and file watchers during tests.
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Information);
                });

                services.PostConfigure<RepositoryOptions>(options => options.ConnectionString = connectionString);
                var tempStorageRoot = Path.Combine(Path.GetTempPath(), "printify-tests", Guid.NewGuid().ToString("N"));
                services.PostConfigure<Storage>(options => options.MediaRootPath = tempStorageRoot);

                services.RemoveAll<SqliteConnection>();
                services.RemoveAll<DbContextOptions<PrintifyDbContext>>();
                services.RemoveAll<IUnitOfWork>();
                services.RemoveAll<IClockFactory>();
                //services.RemoveAll<IPrinterBufferCoordinator>();
                services.RemoveAll<IPrinterListenerFactory>();
                services.RemoveAll<ITestPortRegistry>();
                // Avoid starting printer listeners in test environment.
                var descriptors = services
                    .Where(d => d.ImplementationType != null &&
                                typeof(IPrinterListenerBootstrapper).IsAssignableFrom(d.ImplementationType))
                    .ToList();

                foreach (var descriptor in descriptors)
                    services.Remove(descriptor);


                services.AddSingleton(connection);
                services.AddDbContext<PrintifyDbContext>((_, options) =>
                    options.UseSqlite(connectionString, sqlite =>
                        sqlite.CommandTimeout(10)));
                services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();
                services.AddSingleton<IClockFactory, TestClockFactory>();
                services.AddSingleton<ITestPortRegistry, TestPortRegistry>();
                services.AddSingleton<IPrinterListenerFactory, TestPrinterListenerFactory>();
            });
        });

        using (var scope = configuredFactory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
            // Drops the database if it exists
            context.Database.EnsureDeleted();
            // Creates the database and schema if they don't exist
            context.Database.EnsureCreated();
        }

        return new ControllerTestContext(configuredFactory, connection);
    }

    public ServiceProvider Provider { get; } = provider;

    public ValueTask DisposeAsync()
    {
        return Provider.DisposeAsync();
    }

    public void Dispose()
    {
        Provider.Dispose();
    }

    public sealed class ControllerTestContext : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly HttpClient client;

        internal ControllerTestContext(WebApplicationFactory<Program> factory, SqliteConnection connection)
        {
            Factory = factory;
            this.connection = connection;
            client = factory.CreateClient();
        }

        public WebApplicationFactory<Program> Factory { get; }

        public HttpClient Client => client;

        public AsyncServiceScope CreateScope() => Factory.Services.CreateAsyncScope();

        public IPrinterListenerOrchestrator PrinterListenerOrchestrator =>
            Factory.Services.GetRequiredService<IPrinterListenerOrchestrator>();

        public IPrintJobSessionsOrchestrator PrintJobSessionsOrchestrator =>
            Factory.Services.GetRequiredService<IPrintJobSessionsOrchestrator>();

        public IPrinterDocumentStream DocumentStream =>
            Factory.Services.GetRequiredService<IPrinterDocumentStream>();

        public IPrinterStatusStream StatusStream =>
            Factory.Services.GetRequiredService<IPrinterStatusStream>();

        public IClockFactory ClockFactory => Factory.Services.GetRequiredService<IClockFactory>();

        public HttpClient CreateClient()
        {
            return Factory.CreateClient(new WebApplicationFactoryClientOptions());
        }

        public async ValueTask DisposeAsync()
        {
            // Dispose leaves first
            client.Dispose();

            // Dispose the connection BEFORE the factory, but only if we own it
            try
            {
                // Close first; CloseAsync is safe even if already closed.
                if (connection.State != System.Data.ConnectionState.Closed)
                    await connection.CloseAsync().ConfigureAwait(false);

                await connection.DisposeAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // already disposed elsewhere - ignore
            }

            try
            {
                using var scope = Factory.Services.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<IOptions<Storage>>().Value;
                if (!string.IsNullOrWhiteSpace(storage.MediaRootPath) && Directory.Exists(storage.MediaRootPath))
                {
                    Directory.Delete(storage.MediaRootPath, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup
            }

            await Factory.DisposeAsync().ConfigureAwait(false);
        }
    }
}
