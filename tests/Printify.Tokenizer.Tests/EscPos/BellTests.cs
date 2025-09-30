using Printify.TestServices;

namespace Printify.Tokenizer.Tests.EscPos;

using Contracts;
using Contracts.Elements;
using Xunit;

public sealed class BellTests
{
    [Fact]
    public void EmitsBellOnBelCharacter()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();

        session.Feed([EscPosTokenizer.Bell]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new Bell(1)
            ]);
    }
}
