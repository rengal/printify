namespace Printify.Tokenizer.Tests.EscPos;

using Contracts;
using Contracts.Elements;
using Xunit;

public sealed class PagecutTests
{
    [Fact]
    public void EmitsPageCutForEscSequence()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            (byte)'i'
        ]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements: new Element[]
            {
                new Pagecut(1)
            });
    }

    [Fact]
    public void EmitsPageCutForGsSequence()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Gs,
            0x56,
            0x00
        ]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements: new Element[]
            {
                new Pagecut(1)
            });
    }
}
