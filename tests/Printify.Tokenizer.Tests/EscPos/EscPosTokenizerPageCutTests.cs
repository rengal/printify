namespace Printify.Tokenizer.Tests.EscPos;

using Printify.Contracts;
using Printify.Contracts.Elements;
using Printify.Contracts.Service;
using Xunit;

public sealed class EscPosTokenizerPageCutTests
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
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new PageCut(1)
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
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new PageCut(1)
            });
    }
}
