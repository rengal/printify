using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Sockets;
using System.Net;
using Microsoft.Extensions.Options;
using Printify.Domain.Config;
using Printify.Domain.Services;
using Printify.Infrastructure.Config;
using Printify.Application.Interfaces;
using Printify.Web.Controllers;
using ListenerOptions = Printify.Domain.Config.ListenerOptions;

namespace Printify.TestServices;

public sealed class TestServiceContext(ServiceProvider provider, ListenerOptions listenerOptions) : IAsyncDisposable, IDisposable
{
    private const string InMemoryConnectionString = "Data Source=PrintifyTests;Mode=Memory;Cache=Shared";

    public static TestServiceContext Create(BufferOptions? bufferOptions = null, JwtOptions? jwtOptions = null, Type? tokenizer = null, Type? listener = null, string? connectionString = null)
    {
        var services = new ServiceCollection();

        var listenerOptions = GetListenerOptions();

        if (bufferOptions == null)
        {
            bufferOptions = new BufferOptions
            {
                BusyThreshold = null,
                MaxCapacity = null,
                DrainRate = null
            };
        }

        if (jwtOptions == null)
        {
            jwtOptions = new JwtOptions
            {
                Issuer = "printify-auth",
                Audience = "printify-api",
                ExpiresInSeconds = 100,
                SecretKey = new string('0', 100)
            };
        }

        services.TryAddSingleton<IClockFactory, TestClockFactory>();

        services.AddSingleton(Options.Create(bufferOptions));
        services.AddSingleton(Options.Create(listenerOptions));
        services.AddSingleton(Options.Create(jwtOptions));
        services.AddSingleton(Options.Create(new RepositoryOptions
        {
            ConnectionString = connectionString ?? InMemoryConnectionString
        }));

        return new TestServiceContext(services.BuildServiceProvider(), listenerOptions);
    }

    public static TestServiceContext CreateForAuthControllerTest()
    {
        var services = new ServiceCollection();

        var listenerOptions = GetListenerOptions();
        var bufferOptions = new BufferOptions
        {
            BusyThreshold = null,
            MaxCapacity = null,
            DrainRate = null
        };

        var jwtOptions = new JwtOptions
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
            ConnectionString = InMemoryConnectionString
        }));

        services.AddScoped<IMediator, ThrowingMediator>();
        services.AddScoped<IJwtTokenGenerator, ThrowingJwtGenerator>();
        services.AddTransient<AuthController>();
        services.AddSingleton<IUserRepository, NullUserRepository>();
        services.AddSingleton<IPrinterRepository, NullPrinterRepository>();

        return new TestServiceContext(services.BuildServiceProvider(), listenerOptions);
    }

    public ServiceProvider Provider { get; } = provider;

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
}
