using Printify.TestServcies.Timing;

namespace Printify.Tokenizer.Tests.EscPos;

using Microsoft.Extensions.DependencyInjection;
using Contracts.Service;
using TestServcies;
using TestServcies.Storage;

internal static class EscPosTestHelper
{
    public static TokenizerTestContext CreateContext()
    {
        var services = TestServiceCollectionFactory.Create();
        services.AddSingleton<ITokenizer, EscPosTokenizer>();
        var provider = services.BuildServiceProvider();
        var tokenizer = provider.GetRequiredService<ITokenizer>();
        services.AddSingleton<IClockFactory, TestClockFactory>();
        var clock = provider.GetRequiredService<IClockFactory>();
        var blobStorage = (InMemoryBlobStorage)provider.GetRequiredService<IBlobStorage>();
        return new TokenizerTestContext(provider, tokenizer, blobStorage, clock);
    }

    public sealed class TokenizerTestContext : IDisposable
    {
        public TokenizerTestContext(ServiceProvider provider, ITokenizer tokenizer, InMemoryBlobStorage blobStorage, IClockFactory clockFactory)
        {
            Provider = provider;
            Tokenizer = tokenizer;
            BlobStorage = blobStorage;
            ClockFactory = clockFactory;
        }

        public ServiceProvider Provider { get; }

        public ITokenizer Tokenizer { get; }

        public InMemoryBlobStorage BlobStorage { get; }

        public IClockFactory ClockFactory { get; }

        public void Dispose()
        {
            Provider.Dispose();
        }
    }
}
