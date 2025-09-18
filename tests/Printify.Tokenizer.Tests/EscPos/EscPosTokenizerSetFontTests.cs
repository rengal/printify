namespace Printify.Tokenizer.Tests.EscPos;

using Printify.Contracts;
using Printify.Contracts.Elements;
using Printify.Contracts.Service;
using Xunit;

public sealed class EscPosTokenizerSetFontTests
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
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new SetFont(1, 5, true, true)
            });
    }
}

