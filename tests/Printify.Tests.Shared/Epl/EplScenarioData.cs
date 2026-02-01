using System.Text;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Printing;
using Printify.Domain.Specifications;
using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;
using Printify.Web.Mapping;
using Xunit;

namespace Printify.Tests.Shared.Epl;

/// <summary>
/// Provides reusable EPL parser scenarios for unit and integration tests.
/// </summary>
public static class EplScenarioData
{
    private static readonly bool EncodingProviderRegistered = RegisterEncodingProvider();

    public static TheoryData<EplScenario> ClearBufferScenarios { get; } =
    [
        new(
            id: 1001,
            input: "N\n"u8.ToArray(),
            expectedRequestCommands: [new ClearBuffer { LengthInBytes = 2 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("clearBuffer", lengthInBytes: 2)
                ]
            ])
    ];

    public static TheoryData<EplScenario> LabelConfigScenarios { get; } =
    [
        new(
            id: 2001,
            input: "q500\n"u8.ToArray(),
            expectedRequestCommands: [new SetLabelWidth(500) { LengthInBytes = 5 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setLabelWidth", lengthInBytes: 5, parameters: new Dictionary<string, string> { ["Width"] = "500" })
                ]
            ]),
        new(
            id: 2002,
            input: "Q300,26\n"u8.ToArray(),
            expectedRequestCommands: [new SetLabelHeight(300, 26) { LengthInBytes = 8 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setLabelHeight", lengthInBytes: 8, parameters: new Dictionary<string, string> { ["Height"] = "300", ["SecondParameter"] = "26" })
                ]
            ]),
        new(
            id: 2003,
            input: "R3\n"u8.ToArray(),
            expectedRequestCommands: [new SetPrintSpeed(3) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setPrintSpeed", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Speed"] = "3" })
                ]
            ]),
        new(
            id: 2004,
            input: "S10\n"u8.ToArray(),
            expectedRequestCommands: [new SetPrintDarkness(10) { LengthInBytes = 4 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setPrintDarkness", lengthInBytes: 4, parameters: new Dictionary<string, string> { ["Darkness"] = "10" })
                ]
            ]),
        new(
            id: 2005,
            input: "ZT\n"u8.ToArray(),
            expectedRequestCommands: [new SetPrintDirection(PrintDirection.TopToBottom) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setPrintDirection", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Direction"] = "TopToBottom" })
                ]
            ]),
        new(
            id: 2006,
            input: "ZB\n"u8.ToArray(),
            expectedRequestCommands: [new SetPrintDirection(PrintDirection.BottomToTop) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setPrintDirection", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Direction"] = "BottomToTop" })
                ]
            ]),
        new(
            id: 2007,
            input: "I8\n"u8.ToArray(),
            expectedRequestCommands: [new SetInternationalCharacter(8, 0, 0) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setInternationalCharacter", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["P1"] = "8", ["P2"] = "0", ["P3"] = "0" })
                ]
            ]),
        new(
            id: 2008,
            input: "I8,10\n"u8.ToArray(),
            expectedRequestCommands: [new SetInternationalCharacter(8, 10, 0) { LengthInBytes = 6 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setInternationalCharacter", lengthInBytes: 6, parameters: new Dictionary<string, string> { ["P1"] = "8", ["P2"] = "10", ["P3"] = "0" })
                ]
            ])
    ];

    public static TheoryData<EplScenario> TextScenarios { get; } =
    [
        new(
            id: 3001,
            input: "A10,20,0,2,1,1,N,\"Hello\"\n"u8.ToArray(),
            expectedRequestCommands:
            [
                CreateScalableTextCommand(
                    10,
                    20,
                    0,
                    2,
                    1,
                    1,
                    'N',
                    "Hello",
                    lengthInBytes: "A10,20,0,2,1,1,N,\"Hello\"\n"u8.Length)
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("scalableText", lengthInBytes: 25, parameters: new Dictionary<string, string>
                    {
                        ["X"] = "10",
                        ["Y"] = "20",
                        ["Rotation"] = "0",
                        ["Font"] = "2",
                        ["HorizontalMultiplication"] = "1",
                        ["VerticalMultiplication"] = "1",
                        ["Reverse"] = "N",
                        ["Text"] = "Hello"
                    }),
                    TextElement("Hello", x: 10, y: 20,
                        width: EplSpecs.Fonts.Font2.BaseWidthInDots * "Hello".Length, height: EplSpecs.Fonts.Font2.BaseHeightInDots,
                        fontName: EplSpecs.Fonts.Font2.FontName, charScaleX: 1, charScaleY: 1, rotation: 0, isReverse: false)
                ]
            ]),
        new(
            id: 3002,
            input: "A50,100,1,3,2,2,R,\"World\"\n"u8.ToArray(),
            expectedRequestCommands:
            [
                CreateScalableTextCommand(
                    50,
                    100,
                    1,
                    3,
                    2,
                    2,
                    'R',
                    "World",
                    lengthInBytes: "A50,100,1,3,2,2,R,\"World\"\n"u8.Length)
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("scalableText", lengthInBytes: 26, parameters: new Dictionary<string, string>
                    {
                        ["X"] = "50",
                        ["Y"] = "100",
                        ["Rotation"] = "1",
                        ["Font"] = "3",
                        ["HorizontalMultiplication"] = "2",
                        ["VerticalMultiplication"] = "2",
                        ["Reverse"] = "R",
                        ["Text"] = "World"
                    }),
                    // Font 3, scale 2x2: base 24x24, scaled 48x48, rotated (90°) = 48x48
                    TextElement("World", x: 50, y: 100,
                        width: EplSpecs.Fonts.Font3.BaseHeightInDots * 2, height: EplSpecs.Fonts.Font3.BaseWidthInDots * 2 * "World".Length, // rotation 90
                        fontName: EplSpecs.Fonts.Font3.FontName, charScaleX: 2, charScaleY: 2,
                        rotation: Rotation.Rotate90, isReverse: true)
                ]
            ]),
        new(
            id: 3003,
            input: "A0,0,0,4,3,3,N,\"Test123\"\n"u8.ToArray(),
            expectedRequestCommands:
            [
                CreateScalableTextCommand(
                    0,
                    0,
                    0,
                    4,
                    3,
                    3,
                    'N',
                    "Test123",
                    lengthInBytes: "A0,0,0,4,3,3,N,\"Test123\"\n"u8.Length)
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("scalableText", lengthInBytes: 25, parameters: new Dictionary<string, string>
                    {
                        ["X"] = "0",
                        ["Y"] = "0",
                        ["Rotation"] = "0",
                        ["Font"] = "4",
                        ["HorizontalMultiplication"] = "3",
                        ["VerticalMultiplication"] = "3",
                        ["Reverse"] = "N",
                        ["Text"] = "Test123"
                    }),
                    // Font 4, scale 3x3: base 24x24, scaled 72x72
                    TextElement("Test123", x: 0, y: 0,
                        width: EplSpecs.Fonts.Font4.BaseWidthInDots * 3 * "Test123".Length,
                        height: EplSpecs.Fonts.Font4.BaseHeightInDots * 3,
                        fontName: EplSpecs.Fonts.Font4.FontName,
                        charScaleX: 3, charScaleY: 3, rotation: 0, isReverse: false)
                ]
            ]),
        // CR (carriage return) as no-op command
        new(
            id: 3004,
            input: "\r"u8.ToArray(),
            expectedRequestCommands: [new CarriageReturn { LengthInBytes = 1 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("carriageReturn", lengthInBytes: 1)
                ]
            ]),
        // LF (line feed) as no-op command
        new(
            id: 3005,
            input: "\n"u8.ToArray(),
            expectedRequestCommands: [new LineFeed { LengthInBytes = 1 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("lineFeed", lengthInBytes: 1)
                ]
            ]),
        // Clear buffer with CR terminator (not LF)
        new(
            id: 3006,
            input: "N\r"u8.ToArray(),
            expectedRequestCommands: [new ClearBuffer { LengthInBytes = 2 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("clearBuffer", lengthInBytes: 2)
                ]
            ]),
        // Clear buffer with LF terminator (standard)
        new(
            id: 3007,
            input: "N\n"u8.ToArray(),
            expectedRequestCommands: [new ClearBuffer { LengthInBytes = 2 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("clearBuffer", lengthInBytes: 2)
                ]
            ]),
        // Text command with CR terminator
        new(
            id: 3008,
            input: "A10,20,0,2,1,1,N,\"Test\"\r"u8.ToArray(),
            expectedRequestCommands:
            [
                CreateScalableTextCommand(
                    10,
                    20,
                    0,
                    2,
                    1,
                    1,
                    'N',
                    "Test",
                    lengthInBytes: "A10,20,0,2,1,1,N,\"Test\"\r"u8.Length)
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("scalableText", lengthInBytes: 24, parameters: new Dictionary<string, string>
                    {
                        ["X"] = "10",
                        ["Y"] = "20",
                        ["Rotation"] = "0",
                        ["Font"] = "2",
                        ["HorizontalMultiplication"] = "1",
                        ["VerticalMultiplication"] = "1",
                        ["Reverse"] = "N",
                        ["Text"] = "Test"
                    }),
                    TextElement("Test", x: 10, y: 20,
                        width: EplSpecs.Fonts.Font2.BaseWidthInDots * "Test".Length,
                        height: EplSpecs.Fonts.Font2.BaseHeightInDots,
                        fontName: EplSpecs.Fonts.Font2.FontName,
                        charScaleX: 1, charScaleY: 1, rotation: 0, isReverse: false)
                ]
            ]),
        // CRLF sequence - CR terminates N command, remaining LF becomes LineFeed no-op
        new(
            id: 3009,
            input: "N\r\n"u8.ToArray(),
            expectedRequestCommands:
            [
                new ClearBuffer { LengthInBytes = 2 },
                new LineFeed { LengthInBytes = 1 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("clearBuffer", lengthInBytes: 2),
                    DebugElement("lineFeed", lengthInBytes: 1)
                ]
            ]),
        // Text command with LF then CR sequence (two separate no-ops)
        new(
            id: 3010,
            input: "\n\r"u8.ToArray(),
            expectedRequestCommands:
            [
                new LineFeed { LengthInBytes = 1 },
                new CarriageReturn { LengthInBytes = 1 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("lineFeed", lengthInBytes: 1),
                    DebugElement("carriageReturn", lengthInBytes: 1)
                ]
            ])
    ];

    public static TheoryData<EplScenario> BarcodeScenarios { get; } =
    [
        new(
            id: 4001,
            input: "B10,50,0,E30,2,100,B,\"123456789012\"\n"u8.ToArray(),
            expectedRequestCommands: [new PrintBarcode(10, 50, 0, "E30", 2, 100, 'B', "123456789012") { LengthInBytes = 36 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("printBarcode", lengthInBytes: 36, parameters: new Dictionary<string, string>
                    {
                        ["X"] = "10",
                        ["Y"] = "50",
                        ["Rotation"] = "0",
                        ["Type"] = "E30",
                        ["Width"] = "2",
                        ["Height"] = "100",
                        ["Hri"] = "B",
                        ["Data"] = "123456789012"
                    }),
                    ImageElement(x: 10, y: 50, width: 2, height: 100, rotation: 0, contentType: "image/barcode")
                ]
            ]),
        new(
            id: 4002,
            input: "B20,80,1,2A,3,120,N,\"ABC123\"\n"u8.ToArray(),
            expectedRequestCommands: [new PrintBarcode(20, 80, 1, "2A", 3, 120, 'N', "ABC123") { LengthInBytes = 29 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("printBarcode", lengthInBytes: 29, parameters: new Dictionary<string, string>
                    {
                        ["X"] = "20",
                        ["Y"] = "80",
                        ["Rotation"] = "1",
                        ["Type"] = "2A",
                        ["Width"] = "3",
                        ["Height"] = "120",
                        ["Hri"] = "N",
                        ["Data"] = "ABC123"
                    }),
                    // rotation=1 means 90° clockwise: width/height swap expected
                    ImageElement(x: 20, y: 80, width: 120, height: 3, rotation: Rotation.Rotate90, contentType: "image/barcode")
                ]
            ])
    ];

    // TODO: GW graphic command needs further parser implementation verification
    public static TheoryData<EplScenario> GraphicScenarios { get; } = [];

    // TODO: Combined scenario needs parser behavior investigation - currently produces individual byte elements
    public static TheoryData<EplScenario> CombinedScenarios { get; } = [];

    public static TheoryData<EplScenario> ShapeScenarios { get; } =
    [
        new(
            id: 6001,
            input: "LO10,20,2,100\n"u8.ToArray(),
            expectedRequestCommands: [new DrawHorizontalLine(10, 20, 2, 100) { LengthInBytes = 14 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("drawHorizontalLine", lengthInBytes: 14, parameters: new Dictionary<string, string>
                    {
                        ["X"] = "10",
                        ["Y"] = "20",
                        ["Thickness"] = "2",
                        ["Length"] = "100"
                    }),
                    // Horizontal lines represented as empty text element with line dimensions
                    TextElement("", x: 10, y: 20, width: 100, height: 2)
                ]
            ]),
        new(
            id: 6002,
            input: "X5,10,1,200,50\n"u8.ToArray(),
            expectedRequestCommands: [new DrawLine(5, 10, 1, 200, 50) { LengthInBytes = 15 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("drawLine", lengthInBytes: 15, parameters: new Dictionary<string, string>
                    {
                        ["X1"] = "5",
                        ["Y1"] = "10",
                        ["Thickness"] = "1",
                        ["X2"] = "200",
                        ["Y2"] = "50"
                    }),
                    // Lines represented as empty text element with bounding box dimensions
                    // Bounding box: x=min(5,200)=5, y=min(10,50)=10, width=195, height=40
                    // width = max(|200-5|, thickness=1) = 195, height = max(|50-10|, thickness=1) = 40
                    TextElement("", x: 5, y: 10, width: 195, height: 40)
                ]
            ])
    ];

    public static TheoryData<EplScenario> PrintScenarios { get; } =
    [
        new(
            id: 7001,
            input: "P1\n"u8.ToArray(),
            expectedRequestCommands: [new Print(1) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("print", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Copies"] = "1" })
                ]
            ]),
        new(
            id: 7002,
            input: "P5\n"u8.ToArray(),
            expectedRequestCommands: [new Print(5) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("print", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Copies"] = "5" })
                ]
            ])
    ];

    public static TheoryData<EplScenario> ErrorScenarios { get; } =
    [
        new(
            id: 8001,
            input: "\x00\x01\x02"u8.ToArray(),
            expectedRequestCommands: [new PrinterError("") { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("printerError", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Message"] = "" })
                ]
            ])
    ];

    public static TheoryData<EplScenario> AllScenarios { get; } = BuildAllScenarios();

    private static TheoryData<EplScenario> BuildAllScenarios()
    {
        var data = new TheoryData<EplScenario>();
        AddRange(data, ClearBufferScenarios);
        AddRange(data, LabelConfigScenarios);
        AddRange(data, TextScenarios);
        AddRange(data, BarcodeScenarios);
        AddRange(data, GraphicScenarios);
        AddRange(data, ShapeScenarios);
        AddRange(data, PrintScenarios);
        AddRange(data, ErrorScenarios);
        AddRange(data, CombinedScenarios);
        return data;
    }

    private static void AddRange(TheoryData<EplScenario> target, TheoryData<EplScenario> source)
    {
        foreach (var scenario in source)
        {
            target.Add(scenario);
        }
    }

    private static CanvasDebugElementDto DebugElement(
        string name,
        int lengthInBytes,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        return new CanvasDebugElementDto(name, parameters ?? new Dictionary<string, string>())
        {
            LengthInBytes = lengthInBytes
        };
    }

    private static CanvasTextElementDto TextElement(
        string text,
        int x,
        int y,
        int width,
        int height,
        string? fontName = null,
        int charSpacing = 0,
        bool isBold = false,
        bool isUnderline = false,
        bool isReverse = false,
        int charScaleX = 1,
        int charScaleY = 1,
        Rotation rotation = Rotation.None)
    {
        return new CanvasTextElementDto(
            text,
            x,
            y,
            width,
            height,
            fontName,
            charSpacing,
            isBold,
            isUnderline,
            isReverse,
            charScaleX,
            charScaleY,
            RotationMapper.ToDto(rotation));
    }

    private static CanvasImageElementDto ImageElement(
        int x,
        int y,
        int width,
        int height,
        Rotation rotation = Rotation.None,
        string contentType = "image/barcode")
    {
        return new CanvasImageElementDto(
            new CanvasMediaDto(contentType, 0, "", ""),
            x,
            y,
            width,
            height,
            RotationMapper.ToDto(rotation));
    }

    private static ScalableText CreateScalableTextCommand(
        int x,
        int y,
        int rotation,
        int font,
        int hMul,
        int vMul,
        char reverse,
        string text,
        int? lengthInBytes = null,
        Encoding? encoding = null)
    {
        encoding ??= Encoding.GetEncoding(437);
        var bytes = encoding.GetBytes(text);
        return new ScalableText(x, y, rotation, font, hMul, vMul, reverse, bytes)
        {
            LengthInBytes = lengthInBytes ?? text.Length + 15
        };
    }

    private static bool RegisterEncodingProvider()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return true;
    }
}
