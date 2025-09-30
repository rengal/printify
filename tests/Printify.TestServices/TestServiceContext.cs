using Microsoft.Extensions.Options;
using Printify.Contracts.Config;

namespace Printify.TestServices;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Contracts.Service;
using Listener;
using System.Net.Sockets;
using System.Net;

public sealed class TestServiceContext(ServiceProvider provider, ListenerOptions listenerOptions) : IAsyncDisposable, IDisposable
{
    public static TestServiceContext Create(BufferOptions? bufferOptions = null, Type? tokenizer = null, Type? listener = null)
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

        services.TryAddSingleton<IRecordStorage, InMemoryRecordStorage>();
        services.TryAddSingleton<IBlobStorage, InMemoryBlobStorage>();
        services.TryAddSingleton<IClockFactory, TestClockFactory>();
        
        services.AddSingleton(Options.Create(bufferOptions));
        services.AddSingleton(Options.Create(listenerOptions));

        if (tokenizer is not null)
        {
            if (!typeof(ITokenizer).IsAssignableFrom(tokenizer))
                throw new ArgumentException($"{tokenizer.Name} must implement {nameof(ITokenizer)}");

            services.AddSingleton(typeof(ITokenizer), tokenizer);
        }

        if (listener is not null)
        {
            if (!typeof(IListenerService).IsAssignableFrom(listener))
                throw new ArgumentException($"{listener.Name} must implement {nameof(IListenerService)}");

            services.AddSingleton(typeof(IListenerService), listener);
        }

        return new TestServiceContext(services.BuildServiceProvider(), listenerOptions);
    }

    public ServiceProvider Provider { get; } = provider;
    public IRecordStorage RecordStorage { get; } = provider.GetRequiredService<IRecordStorage>();
    public IBlobStorage BlobStorage { get; } = provider.GetRequiredService<IBlobStorage>();

    public TestClockFactory ClockFactory { get; } = (provider.GetRequiredService<IClockFactory>() as TestClockFactory)!;
    public ITokenizer? Tokenizer { get; } = provider.GetService<ITokenizer>();
    public IListenerService? Listener { get; } = provider.GetService<IListenerService>();
    public ListenerOptions ListenerOptions { get; private set; } = listenerOptions;

    private static ListenerOptions GetListenerOptions()
    {
        return new ListenerOptions
        {
            Port = GetFreePort(),
            IdleTimeoutSeconds = 1
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
