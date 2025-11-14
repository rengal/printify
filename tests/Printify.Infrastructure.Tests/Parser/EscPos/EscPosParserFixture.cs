using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Tests.Parser.EscPos;

public sealed class EscPosParserFixture : IDisposable
{
    public IEscPosCommandTrieProvider TrieProvider { get; } = new EscPosCommandTrieProvider();

    public void Dispose()
    {
        if (TrieProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
