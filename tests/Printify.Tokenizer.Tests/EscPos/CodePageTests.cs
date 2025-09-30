using Printify.Contracts.Documents.Elements;

namespace Printify.Tokenizer.Tests.EscPos;

using TestServices;
using System.Collections.Generic;
using Contracts;
using Xunit;

public sealed class CodePageTests
{
    [Fact]
    public void ProcessesAllCodePagesSequentially()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        
        Assert.NotNull(context.Tokenizer);

        var session = context.Tokenizer.CreateSession();
        var expectedElements = new List<Element>();
        var sequence = 0;

        foreach (var vector in EscPosCodePageData.All)
        {
            if (vector.Command.Length > 0)
            {
                session.Feed(vector.Command);
                expectedElements.Add(new SetCodePage(++sequence, vector.CodePage));
            }

            var upperBytes = vector.Encoding.GetBytes(vector.Uppercase);
            session.Feed(upperBytes);
            session.Feed([EscPosTokenizer.Lf]);
            var expectedUpper = vector.Encoding.GetString(upperBytes);
            expectedElements.Add(new TextLine(++sequence, expectedUpper));

            var lowerBytes = vector.Encoding.GetBytes(vector.Lowercase);
            session.Feed(lowerBytes);
            session.Feed([EscPosTokenizer.Lf]);
            var expectedLower = vector.Encoding.GetString(lowerBytes);
            expectedElements.Add(new TextLine(++sequence, expectedLower));
        }

        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements);
    }
}
