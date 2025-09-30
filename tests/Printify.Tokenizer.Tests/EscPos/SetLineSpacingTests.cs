namespace Printify.Tokenizer.Tests.EscPos;

using TestServices;
using Contracts;
using Contracts.Elements;
using Xunit;

public sealed class SetLineSpacingTests
{
    [Fact]
    public void EmitsSetLineSpacingForEsc3()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
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
            expectedElements:
            [
                new SetLineSpacing(1, 0x40)
            ]);
    }

    [Fact]
    public void EmitsDefaultLineSpacingForEsc2()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            0x32
        ]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new SetLineSpacing(1, 30)
            ]);
    }
}
