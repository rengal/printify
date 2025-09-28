namespace Printify.Tokenizer.Tests.EscPos;

using Printify.TestServcies;
using Contracts;
using Contracts.Elements;
using Xunit;

public sealed class BellTests
{
    [Fact]
    public void EmitsBellOnBelCharacter()
    {
        using var context = TestServices.CreateTokenizerContext<EscPosTokenizer>();
        var session = context.Tokenizer.CreateSession();

        session.Feed(new[] { EscPosTokenizer.Bell });
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements: new Element[]
            {
                new Bell(1)
            });
    }
}
