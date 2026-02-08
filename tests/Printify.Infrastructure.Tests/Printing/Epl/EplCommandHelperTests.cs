using Printify.Domain.Media;
using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.Epl;
using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Tests.Shared;
using DomainMedia = Printify.Domain.Media.Media;

namespace Printify.Infrastructure.Tests.Printing.Epl;

public sealed class EplCommandHelperTests
{
    private static readonly DomainMedia TestMedia = DomainMedia.CreateDefaultPng(100);

    [Fact]
    public void GetDescription_ReturnsNonEmptyDescription_ForAllEplCommandTypes()
    {
        // Create sample commands to test
        var commands = new List<Command>
        {
            // Config commands
            new ClearBuffer(),
            new CarriageReturn(),
            new LineFeed(),
            new SetLabelWidth(500),
            new SetLabelHeight(300, 26),
            new SetPrintSpeed(3),
            new SetPrintDarkness(10),
            new SetPrintDirection(PrintDirection.TopToBottom),
            new SetPrintDirection(PrintDirection.BottomToTop),
            new SetInternationalCharacter(0),
            new SetInternationalCharacter(8),

            // Text commands
            new ScalableText(10, 20, 0, 1, 1, 1, 'N', "test"u8.ToArray()),
            new ScalableText(10, 20, 1, 2, 2, 2, 'R', "test"u8.ToArray()),
            new DrawHorizontalLine(10, 20, 2, 100),

            // Barcode commands
            new PrintBarcode(10, 20, 0, "CODE128", 2, 100, 'N', "12345"),
            new PrintBarcode(10, 20, 1, "CODE39", 3, 80, 'B', "ABC"),
            new EplPrintBarcode(10, 20, 0, "CODE128", 2, 100, 'N', "12345", TestMedia),
            // Note: EplPrintBarcodeUpload is excluded because it's an upload command not meant to be rendered

            // Graphics commands
            new PrintGraphic(10, 20, 100, 50, new byte[20]),
            new EplRasterImage(10, 20, 100, 50, TestMedia),
            // Note: EplRasterImageUpload is excluded because it's an upload command not meant to be rendered

            // Shape commands
            new DrawBox(10, 20, 2, 100, 80),

            // Print commands
            new Print(1),
            new Print(2),

            // Error commands
            new EplParseError("ERR_CODE", "Test error message"),
            new EplPrinterError("Test printer error"),
        };

        // Verify the list is complete via reflection (excluding upload commands)
        CommandTestExtensions.VerifyAllRenderableEplCommandTypesAreTested(commands);

        // Test each command's description
        var failures = new List<string>();

        foreach (var command in commands)
        {
            try
            {
                var description = EplCommandHelper.GetDescription(command);

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
}
