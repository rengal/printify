namespace Printify.Tokenizer.Tests.EscPos;

using TestServices;
using System.Text;
using Contracts;
using Contracts.Elements;
using Xunit;

public sealed class TextTests
{
    /// <summary>
    /// Scenario: Feeding text followed by LF should flush a single text line element into the active session.
    /// </summary>
    [Fact]
    public void FlushesTextLineOnLineFeed()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();
        var data = Encoding.ASCII.GetBytes("ABC\n");

        session.Feed(data);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new TextLine(1, "ABC")
            ]);
    }

    /// <summary>
    /// Scenario: Plain text fed once without control bytes should stay as a single text element.
    /// </summary>
    [Fact]
    public void ProducesSingleTextElementForPlainText()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();

        session.Feed(Encoding.ASCII.GetBytes("ABC"));
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new TextLine(1, "ABC")
            ]);
    }

    /// <summary>
    /// Scenario: Text appended through multiple feeds should coalesce into a single text element.
    /// </summary>
    [Fact]
    public void CoalescesSequentialPlainTextFeeds()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();

        session.Feed(Encoding.ASCII.GetBytes("A"));
        session.Feed(Encoding.ASCII.GetBytes("B"));
        session.Feed(Encoding.ASCII.GetBytes("C"));
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new TextLine(1, "ABC")
            ]);
    }

    /// <summary>
    /// Scenario: Multiple lines in a single feed should create individual text elements per line break.
    /// </summary>
    [Fact]
    public void SplitsLinesWithinSingleFeed()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();

        session.Feed(Encoding.ASCII.GetBytes("ABC\nDEF\nG"));
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new TextLine(1, "ABC"),
                new TextLine(2, "DEF"),
                new TextLine(3, "G")
            ]);
    }

    /// <summary>
    /// Scenario: Text without trailing newline should still be emitted before completion commands.
    /// </summary>
    [Fact]
    public void FlushesTrailingTextWhenCompleting()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();
        session.Feed(Encoding.ASCII.GetBytes("ABC"));

        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new TextLine(1, "ABC")
            ]);
    }

    /// <summary>
    /// Scenario: Large text buffers should remain a single text element without implicit splitting.
    /// </summary>
    [Fact]
    public void DoesNotSplitLongPlainText()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();
        var longText = new string('A', 10_000);

        session.Feed(Encoding.ASCII.GetBytes(longText));
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new TextLine(1, longText)
            ]);
    }
}
