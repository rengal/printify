using Printify.Domain.Documents;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Media;
using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Domain.Specifications;
using Printify.Infrastructure.Printing.EscPos.Commands;
using Printify.Infrastructure.Printing.EscPos.Renderers;
using Printify.Tests.Shared;
using DomainMedia = Printify.Domain.Media.Media;

namespace Printify.Infrastructure.Tests.Printing.EscPos;

public sealed class EscPosRendererCoverageTests
{
    [Fact]
    public void Renderer_DoesNotThrowException_ForAllEscPosCommandTypes()
    {
        // Create sample commands to test
        var commands = new List<Command>
        {
            // Control commands
            new Bell(),
            new Initialize(),
            new CutPaper(PagecutMode.Full),
            new Pulse(0, 100, 100),
            new GetPrinterStatus(1),
            new GetPrinterStatus(1, 2),
            new StatusRequest(StatusRequestType.PrinterStatus),
            new StatusResponse(0x00, false, false, false),

            // Text commands
            new SetBoldMode(true),
            new SetBoldMode(false),
            new SetUnderlineMode(true),
            new SetUnderlineMode(false),
            new SetReverseMode(true),
            new SetReverseMode(false),
            new SetJustification(TextJustification.Left),
            new SetJustification(TextJustification.Center),
            new SetJustification(TextJustification.Right),
            new SetCodePage("437"),
            new SetCodePage("866"),
            new SelectFont(0, false, false),
            new SelectFont(1, true, false),
            new SelectFont(2, false, true),
            new SelectFont(3, true, true),
            new SetLineSpacing(30),
            new ResetLineSpacing(),
            new AppendText("test"u8.ToArray()),
            new PrintAndLineFeed(),
            new LegacyCarriageReturn(),

            // Barcode commands
            new SetBarcodeHeight(100),
            new SetBarcodeModuleWidth(2),
            new SetBarcodeLabelPosition(BarcodeLabelPosition.NotPrinted),
            new SetBarcodeLabelPosition(BarcodeLabelPosition.Above),
            new SetBarcodeLabelPosition(BarcodeLabelPosition.Below),
            new SetBarcodeLabelPosition(BarcodeLabelPosition.AboveAndBelow),
            new PrintBarcode(BarcodeSymbology.Code128, "12345", 100, 50, CreateTestMedia()),
            // Note: PrintBarcodeUpload is excluded because it's an upload command not meant to be rendered

            // QR Code commands
            new SetQrErrorCorrection(QrErrorCorrectionLevel.Low),
            new SetQrModel(QrModel.Model1),
            new SetQrModuleSize(8),
            new StoreQrData("TEST"),
            new PrintQrCode("TEST", 100, 100, CreateTestMedia()),
            // Note: PrintQrCodeUpload is excluded because it's an upload command not meant to be rendered

            // Graphics commands
            new RasterImage(100, 100, CreateTestMedia()),
            // Note: RasterImageUpload is excluded because it's an upload command not meant to be rendered

            // Logo commands
            new StoredLogo(1),

            // Error commands
            new EscPosParseError("ERR_CODE", "Test error message"),
            new EscPosPrinterError("Test printer error"),
        };

        // Verify the list is complete via reflection (excluding upload commands)
        CommandTestExtensions.VerifyAllRenderableEscPosCommandTypesAreTested(commands);

        // Create document and render
        var document = new Document(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Protocol.EscPos,
            null,
            0,
            0,
            EscPosSpecs.DefaultCanvasWidth,
            null,
            commands,
            null);

        // Act & Assert - should not throw
        var renderer = new EscPosRenderer();
        var exception = Record.Exception(() => renderer.Render(document));

        Assert.Null(exception);
    }

    [Fact]
    public void Renderer_ProducesValidCanvases_ForAllEscPosCommandTypes()
    {
        // Create sample commands to test
        var commands = new List<Command>
        {
            // Control commands
            new Bell(),
            new Initialize(),
            new CutPaper(PagecutMode.Full),
            new Pulse(0, 100, 100),
            new GetPrinterStatus(1),
            new GetPrinterStatus(1, 2),
            new StatusRequest(StatusRequestType.PrinterStatus),
            new StatusResponse(0x00, false, false, false),

            // Text commands
            new SetBoldMode(true),
            new SetBoldMode(false),
            new SetUnderlineMode(true),
            new SetUnderlineMode(false),
            new SetReverseMode(true),
            new SetReverseMode(false),
            new SetJustification(TextJustification.Left),
            new SetJustification(TextJustification.Center),
            new SetJustification(TextJustification.Right),
            new SetCodePage("437"),
            new SetCodePage("866"),
            new SelectFont(0, false, false),
            new SelectFont(1, true, false),
            new SelectFont(2, false, true),
            new SelectFont(3, true, true),
            new SetLineSpacing(30),
            new ResetLineSpacing(),
            new AppendText("test"u8.ToArray()),
            new PrintAndLineFeed(),
            new LegacyCarriageReturn(),

            // Barcode commands
            new SetBarcodeHeight(100),
            new SetBarcodeModuleWidth(2),
            new SetBarcodeLabelPosition(BarcodeLabelPosition.NotPrinted),
            new SetBarcodeLabelPosition(BarcodeLabelPosition.Above),
            new SetBarcodeLabelPosition(BarcodeLabelPosition.Below),
            new SetBarcodeLabelPosition(BarcodeLabelPosition.AboveAndBelow),
            new PrintBarcode(BarcodeSymbology.Code128, "12345", 100, 50, CreateTestMedia()),
            // Note: PrintBarcodeUpload is excluded because it's an upload command not meant to be rendered

            // QR Code commands
            new SetQrErrorCorrection(QrErrorCorrectionLevel.Low),
            new SetQrModel(QrModel.Model1),
            new SetQrModuleSize(8),
            new StoreQrData("TEST"),
            new PrintQrCode("TEST", 100, 100, CreateTestMedia()),
            // Note: PrintQrCodeUpload is excluded because it's an upload command not meant to be rendered

            // Graphics commands
            new RasterImage(100, 100, CreateTestMedia()),
            // Note: RasterImageUpload is excluded because it's an upload command not meant to be rendered

            // Logo commands
            new StoredLogo(1),

            // Error commands
            new EscPosParseError("ERR_CODE", "Test error message"),
            new EscPosPrinterError("Test printer error"),
        };

        // Verify the list is complete via reflection (excluding upload commands)
        CommandTestExtensions.VerifyAllRenderableEscPosCommandTypesAreTested(commands);

        // Create document and render
        var document = new Document(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Protocol.EscPos,
            null,
            0,
            0,
            EscPosSpecs.DefaultCanvasWidth,
            null,
            commands,
            null);

        // Act
        var renderer = new EscPosRenderer();
        var canvases = renderer.Render(document);

        // Assert
        Assert.NotNull(canvases);
        Assert.NotEmpty(canvases);

        // Each canvas should have at least one element
        foreach (var canvas in canvases)
        {
            Assert.NotNull(canvas.Items);
            Assert.NotEmpty(canvas.Items);
        }
    }

    private static DomainMedia CreateTestMedia()
    {
        return DomainMedia.CreateDefaultPng(100);
    }
}
