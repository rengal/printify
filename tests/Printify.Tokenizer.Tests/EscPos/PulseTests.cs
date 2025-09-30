namespace Printify.Tokenizer.Tests.EscPos;

using TestServices;
using Contracts;
using Contracts.Elements;
using Xunit;

public sealed class PulseTests
{
    [Fact]
    public void EmitsPulseForEscSequence()
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));
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
            expectedElements:
            [
                new Pulse(1, PulsePin.Drawer2, 10, 20)
            ]);
    }
}
