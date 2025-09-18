namespace Printify.Tokenizer.Tests.EscPos;

using System;
using System.Text;
using Printify.Contracts;
using Printify.Contracts.Elements;
using Printify.Contracts.Service;
using Xunit;

public sealed class EscPosTokenizerSessionTests
{
    /// <summary>
    /// Scenario: Completing the session should finalize the document and mark the session as completed.
    /// </summary>
    [Fact]
    public void MarksCompletionOnComplete()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();
        session.Feed(Encoding.ASCII.GetBytes("ABC"));

        session.Complete(CompletionReason.ClientDisconnected);

        Assert.True(session.IsCompleted);
        var document = session.Document;
        DocumentAssertions.Equal(
            document,
            Protocol.EscPos,
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new TextLine(1, "ABC")
            });
    }

    /// <summary>
    /// Scenario: Accessing Document before completion should throw.
    /// </summary>
    [Fact]
    public void DocumentIsUnavailableBeforeCompletion()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();

        Assert.Throws<InvalidOperationException>(() => _ = session.Document);
    }

    /// <summary>
    /// Scenario: Calling Complete twice should throw to prevent duplicate termination work.
    /// </summary>
    [Fact]
    public void CompleteTwiceThrows()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();

        session.Complete(CompletionReason.ClientDisconnected);
        var document = session.Document;
        DocumentAssertions.Equal(
            document,
            Protocol.EscPos,
            expectedSourceIp: null,
            expectedElements: Array.Empty<Element>());

        Assert.Throws<InvalidOperationException>(() => session.Complete(CompletionReason.DataTimeout));
    }
}
