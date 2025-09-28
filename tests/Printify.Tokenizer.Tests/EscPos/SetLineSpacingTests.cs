namespace Printify.Tokenizer.Tests.EscPos;

using Printify.TestServcies;
using Contracts;
using Contracts.Elements;
using Xunit;

public sealed class SetLineSpacingTests
{
    [Fact]
    public void EmitsSetLineSpacingForEsc3()
    {
        using var context = TestServices.CreateTokenizerContext<EscPosTokenizer>();
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
            expectedElements: new Element[]
            {
                new SetLineSpacing(1, 0x40)
            });
    }

    [Fact]
    public void EmitsDefaultLineSpacingForEsc2()
    {
        using var context = TestServices.CreateTokenizerContext<EscPosTokenizer>();
        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            0x32
        ]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements: new Element[]
            {
                new SetLineSpacing(1, 30)
            });
    }
}
