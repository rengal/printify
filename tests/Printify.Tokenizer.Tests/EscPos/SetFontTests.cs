namespace Printify.Tokenizer.Tests.EscPos;

using Xunit;
using TestServices;
using Contracts;
using Contracts.Documents.Elements;

public sealed class SetFontTests
{
    [Fact]
    public void EmitsSetFontWithParsedAttributes()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
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

