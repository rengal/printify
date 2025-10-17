using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Printify.Application.Interfaces;
using Printify.Domain.Config;
using Printify.Domain.Services;
using Printify.Infrastructure.Config;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Repositories;
using Printify.Web.Controllers;
using System.Net;
using System.Net.Sockets;
using ListenerOptions = Printify.Domain.Config.ListenerOptions;

namespace Printify.TestServices;

public sealed class TestServiceContext(ServiceProvider provider, ListenerOptions listenerOptions) : IAsyncDisposable, IDisposable
{
    private const string InMemoryConnectionString = "Data Source=:memory:;Cache=Shared";

    public static TestServiceContext Create(BufferOptions? bufferOptions = null, JwtOptions? jwtOptions = null, Type? tokenizer = null, Type? listener = null, string? connectionString = null)
    {
        var services = new ServiceCollection();

        var listenerOptions = GetListenerOptions();
        var resolvedConnectionString = connectionString ?? InMemoryConnectionString;

        bufferOptions ??= new BufferOptions
        {
            BusyThreshold = null,
            MaxCapacity = null,
            DrainRate = null
        };

        jwtOptions ??= new JwtOptions
        {
            Issuer = "printify-auth",
            Audience = "printify-api",
            ExpiresInSeconds = 100,
            SecretKey = new string('0', 100)
        };

        services.TryAddSingleton<IClockFactory, TestClockFactory>();

        services.AddSingleton(Options.Create(bufferOptions));
        services.AddSingleton(Options.Create(listenerOptions));
        services.AddSingleton(Options.Create(jwtOptions));
        services.AddSingleton(Options.Create(new RepositoryOptions
        {
            ConnectionString = resolvedConnectionString
        }));
        services.AddSingleton(provider =>
        {
            var sqlConnection = new SqliteConnection(resolvedConnectionString);
            sqlConnection.Open();
            return sqlConnection;
        });
        services.AddDbContext<PrintifyDbContext>((serviceProvider, options) =>
        {
            var connection = serviceProvider.GetRequiredService<SqliteConnection>();
            options.UseSqlite(connection);
        });
        services.AddScoped<IAnonymousSessionRepository, AnonymousSessionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPrinterRepository, PrinterRepository>();

        var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        return new TestServiceContext(provider, listenerOptions);
    }

    public static AuthControllerTestContext CreateForAuthControllerTest(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var connection = new SqliteConnection(InMemoryConnectionString);
        connection.Open();

        var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<RepositoryOptions>(options => options.ConnectionString = InMemoryConnectionString);

                services.RemoveAll<SqliteConnection>();
                services.RemoveAll<DbContextOptions<PrintifyDbContext>>();
                services.RemoveAll<IUnitOfWork>();

                services.AddSingleton(connection);
                services.AddDbContext<PrintifyDbContext>((serviceProvider, options) => options.UseSqlite(connection));
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

        internal AuthControllerTestContext(WebApplicationFactory<Program> factory, SqliteConnection connection, ListenerOptions listenerOptions)
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
            client.Dispose();
            Factory.Dispose();
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task BeginTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

