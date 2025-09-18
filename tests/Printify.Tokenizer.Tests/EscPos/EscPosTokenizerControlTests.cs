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
}
