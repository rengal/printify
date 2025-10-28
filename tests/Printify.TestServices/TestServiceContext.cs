using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Printify.Application.Interfaces;
using Printify.Infrastructure.Config;
using Printify.Infrastructure.Persistence;
using System.Net;
using System.Net.Sockets;
using ListenerOptions = Printify.Domain.Config.ListenerOptions;

namespace Printify.TestServices;

public sealed class TestServiceContext(ServiceProvider provider, ListenerOptions listenerOptions)
    : IAsyncDisposable, IDisposable
{
    private const string InMemoryConnectionStringFormat = "Data Source=file:{0}?mode=memory&cache=shared";

    public static AuthControllerTestContext CreateForAuthControllerTest(WebApplicationFactory<Program> factory)
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

                services.RemoveAll<SqliteConnection>();
                services.RemoveAll<DbContextOptions<PrintifyDbContext>>();
                services.RemoveAll<IUnitOfWork>();

                services.AddSingleton(connection);
                services.AddDbContext<PrintifyDbContext>((_, options) => options.UseSqlite(connection));
                services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();
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

        return new AuthControllerTestContext(configuredFactory, connection, GetListenerOptions());
    }

    public ServiceProvider Provider { get; } = provider;

    public ListenerOptions ListenerOptions { get; } = listenerOptions;

    private static ListenerOptions GetListenerOptions()
    {
        return new ListenerOptions
        {
            Port = GetFreePort(),
            IdleTimeoutInMs = 1000
        };
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    public ValueTask DisposeAsync()
    {
        return Provider.DisposeAsync();
    }

    public void Dispose()
    {
        Provider.Dispose();
    }

    public sealed class AuthControllerTestContext : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly HttpClient client;

        internal AuthControllerTestContext(WebApplicationFactory<Program> factory, SqliteConnection connection,
            ListenerOptions listenerOptions)
        {
            Factory = factory;
            this.connection = connection;
            ListenerOptions = listenerOptions;
            client = factory.CreateClient();
        }

        public WebApplicationFactory<Program> Factory { get; }

        public HttpClient Client => client;

        public ListenerOptions ListenerOptions { get; }

        public AsyncServiceScope CreateScope() => Factory.Services.CreateAsyncScope();

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
                // already disposed elsewhere � ignore
            }

            await Factory.DisposeAsync().ConfigureAwait(false);
        }
    }
}

