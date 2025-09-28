namespace Printify.TestServcies;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Printify.Contracts.Service;
using Printify.Listener;
using Printify.TestServcies.Storage;
using Printify.TestServcies.Timing;
using Printify.Tokenizer;

public static class TestServices
{
    public static TestServiceContext Create(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();

        configure?.Invoke(services);

        services.TryAddSingleton<IBlobStorage, InMemoryBlobStorage>();
        services.TryAddSingleton<IRecordStorage, InMemoryRecordStorage>();
        services.TryAddSingleton<IClockFactory, TestClockFactory>();

        return new TestServiceContext(services.BuildServiceProvider());
    }

    public static TokenizerTestContext<TTokenizer> CreateTokenizerContext<TTokenizer>(Action<IServiceCollection>? configure = null)
        where TTokenizer : class, ITokenizer
    {
        var context = Create(services =>
        {
            configure?.Invoke(services);

            services.TryAddSingleton<TTokenizer>();
            services.TryAddSingleton<ITokenizer>(sp => sp.GetRequiredService<TTokenizer>());
        });

        return new TokenizerTestContext<TTokenizer>(context);
    }

    public static ListenerTestContext CreateListenerContext(Action<IServiceCollection>? configure = null)
    {
        var context = Create(services =>
        {
            configure?.Invoke(services);

            services.TryAddSingleton<ILogger<ListenerService>>(_ => NullLogger<ListenerService>.Instance);
            services.TryAddSingleton<IOptions<ListenerOptions>>(_ => Options.Create(new ListenerOptions()));
            services.TryAddSingleton<ListenerService>();
            services.TryAddSingleton<IListenerService>(sp => sp.GetRequiredService<ListenerService>());
        });

        return new ListenerTestContext(context);
    }

    public static StorageTestContext CreateStorageContext(Action<IServiceCollection>? configure = null)
    {
        var context = Create(services =>
        {
            configure?.Invoke(services);

            services.TryAddSingleton<IRecordStorage, InMemoryRecordStorage>();
        });

        return new StorageTestContext(context);
    }
}

public sealed class TestServiceContext : IAsyncDisposable, IDisposable
{
    public TestServiceContext(ServiceProvider provider)
    {
        Provider = provider;
    }

    public ServiceProvider Provider { get; }

    public IServiceScope CreateScope()
    {
        return Provider.CreateScope();
    }

    public T GetRequiredService<T>()
        where T : notnull
    {
        return Provider.GetRequiredService<T>();
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

public sealed class TokenizerTestContext<TTokenizer> : IAsyncDisposable, IDisposable
    where TTokenizer : class, ITokenizer
{
    private readonly TestServiceContext innerContext;

    public TokenizerTestContext(TestServiceContext innerContext)
    {
        this.innerContext = innerContext;
        Tokenizer = innerContext.GetRequiredService<TTokenizer>();
        BlobStorage = innerContext.GetRequiredService<IBlobStorage>();
        ClockFactory = innerContext.GetRequiredService<IClockFactory>();
    }

    public ServiceProvider Provider => innerContext.Provider;

    public TTokenizer Tokenizer { get; }

    public IBlobStorage BlobStorage { get; }

    public IClockFactory ClockFactory { get; }

    public IServiceScope CreateScope()
    {
        return innerContext.CreateScope();
    }

    public T GetRequiredService<T>()
        where T : notnull
    {
        return innerContext.GetRequiredService<T>();
    }

    public ValueTask DisposeAsync()
    {
        return innerContext.DisposeAsync();
    }

    public void Dispose()
    {
        innerContext.Dispose();
    }
}

public sealed class ListenerTestContext : IAsyncDisposable, IDisposable
{
    private readonly TestServiceContext innerContext;

    public ListenerTestContext(TestServiceContext innerContext)
    {
        this.innerContext = innerContext;
        ListenerService = innerContext.GetRequiredService<ListenerService>();
        Listener = innerContext.GetRequiredService<IListenerService>();
    }

    public ServiceProvider Provider => innerContext.Provider;

    public ListenerService ListenerService { get; }

    public IListenerService Listener { get; }

    public IServiceScope CreateScope()
    {
        return innerContext.CreateScope();
    }

    public T GetRequiredService<T>()
        where T : notnull
    {
        return innerContext.GetRequiredService<T>();
    }

    public ValueTask DisposeAsync()
    {
        return innerContext.DisposeAsync();
    }

    public void Dispose()
    {
        innerContext.Dispose();
    }
}

public sealed class StorageTestContext : IAsyncDisposable, IDisposable
{
    private readonly TestServiceContext innerContext;

    public StorageTestContext(TestServiceContext innerContext)
    {
        this.innerContext = innerContext;
        Storage = innerContext.GetRequiredService<IRecordStorage>();
    }

    public ServiceProvider Provider => innerContext.Provider;

    public IRecordStorage Storage { get; }

    public IServiceScope CreateScope()
    {
        return innerContext.CreateScope();
    }

    public T GetRequiredService<T>()
        where T : notnull
    {
        return innerContext.GetRequiredService<T>();
    }

    public ValueTask DisposeAsync()
    {
        return innerContext.DisposeAsync();
    }

    public void Dispose()
    {
        innerContext.Dispose();
    }
}
