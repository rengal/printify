namespace Printify.Tokenizer.Tests.EscPos;

using Contracts;
using Contracts.Elements;
using Xunit;

public sealed class EscPosTokenizerBellTests
{
    [Fact]
    public void EmitsBellOnBelCharacter()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();

        session.Feed(new[] { EscPosTokenizer.Bell });
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new Bell(1)
            });
    }
}
