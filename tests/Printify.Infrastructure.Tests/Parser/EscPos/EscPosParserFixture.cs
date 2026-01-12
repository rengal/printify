using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public sealed class EscPosParserFixture : IDisposable
{
    public EscPosCommandTrieProvider TrieProvider { get; } = new EscPosCommandTrieProvider();

    public void Dispose()
    {
        // EscPosCommandTrieProvider doesn't implement IDisposable
    }
}
