namespace Printify.Tokenizer.Tests.EscPos;

using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts.Service;
using Printify.TestServcies;
using Printify.TestServcies.Storage;

internal static class EscPosTestHelper
{
    public static TokenizerTestContext CreateContext()
    {
        var services = TestServiceCollectionFactory.Create();
        services.AddSingleton<ITokenizer, EscPosTokenizer>();
        var provider = services.BuildServiceProvider();
        var tokenizer = provider.GetRequiredService<ITokenizer>();
        var blobStorage = (InMemoryBlobStorage)provider.GetRequiredService<IBlobStorage>();
        return new TokenizerTestContext(provider, tokenizer, blobStorage);
    }

    public sealed class TokenizerTestContext : IDisposable
    {
        public TokenizerTestContext(ServiceProvider provider, ITokenizer tokenizer, InMemoryBlobStorage blobStorage)
        {
            Provider = provider;
            Tokenizer = tokenizer;
            BlobStorage = blobStorage;
        }

        public ServiceProvider Provider { get; }

        public ITokenizer Tokenizer { get; }

        public InMemoryBlobStorage BlobStorage { get; }

        public void Dispose()
        {
            Provider.Dispose();
        }
    }
}
