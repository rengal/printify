using Printify.Infrastructure.Printing.Epl;

namespace Printify.Infrastructure.Tests.Parser.Epl;

public sealed class EplParserFixture : IDisposable
{
    public EplCommandTrieProvider TrieProvider { get; } = new EplCommandTrieProvider();

    public void Dispose()
    {
        // EplCommandTrieProvider doesn't implement IDisposable
    }
}
