using System.Runtime.InteropServices;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.Epl;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.Epl;

/// <summary>
/// Parser for EPL (Eltron Programming Language) page mode printer commands.
/// EPL uses newline-terminated ASCII commands that draw elements on a label.
/// Unlike ESC/POS line mode, EPL accumulates drawing commands until a print command (P) is received.
/// </summary>
public sealed class EplParser : Parser<EplParserState, EplCommandTrieProvider>
{
    public EplParser(Action<Element> onElement)
        : base(new EplCommandTrieProvider(), new EplParserState(new EplCommandTrieProvider().Root), null, null, null, onElement, null)
    {
    }

    protected override ParserMode GetMode() => State.Mode;

    protected override void SetMode(ParserMode newMode) => State.Mode = newMode;

    protected override void EmitBufferForModeChange(ParserMode oldMode, ParserMode newMode)
    {
        // EPL doesn't have text mode, so we only handle command and error modes
        if (oldMode == ParserMode.Command || oldMode == ParserMode.Error)
        {
            if (State.Buffer.Count > 0)
            {
                EmitUnrecognizedBufferAsError();
            }
        }
    }

    protected override List<byte> GetBuffer() => State.Buffer;

    protected override List<byte> GetUnrecognizedBuffer() => State.UnrecognizedBuffer;

    protected override ICommandState<EplParserState> GetCommandState() => State.EplCommandState;

    protected override ParserMode GetDefaultMode() => ParserMode.Command;

    protected override bool ShouldSkipBufferOverflowCheck(Element element)
    {
        return element is ParseError or PrinterError;
    }
}
