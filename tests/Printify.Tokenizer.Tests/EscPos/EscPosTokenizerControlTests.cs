namespace Printify.Tokenizer.Tests.EscPos;

using Printify.Contracts;
using Printify.Contracts.Elements;
using Printify.Contracts.Service;
using Xunit;

public sealed class EscPosTokenizerControlTests
{
    /// <summary>
    /// Scenario: ESC i sequence should emit a page cut even without newline before it.
    /// </summary>
    [Fact]
    public void EmitsPageCutForEscSequence()
    {
        using var context = EscPosTestHelper.CreateContext();
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
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new PageCut(1)
            });
    }

    /// <summary>
    /// Scenario: GS 0x61 n should emit a printer status element containing the byte.
    /// </summary>
    [Fact]
    public void EmitsPrinterStatusForGsSequence()
    {
        using var context = EscPosTestHelper.CreateContext();
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
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new PrinterStatus(1, 0x42, null)
            });
    }

    /// <summary>
    /// Scenario: GS V 66 00 should be recognized as a page cut like the client driver implementation.
    /// </summary>
    [Fact]
    public void EmitsPageCutForGsVSequence()
    {
        using var context = EscPosTestHelper.CreateContext();
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
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new PageCut(1)
            });
    }

    /// <summary>
    /// Scenario: ESC @ should reset the printer state.
    /// </summary>
    [Fact]
    public void EmitsResetForEscAtSequence()
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            0x40
        ]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new ResetPrinter(1)
            });
    }

    /// <summary>
    /// Scenario: ESC E toggles emphasized text (bold) on and off.
    /// </summary>
    [Fact]
    public void EmitsSetBoldModeToggleForEscE()
    {
        using var context = EscPosTestHelper.CreateContext();
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
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new SetBoldMode(1, true),
                new SetBoldMode(2, false)
            });
    }

    /// <summary>
    /// Scenario: ESC - toggles underline mode on and off.
    /// </summary>
    [Fact]
    public void EmitsSetUnderlineModeToggleForEscDash()
    {
        using var context = EscPosTestHelper.CreateContext();
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
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new SetUnderlineMode(1, true),
                new SetUnderlineMode(2, false)
            });
    }

    /// <summary>
    /// Scenario: GS B toggles reverse (white-on-black) mode on and off.
    /// </summary>
    [Fact]
    public void EmitsSetReverseModeToggleForGsB()
    {
        using var context = EscPosTestHelper.CreateContext();
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
            expectedSourceIp: null,
            expectedElements: new Element[]
            {
                new SetReverseMode(1, true),
                new SetReverseMode(2, false)
            });
    }
}

