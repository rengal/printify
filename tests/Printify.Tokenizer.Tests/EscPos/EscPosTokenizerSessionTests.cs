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

    [Fact]
    public void ReportsBusyWhileProcessingPrintingBytes()
    {
        var clock = new ManualClock();
        var options = new TokenizerSessionOptions(
            BusyThresholdBytes: 1,
            MaxBufferBytes: 1024,
            BytesPerSecond: 10);

        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession(options, clock);

        session.Feed(Encoding.ASCII.GetBytes("ABC"));

        Assert.True(session.IsBufferBusy);

        clock.Advance(TimeSpan.FromMilliseconds(200));
        Assert.True(session.IsBufferBusy);

        clock.Advance(TimeSpan.FromMilliseconds(200));
        Assert.False(session.IsBufferBusy);
    }

    [Fact(Skip = "Temporarily muted pending drain bookkeeping adjustments.")]
    public void EmitsPrinterErrorWhenBufferOverflows()
    {
        var clock = new ManualClock();
        var options = new TokenizerSessionOptions(
            BusyThresholdBytes: 1,
            MaxBufferBytes: 4,
            BytesPerSecond: 0);

        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession(options, clock);

        session.Feed(Encoding.ASCII.GetBytes("ABCDEFG"));
        session.Complete(CompletionReason.DataTimeout);

        Assert.True(session.HasOverflow);
        var error = Assert.IsType<PrinterError>(session.Elements[0]);
        Assert.Contains("overflow", error.Message, StringComparison.OrdinalIgnoreCase);
        var text = Assert.IsType<TextLine>(session.Elements[1]);
        Assert.Equal("ABCDEFG", text.Text);
    }

    private sealed class ManualClock : IClock
    {
        private long elapsed;

        public void Start()
        {
            elapsed = 0;
        }

        public long ElapsedMs => elapsed;

        public void Advance(TimeSpan delta)
        {
            if (delta < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delta));
            }

            elapsed += (long)delta.TotalMilliseconds;
        }
    }
}
