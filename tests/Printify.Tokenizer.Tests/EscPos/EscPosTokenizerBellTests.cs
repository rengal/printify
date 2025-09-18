namespace Printify.Tokenizer.Tests.EscPos;

using Printify.Contracts;
using Printify.Contracts.Elements;
using Printify.Contracts.Service;
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
