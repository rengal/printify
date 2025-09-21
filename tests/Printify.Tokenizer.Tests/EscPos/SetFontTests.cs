namespace Printify.Tokenizer.Tests.EscPos;

using Contracts;
using Contracts.Elements;
using Xunit;

public sealed class SetFontTests
{
    [Fact]
    public void EmitsSetFontWithParsedAttributes()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            0x21,
            0x35
        ]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements: new Element[]
            {
                new SetFont(1, 5, true, true)
            });
    }
}

