using Printify.Domain.Media;
using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.EscPos;
using Printify.Infrastructure.Printing.EscPos.Commands;
using Printify.Tests.Shared;
using DomainMedia = Printify.Domain.Media.Media;

namespace Printify.Infrastructure.Tests.Printing.EscPos;

public sealed class EscPosCommandHelperTests
{
    [Fact]
    public void GetDescription_ReturnsNonEmptyDescription_ForAllEscPosCommandTypes()
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
            new EscPosStatusResponse(0x00, EscPosStatusRequestType.PrinterStatus, false, false, false),

            // Text commands
            new EscPosSetBoldMode(true),
            new EscPosSetBoldMode(false),
            new EscPosCancelBoldMode(),
            new EscPosSetDoubleStrikeMode(true),
            new EscPosSetDoubleStrikeMode(false),
            new EscPosEnableItalicMode(),
            new EscPosDisableItalicMode(),
            new EscPosSetUnderlineMode(true),
            new EscPosSetUnderlineMode(false),
            new EscPosSetReverseMode(true),
            new EscPosSetReverseMode(false),
            new EscPosSetCharacterSize(2, 3),
            new EscPosSetRightCharacterSpacing(4),
            new EscPosSetUpsideDownMode(true),
            new EscPosSetUpsideDownMode(false),
            new EscPosSetJustification(EscPosTextJustification.Left),
            new EscPosSetJustification(EscPosTextJustification.Center),
            new EscPosSetJustification(EscPosTextJustification.Right),
            new EscPosSetCodePage("437"),
            new EscPosSetCodePage("866"),
            new EscPosSetPrintMode(0, false, false),
            new EscPosSetPrintMode(1, true, false),
            new EscPosSetPrintMode(2, false, true),
            new EscPosSetPrintMode(3, true, true),
            new EscPosSetFont(0),
            new EscPosSetFont(1),
            new EscPosPrintAndFeedLines(0),
            new EscPosPrintAndFeedLines(3),
            new EscPosPrintAndFeedDots(7),
            new EscPosHorizontalTab(),
            new EscPosSetHorizontalTabStops([4, 8]),
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
            new EscPosPrintBarcodeUpload(EscPosBarcodeSymbology.Code128, "12345"),

            // QR Code commands
            new EscPosSetQrErrorCorrection(EscPosQrErrorCorrectionLevel.Low),
            new EscPosSetQrModel(EscPosQrModel.Model1),
            new EscPosSetQrModuleSize(8),
            new EscPosStoreQrData("TEST"),
            new EscPosPrintQrCode("TEST", 100, 100, CreateTestMedia()),
            new EscPosPrintQrCodeUpload(),

            // Graphics commands
            new EscPosRasterImageGs7630(100, 100, CreateTestMedia()),
            new EscPosRasterImageGs284C(100, 100, CreateTestMedia()),
            new EscPosRasterImageGs384C(100, 100, CreateTestMedia()),
            new EscPosRasterImageUploadGs7630(200, 200, CreateTestMediaUpload()),
            new EscPosRasterImageStoreGs284C(200, 200, CreateTestMediaUpload()),
            new EscPosRasterImageStoreGs384C(200, 200, CreateTestMediaUpload()),
            new EscPosRasterImagePrintUploadGs284C(),

            // Logo commands
            new EscPosPrintLogo(1),

            // Error commands
            new EscPosParseError("ERR_CODE", "Test error message"),
            new EscPosPrinterError("Test printer error"),
        };

        // Verify the list is complete via reflection (shared extension)
        CommandTestExtensions.VerifyAllEscPosCommandTypesAreTested(commands);

        // Test each command's description
        var failures = new List<string>();

        foreach (var command in commands)
        {
            try
            {
                var description = EscPosCommandHelper.GetDescription(command);

                if (description == null || description.Count == 0)
                {
                    failures.Add($"{command.GetType().Name}: GetDescription returned null or empty");
                }
                else
                {
                    var emptyLines = description.Where(string.IsNullOrWhiteSpace).ToList();
                    if (emptyLines.Any())
                    {
                        failures.Add($"{command.GetType().Name}: Contains empty/whitespace lines");
                    }
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{command.GetType().Name}: Exception - {ex.Message}");
            }
        }

        if (failures.Any())
        {
            Assert.Fail($"GetDescription test failed for {failures.Count} commands:\n{string.Join("\n", failures)}");
        }
    }

    private static DomainMedia CreateTestMedia()
    {
        return DomainMedia.CreateDefaultPng(100);
    }

    private static MediaUpload CreateTestMediaUpload()
    {
        return new MediaUpload("image/test", "test-data"u8.ToArray());
    }
}
