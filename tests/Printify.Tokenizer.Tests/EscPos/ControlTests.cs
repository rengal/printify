using Printify.Domain.Config;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Printify.Services.Tokenizer;

namespace Printify.Tokenizer.Tests.EscPos;

using Xunit;
using TestServices;
using Printify.Domain.PrintJobs;

public sealed class ControlTests
{
    /// <summary>
    /// Scenario: "ESC i" sequence should emit a page cut even without newline before it.
    /// </summary>
    [Fact]
    public void EmitsPageCutForEscSequence()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();
        byte[] data =
        [
            EscPosTokenizer.Esc,
            (byte)'i'
        ];

        session.Feed(data);
        Assert.False(session.IsCompleted);

        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new Pagecut(1)
            ]);
    }

    /// <summary>
    /// Scenario: GS 0x61 n should emit a printer status element containing the byte.
    /// </summary>
    [Fact]
    public void EmitsPrinterStatusForGsSequence()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();
        byte[] data =
        [
            EscPosTokenizer.Gs,
            0x61,
            0x42
        ];

        session.Feed(data);
        Assert.False(session.IsCompleted);

        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new PrinterStatus(1, 0x42, null)
            ]);
    }

    /// <summary>
    /// Scenario: GS V 66 00 should be recognized as a page cut like the client driver implementation.
    /// </summary>
    [Fact]
    public void EmitsPageCutForGsVSequence()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();
        byte[] data =
        [
            EscPosTokenizer.Gs,
            0x56,
            0x42,
            0x00
        ];

        session.Feed(data);
        Assert.False(session.IsCompleted);

        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new Pagecut(1)
            ]);
    }

    /// <summary>
    /// Scenario: ESC @ should reset the printer state.
    /// </summary>
    [Fact]
    public void EmitsResetForEscAtSequence()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));

        Assert.NotNull(context.Tokenizer);

        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            0x40
        ]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new ResetPrinter(1)
            ]);
    }

    /// <summary>
    /// Scenario: ESC E toggles emphasized text (bold) on and off.
    /// </summary>
    [Fact]
    public void EmitsSetBoldModeToggleForEscE()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));

        Assert.NotNull(context.Tokenizer);

        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            (byte)'E',
            0x01,
            EscPosTokenizer.Esc,
            (byte)'E',
            0x00
        ]);

        Assert.Collection(
            session.Elements,
            element =>
            {
                var bold = Assert.IsType<SetBoldMode>(element);
                Assert.True(bold.IsEnabled);
            },
            element =>
            {
                var bold = Assert.IsType<SetBoldMode>(element);
                Assert.False(bold.IsEnabled);
            });

        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new SetBoldMode(1, true),
                new SetBoldMode(2, false)
            ]);
    }

    /// <summary>
    /// Scenario: ESC - toggles underline mode on and off.
    /// </summary>
    [Fact]
    public void EmitsSetUnderlineModeToggleForEscDash()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            0x2D,
            0x01,
            EscPosTokenizer.Esc,
            0x2D,
            0x00
        ]);

        Assert.Collection(
            session.Elements,
            element =>
            {
                var underline = Assert.IsType<SetUnderlineMode>(element);
                Assert.True(underline.IsEnabled);
            },
            element =>
            {
                var underline = Assert.IsType<SetUnderlineMode>(element);
                Assert.False(underline.IsEnabled);
            });

        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new SetUnderlineMode(1, true),
                new SetUnderlineMode(2, false)
            ]);
    }

    /// <summary>
    /// Scenario: GS B toggles reverse (white-on-black) mode on and off.
    /// </summary>
    [Fact]
    public void EmitsSetReverseModeToggleForGsB()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));

        Assert.NotNull(context.Tokenizer);

        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Gs,
            0x42,
            0x01,
            EscPosTokenizer.Gs,
            0x42,
            0x00
        ]);

        Assert.Collection(
            session.Elements,
            element =>
            {
                var reverse = Assert.IsType<SetReverseMode>(element);
                Assert.True(reverse.IsEnabled);
            },
            element =>
            {
                var reverse = Assert.IsType<SetReverseMode>(element);
                Assert.False(reverse.IsEnabled);
            });

        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements:
            [
                new SetReverseMode(1, true),
                new SetReverseMode(2, false)
            ]);
    }

    /// <summary>
    /// Scenario: Simulated buffer overflow is reported when the accumulated printed bytes exceed MaxBufferBytes.
    /// </summary>
    [Fact]
    public void TriggersOverflowWhenMaxBufferExceeded()
    {
        // Create session with a very small max buffer and no drain to force overflow deterministically.
        var bufferOptions = new BufferOptions
        {
            MaxCapacity = 10,
            DrainRate = 0,
            BusyThreshold = 1
        };

        using var context = TestServiceContext.Create(bufferOptions: bufferOptions, tokenizer: typeof(EscPosTokenizer));

        var session = context.Tokenizer.CreateSession();

        // Feed exactly MaxBufferBytes printable bytes one at a time — should NOT trigger overflow yet.
        for (var i = 0; i < 10; i++)
        {
            session.Feed([(byte)'A']);
            // Ensure overflow hasn't been triggered while at or below the limit.
            Assert.False(session.HasOverflow);
            // Ensure no PrinterError element was emitted yet.
            Assert.DoesNotContain(session.Elements, e => e is PrinterError);
        }

        // Feed a single additional printable byte to exceed the configured MaxBufferBytes.
        session.Feed([(byte)'A']);

        // Now overflow should have been detected and an error element added.
        Assert.True(session.HasOverflow);
        Assert.Contains(session.Elements, e => e is PrinterError);

        // Finalize session to flush final state.
        session.Complete(CompletionReason.DataTimeout);
    }
}

