using Printify.Contracts;
using Printify.Contracts.Documents.Elements;
using Printify.Services.Tokenizer;
using Printify.TestServices;

namespace Printify.Tokenizer.Tests.EscPos;

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
            expectedElements:
            [
                new SetFont(1, 5, true, true)
            ]);
    }
}
