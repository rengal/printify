using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.Epl;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.Epl;

/// <summary>
/// Parser for EPL (Eltron Programming Language) page mode printer commands.
/// EPL uses newline-terminated ASCII commands that draw elements on a label.
/// Unlike ESC/POS line mode, EPL accumulates drawing commands until a print command (P) is received.
/// EPL does not support free text - any bytes that aren't valid commands are treated as errors.
/// </summary>
public sealed class EplParser : Parser<EplDeviceContext, EplCommandTrieProvider>
{
    /// <summary>
    /// Initializes a new EPL parser with the specified element callback.
    /// </summary>
    /// <param name="onElement">Callback invoked for each parsed element.</param>
    public EplParser(Action<Element> onElement)
        : base(
            new EplCommandTrieProvider(),
            new ParserState<EplDeviceContext>(new EplDeviceContext(), new EplCommandTrieProvider().Root),
            scopeFactory: null,
            printer: null,
            settings: null,
            onElement: onElement,
            onResponse: null)
    {
        // EPL starts in Command mode (no Text mode)
        State.Mode = ParserMode.Command;
    }

    /// <summary>
    /// Modifies the device context based on the parsed element.
    /// Called after successful parse but before emitting.
    /// </summary>
    protected override void ModifyDeviceContext(Element element)
    {
        // EPL commands that modify device context state
        switch (element)
        {
            case SetLabelWidth setLabelWidth:
                State.DeviceContext.LabelWidth = setLabelWidth.Width;
                break;
            case SetLabelHeight setLabelHeight:
                State.DeviceContext.LabelHeight = setLabelHeight.Height;
                break;
            case SetPrintSpeed setPrintSpeed:
                State.DeviceContext.PrintSpeed = setPrintSpeed.Speed;
                break;
            case SetPrintDarkness setPrintDarkness:
                State.DeviceContext.PrintDarkness = setPrintDarkness.Darkness;
                break;
            case SetInternationalCharacter setInternationalCharacter:
                // Update encoding based on international character set
                if (setInternationalCharacter.Code is 8 or 38) // DOS 866 Cyrillic
                    State.DeviceContext.Encoding = System.Text.Encoding.GetEncoding(866);
                break;
            case SetCodePage setCodePage:
                // Update encoding based on code page
                if (setCodePage.Code is 0 or 8) // DOS 866 Cyrillic
                    State.DeviceContext.Encoding = System.Text.Encoding.GetEncoding(866);
                break;
        }
    }

    /// <summary>
    /// EPL doesn't have text mode, so we only handle command and error modes.
    /// When switching from Command to Error mode, emit any buffered bytes as an error.
    /// </summary>
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

    /// <summary>
    /// EPL doesn't have text mode, so the default mode is Command.
    /// </summary>
    protected override ParserMode GetDefaultMode() => ParserMode.Command;

    /// <summary>
    /// Skip buffer overflow check for error elements.
    /// </summary>
    protected override bool ShouldSkipBufferOverflowCheck(Element element)
    {
        return element is ParseError or PrinterError;
    }
}
