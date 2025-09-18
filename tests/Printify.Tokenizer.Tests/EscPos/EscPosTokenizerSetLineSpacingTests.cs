namespace Printify.Tokenizer.Tests.EscPos;

using Printify.Contracts;
using Printify.Contracts.Elements;
using Printify.Contracts.Service;
using Xunit;

public sealed class EscPosTokenizerSetLineSpacingTests
{
    [Fact]
    public void EmitsSetLineSpacingForEsc3()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            0x33,
            0x40
        ]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new SetLineSpacing(1, 0x40)
            });
    }

    [Fact]
    public void EmitsDefaultLineSpacingForEsc2()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            0x32
        ]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new SetLineSpacing(1, 30)
            });
    }
}
