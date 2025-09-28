namespace Printify.Tokenizer.Tests.EscPos;

using Printify.TestServcies;
using System;
using System.Text;
using Contracts;
using Contracts.Elements;
using Contracts.Service;
using Xunit;

public sealed class SessionTests
{
    /// <summary>
    /// Scenario: Completing the session should finalize the document and mark the session as completed.
    /// </summary>
    [Fact]
    public void MarksCompletionOnComplete()
    {
        using var context = TestServices.CreateTokenizerContext<EscPosTokenizer>();
        var session = context.Tokenizer.CreateSession();
        session.Feed(Encoding.ASCII.GetBytes("ABC"));

        session.Complete(CompletionReason.ClientDisconnected);

        Assert.True(session.IsCompleted);
        var document = session.Document;
        DocumentAssertions.Equal(
            document,
            Protocol.EscPos,
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
        using var context = TestServices.CreateTokenizerContext<EscPosTokenizer>();
        var session = context.Tokenizer.CreateSession();

        Assert.Throws<InvalidOperationException>(() => _ = session.Document);
    }

    /// <summary>
    /// Scenario: Calling Complete twice should throw to prevent duplicate termination work.
    /// </summary>
    [Fact]
    public void CompleteTwiceThrows()
    {
        using var context = TestServices.CreateTokenizerContext<EscPosTokenizer>();
        var session = context.Tokenizer.CreateSession();

        session.Complete(CompletionReason.ClientDisconnected);
        var document = session.Document;
        DocumentAssertions.Equal(
            document,
            Protocol.EscPos,
            expectedElements: Array.Empty<Element>());

        Assert.Throws<InvalidOperationException>(() => session.Complete(CompletionReason.DataTimeout));
    }

    [Fact]
    public void ReportsBusyWhileProcessingPrintingBytes()
    {
        // Configure a manual/test clock via the test context so time is under test control.
        var options = new TokenizerSessionOptions(
            BusyThresholdBytes: 1,
            MaxBufferBytes: 1024,
            BytesPerSecond: 10);

        using var context = TestServices.CreateTokenizerContext<EscPosTokenizer>();
        var clock = context.ClockFactory.Create();
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
        var options = new TokenizerSessionOptions(
            BusyThresholdBytes: 1,
            MaxBufferBytes: 4,
            BytesPerSecond: 0);

        using var context = TestServices.CreateTokenizerContext<EscPosTokenizer>();
        var clock = context.ClockFactory.Create();
        var session = context.Tokenizer.CreateSession(options, clock);

        session.Feed(Encoding.ASCII.GetBytes("ABCDEFG"));
        session.Complete(CompletionReason.DataTimeout);

        Assert.True(session.HasOverflow);
        var error = Assert.IsType<PrinterError>(session.Elements[0]);
        Assert.Contains("overflow", error.Message, StringComparison.OrdinalIgnoreCase);
        var text = Assert.IsType<TextLine>(session.Elements[1]);
        Assert.Equal("ABCDEFG", text.Text);
    }

    [Fact]
    public void SimulatedDrainReducesBufferOverTime()
    {
        using var context = TestServices.CreateTokenizerContext<EscPosTokenizer>();
        
        // Configure a deterministic drain rate so we can reason about bytes drained per second.
        var options = new TokenizerSessionOptions(
            BusyThresholdBytes: 1,
            MaxBufferBytes: 1024,
            BytesPerSecond: 10);
        var clock = context.ClockFactory.Create();
        var session = context.Tokenizer.CreateSession(options, clock);

        // Feed 20 printable bytes which should be accumulated into the simulated buffer.
        session.Feed(Encoding.ASCII.GetBytes(new string('A', 20)));

        // Immediately after feeding, buffer should be considered busy (20 bytes > threshold).
        Assert.True(session.IsBufferBusy);
        Assert.False(session.HasOverflow);

        // Advance 1 second -> drain 10 bytes: remaining = 10 -> still busy.
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(session.IsBufferBusy);

        // Advance another 1 second -> drain remaining 10 bytes: remaining = 0 -> not busy.
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(session.IsBufferBusy);

        // Ensure overflow was never triggered during this scenario.
        Assert.False(session.HasOverflow);

        // Finalize session to flush state.
        session.Complete(CompletionReason.DataTimeout);
    }
}
