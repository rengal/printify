using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Printify.Services.Tokenizer;

namespace Printify.Tokenizer.Tests.EscPos;

using Xunit;
using TestServices;
using Printify.Domain.PrintJobs;

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
