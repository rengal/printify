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
            new EscPosBell(),
            new EscPosInitialize(),
            new EscPosCutPaper(EscPosPagecutMode.Full),
            new EscPosPulse(0, 100, 100),
            new EscPosGetPrinterStatus(1),
            new EscPosGetPrinterStatus(1, 2),
            new EscPosStatusRequest(EscPosStatusRequestType.PrinterStatus),
            new EscPosStatusResponse(0x00, false, false, false),

            // Text commands
            new EscPosSetBoldMode(true),
            new EscPosSetBoldMode(false),
            new EscPosSetUnderlineMode(true),
            new EscPosSetUnderlineMode(false),
            new EscPosSetReverseMode(true),
            new EscPosSetReverseMode(false),
            new EscPosSetJustification(EscPosTextJustification.Left),
            new EscPosSetJustification(EscPosTextJustification.Center),
            new EscPosSetJustification(EscPosTextJustification.Right),
            new EscPosSetCodePage("437"),
            new EscPosSetCodePage("866"),
            new EscPosSelectFont(0, false, false),
            new EscPosSelectFont(1, true, false),
            new EscPosSelectFont(2, false, true),
            new EscPosSelectFont(3, true, true),
            new EscPosSetLineSpacing(30),
            new EscPosResetLineSpacing(),
            new EscPosAppendText("test"u8.ToArray()),
            new EscPosPrintAndLineFeed(),
            new EscPosLegacyCarriageReturn(),

            // Barcode commands
            new EscPosSetBarcodeHeight(100),
            new EscPosSetBarcodeModuleWidth(2),
            new EscPosSetBarcodeLabelPosition(EscPosBarcodeLabelPosition.NotPrinted),
            new EscPosSetBarcodeLabelPosition(EscPosBarcodeLabelPosition.Above),
            new EscPosSetBarcodeLabelPosition(EscPosBarcodeLabelPosition.Below),
            new EscPosSetBarcodeLabelPosition(EscPosBarcodeLabelPosition.AboveAndBelow),
            new EscPosPrintBarcode(EscPosBarcodeSymbology.Code128, "12345", 100, 50, CreateTestMedia()),
            // Note: PrintBarcodeUpload is excluded because it's an upload command not meant to be rendered

            // QR Code commands
            new EscPosSetQrErrorCorrection(EscPosQrErrorCorrectionLevel.Low),
            new EscPosSetQrModel(EscPosQrModel.Model1),
            new EscPosSetQrModuleSize(8),
            new EscPosStoreQrData("TEST"),
            new EscPosPrintQrCode("TEST", 100, 100, CreateTestMedia()),
            // Note: PrintQrCodeUpload is excluded because it's an upload command not meant to be rendered

            // Graphics commands
            new EscPosRasterImage(100, 100, CreateTestMedia()),
            // Note: RasterImageUpload is excluded because it's an upload command not meant to be rendered

            // Logo commands
            new EscPosPrintLogo(1),

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
            new EscPosBell(),
            new EscPosInitialize(),
            new EscPosCutPaper(EscPosPagecutMode.Full),
            new EscPosPulse(0, 100, 100),
            new EscPosGetPrinterStatus(1),
            new EscPosGetPrinterStatus(1, 2),
            new EscPosStatusRequest(EscPosStatusRequestType.PrinterStatus),
            new EscPosStatusResponse(0x00, false, false, false),

            // Text commands
            new EscPosSetBoldMode(true),
            new EscPosSetBoldMode(false),
            new EscPosSetUnderlineMode(true),
            new EscPosSetUnderlineMode(false),
            new EscPosSetReverseMode(true),
            new EscPosSetReverseMode(false),
            new EscPosSetJustification(EscPosTextJustification.Left),
            new EscPosSetJustification(EscPosTextJustification.Center),
            new EscPosSetJustification(EscPosTextJustification.Right),
            new EscPosSetCodePage("437"),
            new EscPosSetCodePage("866"),
            new EscPosSelectFont(0, false, false),
            new EscPosSelectFont(1, true, false),
            new EscPosSelectFont(2, false, true),
            new EscPosSelectFont(3, true, true),
            new EscPosSetLineSpacing(30),
            new EscPosResetLineSpacing(),
            new EscPosAppendText("test"u8.ToArray()),
            new EscPosPrintAndLineFeed(),
            new EscPosLegacyCarriageReturn(),

            // Barcode commands
            new EscPosSetBarcodeHeight(100),
            new EscPosSetBarcodeModuleWidth(2),
            new EscPosSetBarcodeLabelPosition(EscPosBarcodeLabelPosition.NotPrinted),
            new EscPosSetBarcodeLabelPosition(EscPosBarcodeLabelPosition.Above),
            new EscPosSetBarcodeLabelPosition(EscPosBarcodeLabelPosition.Below),
            new EscPosSetBarcodeLabelPosition(EscPosBarcodeLabelPosition.AboveAndBelow),
            new EscPosPrintBarcode(EscPosBarcodeSymbology.Code128, "12345", 100, 50, CreateTestMedia()),
            // Note: PrintBarcodeUpload is excluded because it's an upload command not meant to be rendered

            // QR Code commands
            new EscPosSetQrErrorCorrection(EscPosQrErrorCorrectionLevel.Low),
            new EscPosSetQrModel(EscPosQrModel.Model1),
            new EscPosSetQrModuleSize(8),
            new EscPosStoreQrData("TEST"),
            new EscPosPrintQrCode("TEST", 100, 100, CreateTestMedia()),
            // Note: PrintQrCodeUpload is excluded because it's an upload command not meant to be rendered

            // Graphics commands
            new EscPosRasterImage(100, 100, CreateTestMedia()),
            // Note: RasterImageUpload is excluded because it's an upload command not meant to be rendered

            // Logo commands
            new EscPosPrintLogo(1),

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
