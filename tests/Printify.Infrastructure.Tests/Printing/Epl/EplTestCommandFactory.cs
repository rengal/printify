using Printify.Domain.Media;
using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.Epl.Commands;
using DomainMedia = Printify.Domain.Media.Media;

namespace Printify.Infrastructure.Tests.Printing.Epl;

internal static class EplTestCommandFactory
{
    public static List<Command> CreateSampleEplCommands(bool withUploadCommands)
    {
        var commands = new List<Command>
        {
            // Config commands.
            new EplClearBuffer(),
            new EplCarriageReturn(),
            new EplLineFeed(),
            new EplSetLabelWidth(500),
            new EplSetLabelHeight(300, 26),
            new EplSetPrintSpeed(3),
            new EplSetPrintDarkness(10),
            new SetPrintDirection(EplPrintDirection.TopToBottom),
            new SetPrintDirection(EplPrintDirection.BottomToTop),
            new EplSetInternationalCharacter(0),
            new EplSetInternationalCharacter(8),

            // Text and shape commands.
            new EplScalableText(10, 20, 0, 1, 1, 1, 'N', "test"u8.ToArray()),
            new EplScalableText(10, 20, 1, 2, 2, 2, 'R', "test"u8.ToArray()),
            new EplDrawHorizontalLine(10, 20, 2, 100),
            new EplDrawBox(10, 20, 2, 100, 80),

            // Finalized media commands.
            new EplPrintBarcode(10, 20, 0, "CODE128", 2, 100, 'N', "12345", CreateTestMedia()),
            new EplRasterImage(10, 20, 100, 50, CreateTestMedia()),

            // Error commands.
            new EplParseError("ERR_CODE", "Test error message"),
            new EplPrinterError("Test printer error")
        };

        if (withUploadCommands)
        {
            var upload = CreateTestUpload();
            commands.Add(new EplPrintBarcodeUpload(10, 20, 0, "CODE128", 2, 100, 'N', "12345", upload));
            commands.Add(new EplRasterImageUpload(10, 20, 100, 50, upload));
        }

        // Print commands are included so renderer tests can produce canvases.
        commands.Add(new EplPrint(1));
        commands.Add(new EplScalableText(10, 20, 0, 1, 1, 1, 'N', "test2"u8.ToArray()));
        commands.Add(new EplDrawBox(20, 40, 2, 150, 100));
        commands.Add(new EplPrint(1));
        return commands;
    }

    private static DomainMedia CreateTestMedia()
    {
        return DomainMedia.CreateDefaultPng(100);
    }

    private static MediaUpload CreateTestUpload()
    {
        return new MediaUpload("image/png", "test-upload"u8.ToArray());
    }
}
