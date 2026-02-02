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
            new PrintBarcodeUpload(BarcodeSymbology.Code128, "12345"),

            // QR Code commands
            new SetQrErrorCorrection(QrErrorCorrectionLevel.Low),
            new SetQrModel(QrModel.Model1),
            new SetQrModuleSize(8),
            new StoreQrData("TEST"),
            new PrintQrCode("TEST", 100, 100, CreateTestMedia()),
            new PrintQrCodeUpload(),

            // Graphics commands
            new RasterImage(100, 100, CreateTestMedia()),
            new RasterImageUpload(200, 200, CreateTestMediaUpload()),

            // Logo commands
            new StoredLogo(1),

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
