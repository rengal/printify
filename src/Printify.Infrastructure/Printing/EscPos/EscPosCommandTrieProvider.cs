using Printify.Application.Interfaces;
using Printify.Infrastructure.Media;
using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

namespace Printify.Infrastructure.Printing.EscPos;

/// <summary>
/// Builds the ESC/POS command trie once and keeps it immutable for reuse.
/// </summary>
public sealed class EscPosCommandTrieProvider : CommandTrieProvider<ParserState, ICommandDescriptor<ParserState>>
{
    private static readonly IMediaService RasterMediaService = new MediaService();

    protected override IEnumerable<ICommandDescriptor<ParserState>> AllDescriptors =>
    [
        new BarcodePrintDescriptor(),
        new BarcodeSetHeightDescriptor(),
        new BarcodeSetLabelPositionDescriptor(),
        new BarcodeSetModuleWidthDescriptor(),
        new BelDescriptor(),
        new GetPrinterStatusDescriptor(),
        new FlushLineBufferAndFeedDescriptor(),
        new LegacyCarriageReturnDescriptor(),
        new PageCutDescriptor(),
        new PartialCutOnePointDescriptor(),
        new PartialCutThreePointDescriptor(),
        new PrintStoredLogoDescriptor(),
        new PulseDescriptor(),
        new QrCodeDescriptor(),
        new RasterBitImagePrintDescriptor(RasterMediaService),
        new ResetLineSpacingDescriptor(),
        new ResetPrinterDescriptor(),
        new SetBoldModeDescriptor(),
        new SetChineseCodePageDescriptor(),
        new SetCodePageDescriptor(),
        new SetFontDescriptor(),
        new SetJustificationDescriptor(),
        new SetLineSpacingDescriptor(),
        new SetReverseModeDescriptor(),
        new SetUnderlineModeDescriptor()
    ];
}
