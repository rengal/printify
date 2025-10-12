using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Printify.Services.Tokenizer;

namespace Printify.Tokenizer.Tests.EscPos;

using Printify.Domain.PrintJobs;
using TestServices;
using Xunit;

public sealed class PagecutTests
{
    [Fact]
    public void EmitsPageCutForEscSequence()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            (byte)'i'
        ]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new Pagecut(1)
            ]);
    }

    [Fact]
    public void EmitsPageCutForGsSequence()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
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
            expectedElements:
            [
                new Pagecut(1)
            ]);
    }
}
