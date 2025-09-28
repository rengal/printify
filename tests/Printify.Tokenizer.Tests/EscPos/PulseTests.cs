namespace Printify.Tokenizer.Tests.EscPos;

using Printify.TestServcies;
using Contracts;
using Contracts.Elements;
using Xunit;

public sealed class PulseTests
{
    [Fact]
    public void EmitsPulseForEscSequence()
    {
        using var context = TestServices.CreateTokenizerContext<EscPosTokenizer>();
        var session = context.Tokenizer.CreateSession();

        session.Feed([
            EscPosTokenizer.Esc,
            (byte)'p',
            0x01,
            0x05,
            0x0A
        ]);
        session.Complete(CompletionReason.DataTimeout);

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements: new Element[]
            {
                new Pulse(1, PulsePin.Drawer2, 10, 20)
            });
    }
}
