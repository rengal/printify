namespace Printify.Tokenizer.Tests.EscPos;

using System.Collections.Generic;
using System.Linq;
using Printify.Contracts;
using Printify.Contracts.Elements;
using Printify.Contracts.Service;
using Xunit;

public sealed class EscPosTokenizerCodePageTests
{
    [Fact]
    public void ProcessesAllCodePagesSequentially()
    {
        using var context = EscPosTestHelper.CreateContext();
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
            session.Feed(new[] { EscPosTokenizer.Lf });
            var expectedUpper = vector.Encoding.GetString(upperBytes);
            expectedElements.Add(new TextLine(++sequence, expectedUpper));

            var lowerBytes = vector.Encoding.GetBytes(vector.Lowercase);
            session.Feed(lowerBytes);
            session.Feed(new[] { EscPosTokenizer.Lf });
            var expectedLower = vector.Encoding.GetString(lowerBytes);
            expectedElements.Add(new TextLine(++sequence, expectedLower));
        }

        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedSourceIp: null,
            expectedElements);
    }
}
