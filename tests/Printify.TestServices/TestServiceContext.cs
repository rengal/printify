using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Printify.Application.Interfaces;
using Printify.Domain.Config;
using Printify.Infrastructure.Persistence;
using System.Net;
using System.Net.Sockets;
using Printify.Domain.Services;
using Printify.Application.Printing;
using Printify.TestServices.Printing;

namespace Printify.TestServices;

public sealed class TestServiceContext(ServiceProvider provider)
    : IAsyncDisposable, IDisposable
{
    private const string InMemoryConnectionStringFormat = "Data Source=file:{0}?mode=memory&cache=shared";

    public static ControllerTestContext CreateForControllerTest(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var dbId = Guid.NewGuid().ToString("N");
        var connectionString = string.Format(InMemoryConnectionStringFormat, dbId);
        var connection = new SqliteConnection(connectionString);
        connection.Open();


        var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<RepositoryOptions>(options => options.ConnectionString = connectionString);
                var tempStorageRoot = Path.Combine(Path.GetTempPath(), "printify-tests", Guid.NewGuid().ToString("N"));
                services.PostConfigure<Storage>(options => options.MediaRootPath = tempStorageRoot);

                services.RemoveAll<SqliteConnection>();
                services.RemoveAll<DbContextOptions<PrintifyDbContext>>();
                services.RemoveAll<IUnitOfWork>();
                services.RemoveAll<IClockFactory>();
                services.RemoveAll<IPrinterListenerFactory>();

                services.AddSingleton(connection);
                services.AddDbContext<PrintifyDbContext>((_, options) => options.UseSqlite(connection));
                services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();
                services.AddSingleton<IClockFactory, TestClockFactory>();
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
