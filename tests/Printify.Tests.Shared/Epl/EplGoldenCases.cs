using System.Text;
using EplCommands = Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Domain.Media;
using Printify.Domain.Printing;
using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;
using Xunit;

namespace Printify.Tests.Shared.Epl;

/// <summary>
/// Provides shared EPL golden cases and their expected documents.
/// </summary>
public static class EplGoldenCases
{
    private static readonly bool EncodingProviderRegistered = RegisterEncodingProvider();
    private static readonly
        IReadOnlyDictionary<string, (IReadOnlyList<Command>, IReadOnlyList<Command>?, IReadOnlyList<CanvasElementDto>)>
            expectations =
                new Dictionary<string, (
                    IReadOnlyList<Command> expectedRequestElement,
                    IReadOnlyList<Command>? expectedFinalizedElements,
                    IReadOnlyList<CanvasElementDto> expectedCanvasElements)>
            {
                ["case01"] = (
                    expectedRequestElement:
                    [
                        new EplCommands.EplLineFeed { LengthInBytes = 1 },
                        new EplCommands.EplClearBuffer { LengthInBytes = 2 },
                        new EplCommands.EplSetLabelWidth(432) { LengthInBytes = 5 },
                        new EplCommands.EplSetLabelHeight(982, 26) { LengthInBytes = 8 },
                        new EplCommands.EplRasterImageUpload(1, 0, 432, 890, new MediaUpload("image/png", Array.Empty<byte>())) { LengthInBytes = 48074 },
                        new EplCommands.EplPrint(1) { LengthInBytes = 3 }
                    ],
                    expectedFinalizedElements:
                    [
                        new EplCommands.EplLineFeed { LengthInBytes = 1 },
                        new EplCommands.EplClearBuffer { LengthInBytes = 2 },
                        new EplCommands.EplSetLabelWidth(432) { LengthInBytes = 5 },
                        new EplCommands.EplSetLabelHeight(982, 26) { LengthInBytes = 8 },
                        new EplCommands.EplRasterImage(1, 0, 432, 890, Media.CreateDefaultPng(16514)) { LengthInBytes = 48074 },
                        new EplCommands.EplPrint(1) { LengthInBytes = 3 }
                    ],
                    expectedCanvasElements:
                    [
                        new CanvasDebugElementDto("lineFeed") { LengthInBytes = 1 },
                        new CanvasDebugElementDto("clearBuffer") { LengthInBytes = 2 },
                        new CanvasDebugElementDto("setLabelWidth", new Dictionary<string, string> { ["Width"] = "432" }) { LengthInBytes = 5 },
                        new CanvasDebugElementDto("setLabelHeight", new Dictionary<string, string> { ["Height"] = "982", ["SecondParameter"] = "26" }) { LengthInBytes = 8 },
                        new CanvasDebugElementDto("rasterImage") { LengthInBytes = 48074 },
                        new CanvasDebugElementDto("print", new Dictionary<string, string> { ["Copies"] = "1" }) { LengthInBytes = 3 },
                        new CanvasImageElementDto(
                            new CanvasMediaDto("image/png", 1, "", ""),
                            1,
                            0,
                            432,
                            890) { LengthInBytes = 48074 }
                    ])
            };

    public static
        IReadOnlyDictionary<string, (
            IReadOnlyList<Command> expectedRequestElement,
            IReadOnlyList<Command>? expectedPersistedElements,
            IReadOnlyList<CanvasElementDto> expectedViewElements)> Expectations => expectations;

    public static TheoryData<string, byte[]> Cases { get; } = BuildCases();

    private static TheoryData<string, byte[]> BuildCases()
    {
        var data = new TheoryData<string, byte[]>();
        var assembly = typeof(EplGoldenCases).Assembly;

        var resources = assembly
            .GetManifestResourceNames()
            .Where(name => name.Contains(".Epl.") && name.EndsWith(".b64", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        foreach (var resourceName in resources)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var base64 = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(base64))
            {
                continue;
            }

            try
            {
                var bytes = Convert.FromBase64String(base64);
                var caseId = GetCaseId(resourceName);
                data.Add(caseId, bytes);
            }
            catch (FormatException)
            {
                // Ignore malformed placeholders; content will be filled later.
            }
        }

        return data;
    }

    private static string GetCaseId(string resourceName)
    {
        var withoutExtension = resourceName[..resourceName.LastIndexOf(".", StringComparison.Ordinal)];
        var lastSeparator = withoutExtension.LastIndexOf(".", StringComparison.Ordinal);
        return lastSeparator >= 0
            ? withoutExtension[(lastSeparator + 1)..]
            : withoutExtension;
    }

    private static bool RegisterEncodingProvider()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return true;
    }
}
