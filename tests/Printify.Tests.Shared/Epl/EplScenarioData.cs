using System.Text;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Media;
using Printify.Domain.Printing;
using Printify.Domain.Specifications;
using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Infrastructure.Media;
using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;
using Printify.Web.Mapping;
using Xunit;
using DomainMedia = Printify.Domain.Media.Media;

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
            expectedRequestCommands: [new EplClearBuffer { LengthInBytes = 2 }],
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
            expectedRequestCommands: [new EplSetLabelWidth(500) { LengthInBytes = 5 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setLabelWidth", lengthInBytes: 5, parameters: new Dictionary<string, string> { ["Width"] = "500" })
                ]
            ]),
        new(
            id: 2002,
            input: "Q300,26\n"u8.ToArray(),
            expectedRequestCommands: [new EplSetLabelHeight(300, 26) { LengthInBytes = 8 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setLabelHeight", lengthInBytes: 8, parameters: new Dictionary<string, string> { ["Height"] = "300", ["SecondParameter"] = "26" })
                ]
            ]),
        new(
            id: 2003,
            input: "R3\n"u8.ToArray(),
            expectedRequestCommands: [new EplSetPrintSpeed(3) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setPrintSpeed", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Speed"] = "3" })
                ]
            ]),
        new(
            id: 2004,
            input: "S10\n"u8.ToArray(),
            expectedRequestCommands: [new EplSetPrintDarkness(10) { LengthInBytes = 4 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setPrintDarkness", lengthInBytes: 4, parameters: new Dictionary<string, string> { ["Darkness"] = "10" })
                ]
            ]),
        new(
            id: 2005,
            input: "ZT\n"u8.ToArray(),
            expectedRequestCommands: [new SetPrintDirection(EplPrintDirection.TopToBottom) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setPrintDirection", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Direction"] = "TopToBottom" })
                ]
            ]),
        new(
            id: 2006,
            input: "ZB\n"u8.ToArray(),
            expectedRequestCommands: [new SetPrintDirection(EplPrintDirection.BottomToTop) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setPrintDirection", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Direction"] = "BottomToTop" })
                ]
            ]),
        new(
            id: 2007,
            input: "I8\n"u8.ToArray(),
            expectedRequestCommands: [new EplSetInternationalCharacter(8, 0, 0) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setInternationalCharacter", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["P1"] = "8", ["P2"] = "0", ["P3"] = "0" })
                ]
            ]),
        new(
            id: 2008,
            input: "I8,10\n"u8.ToArray(),
            expectedRequestCommands: [new EplSetInternationalCharacter(8, 10, 0) { LengthInBytes = 6 }],
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
            id: 23001,
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
                    "Hello")
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
                    DebugElement("bufferDiscarded", lengthInBytes: 0, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = "10 bytes in buffer discarded"
                    })
                ]
            ]),
        new(
            id: 23002,
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
                    "World")
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
                    DebugElement("bufferDiscarded", lengthInBytes: 0, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = "10 bytes in buffer discarded"
                    })
                ]
            ]),
        new(
            id: 23003,
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
                    "Test123")
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
                    DebugElement("bufferDiscarded", lengthInBytes: 0, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = "14 bytes in buffer discarded"
                    })
                ]
            ]),
        // CR (carriage return) as no-op command
        new(
            id: 23004,
            input: "\r"u8.ToArray(),
            expectedRequestCommands: [new EplCarriageReturn { LengthInBytes = 1 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("carriageReturn", lengthInBytes: 1)
                ]
            ]),
        // LF (line feed) as no-op command
        new(
            id: 23005,
            input: "\n"u8.ToArray(),
            expectedRequestCommands: [new EplLineFeed { LengthInBytes = 1 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("lineFeed", lengthInBytes: 1)
                ]
            ]),
        // Clear buffer with CR terminator (not LF)
        new(
            id: 23006,
            input: "N\r"u8.ToArray(),
            expectedRequestCommands: [new EplClearBuffer { LengthInBytes = 2 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("clearBuffer", lengthInBytes: 2)
                ]
            ]),
        // Clear buffer with LF terminator (standard)
        new(
            id: 23007,
            input: "N\n"u8.ToArray(),
            expectedRequestCommands: [new EplClearBuffer { LengthInBytes = 2 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("clearBuffer", lengthInBytes: 2)
                ]
            ]),
        // Text command with CR terminator
        new(
            id: 23008,
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
                    "Test")
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
                    DebugElement("bufferDiscarded", lengthInBytes: 0, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = "8 bytes in buffer discarded"
                    })
                ]
            ]),
        // CRLF sequence - CR terminates N command, remaining LF becomes LineFeed no-op
        new(
            id: 23009,
            input: "N\r\n"u8.ToArray(),
            expectedRequestCommands:
            [
                new EplClearBuffer { LengthInBytes = 2 },
                new EplLineFeed { LengthInBytes = 1 }
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
            id: 23010,
            input: "\n\r"u8.ToArray(),
            expectedRequestCommands:
            [
                new EplLineFeed { LengthInBytes = 1 },
                new EplCarriageReturn { LengthInBytes = 1 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("lineFeed", lengthInBytes: 1),
                    DebugElement("carriageReturn", lengthInBytes: 1)
                ]
            ]),
        // Scenario: Text command followed by PRINT 1 (single copy)
        // Expected: Single canvas with debug elements first, then visual elements
        new(
            id: 23011,
            input: "A10,20,0,2,1,1,N,\"DEF\"\nP1\n"u8.ToArray(),
            expectedRequestCommands:
            [
                CreateScalableTextCommand(10, 20, 0, 2, 1, 1, 'N', "DEF"),
                new EplPrint(1) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    // Canvas 0: Debug elements first, then visual elements
                    DebugElement("scalableText", lengthInBytes: 23, parameters: new Dictionary<string, string>
                    {
                        ["X"] = "10",
                        ["Y"] = "20",
                        ["Rotation"] = "0",
                        ["Font"] = "2",
                        ["HorizontalMultiplication"] = "1",
                        ["VerticalMultiplication"] = "1",
                        ["Reverse"] = "N",
                        ["Text"] = "DEF"
                    }),
                    DebugElement("print", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Copies"] = "1" }),
                    TextElement("DEF", x: 10, y: 20,
                        width: EplSpecs.Fonts.Font2.BaseWidthInDots * "DEF".Length, height: EplSpecs.Fonts.Font2.BaseHeightInDots,
                        fontName: EplSpecs.Fonts.Font2.FontName, charScaleX: 1, charScaleY: 1, rotation: 0, isReverse: false)
                ]
            ]),
        // Scenario: Text command followed by PRINT 2 (two copies)
        // Expected: First canvas with debug+visual, second canvas with only visual
        new(
            id: 23012,
            input: "A10,20,0,2,1,1,N,\"XYZ\"\nP2\n"u8.ToArray(),
            expectedRequestCommands:
            [
                CreateScalableTextCommand(10, 20, 0, 2, 1, 1, 'N', "XYZ"),
                new EplPrint(2) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    // Canvas 0: Debug elements first, then visual elements
                    DebugElement("scalableText", lengthInBytes: 23, parameters: new Dictionary<string, string>
                    {
                        ["X"] = "10",
                        ["Y"] = "20",
                        ["Rotation"] = "0",
                        ["Font"] = "2",
                        ["HorizontalMultiplication"] = "1",
                        ["VerticalMultiplication"] = "1",
                        ["Reverse"] = "N",
                        ["Text"] = "XYZ"
                    }),
                    DebugElement("print", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Copies"] = "2" }),
                    TextElement("XYZ", x: 10, y: 20,
                        width: EplSpecs.Fonts.Font2.BaseWidthInDots * "XYZ".Length, height: EplSpecs.Fonts.Font2.BaseHeightInDots,
                        fontName: EplSpecs.Fonts.Font2.FontName, charScaleX: 1, charScaleY: 1, rotation: 0, isReverse: false)
                ],
                [
                    // Canvas 1: Only visual elements (no debug)
                    TextElement("XYZ", x: 10, y: 20,
                        width: EplSpecs.Fonts.Font2.BaseWidthInDots * "XYZ".Length, height: EplSpecs.Fonts.Font2.BaseHeightInDots,
                        fontName: EplSpecs.Fonts.Font2.FontName, charScaleX: 1, charScaleY: 1, rotation: 0, isReverse: false)
                ]
            ])
    ];

    public static TheoryData<EplScenario> BarcodeScenarios { get; } =
    [
        new(
            id: 4001,
            input: "B10,50,0,E30,2,100,B,\"123456789012\"\n"u8.ToArray(),
            expectedRequestCommands:
            [
                // Note: Barcode media is dynamically generated by the barcode service, so we can't predict exact content
                // The DocumentAssertions.EplPrintBarcodeUpload case only verifies structure, not pixels
                new EplPrintBarcodeUpload(10, 50, 0, "E30", 2, 100, 'B', "123456789012", CreateExpectedBarcodeMedia()) { LengthInBytes = 36 }
            ],
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
                    DebugElement("bufferDiscarded", lengthInBytes: 0, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = "0 bytes in buffer discarded"
                    })
                ]
            ]),
        new(
            id: 4002,
            input: "B20,80,1,2A,3,120,N,\"ABC123\"\n"u8.ToArray(),
            expectedRequestCommands:
            [
                new EplPrintBarcodeUpload(20, 80, 1, "2A", 3, 120, 'N', "ABC123", CreateExpectedBarcodeMedia()) { LengthInBytes = 29 }
            ],
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
                    DebugElement("bufferDiscarded", lengthInBytes: 0, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = "0 bytes in buffer discarded"
                    })
                ]
            ])
    ];

    /// <summary>
    /// GW (Graphic Write) image scenarios for EPL protocol.
    /// Similar to EscPos GS v 0 raster bit image printing.
    /// Format: GW x,y,bytesPerRow,height,[binary data]
    /// </summary>
    public static TheoryData<EplScenario> GraphicScenarios { get; } =
    [
        // GW: 8x2 partially set image (3 pixels + 2 pixels set)
        // Row 0: 11100000 (3 colored, 5 transparent)
        // Row 1: 00011000 (2 colored at positions 3-4)
        new(
            id: 3101,
            input:
            [
                .."GW10,50,1,2,"u8.ToArray(),  // GW x,y,bytesPerRow,height,
                0b11100000,  // Row 0: XXX_____ (X=colored/set, _=transparent/unset)
                0b00011000,  // Row 1: ___XX___ (X=colored/set, _=transparent/unset)
                0x0A          // LF terminator
            ],
            expectedRequestCommands:
            [
                new EplRasterImageUpload(10, 50, 8, 2, CreateExpectedRasterMedia(8, 2, [0b11100000, 0b00011000])) { LengthInBytes = 15 }
            ],
            expectedPersistedCommands:
            [
                new EplRasterImage(10, 50, 8, 2, DomainMedia.CreateDefaultPng(96)) { LengthInBytes = 15 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImage", lengthInBytes: 15),
                    CanvasImageElement(10, 50, 8, 2, DomainMedia.CreateDefaultPng(96), lengthInBytes: 15)
                ]
            ]),

        // GW: 8x2 all bits set (8x2, all colored pixels)
        new(
            id: 3102,
            input:
            [
                .."GW10,60,1,2,"u8.ToArray(),
                0xFF,  // Row 0: All colored
                0xFF,  // Row 1: All colored
                0x0A
            ],
            expectedRequestCommands:
            [
                new EplRasterImageUpload(10, 60, 8, 2, CreateExpectedRasterMedia(8, 2, [0xFF, 0xFF])) { LengthInBytes = 15 }
            ],
            expectedPersistedCommands:
            [
                new EplRasterImage(10, 60, 8, 2, DomainMedia.CreateDefaultPng(96)) { LengthInBytes = 15 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImage", lengthInBytes: 15),
                    CanvasImageElement(10, 60, 8, 2, DomainMedia.CreateDefaultPng(96), lengthInBytes: 15)
                ]
            ]),

        // GW: 8x2 all bits unset (8x2, all transparent pixels)
        new(
            id: 3103,
            input:
            [
                .."GW10,70,1,2,"u8.ToArray(),
                0x00,  // Row 0: All transparent
                0x00,  // Row 1: All transparent
                0x0A
            ],
            expectedRequestCommands:
            [
                new EplRasterImageUpload(10, 70, 8, 2, CreateExpectedRasterMedia(8, 2, [0x00, 0x00])) { LengthInBytes = 15 }
            ],
            expectedPersistedCommands:
            [
                new EplRasterImage(10, 70, 8, 2, DomainMedia.CreateDefaultPng(85)) { LengthInBytes = 15 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImage", lengthInBytes: 15),
                    CanvasImageElement(10, 70, 8, 2, DomainMedia.CreateDefaultPng(85), lengthInBytes: 15)
                ]
            ]),

        // GW: 8x2 checkerboard pattern
        new(
            id: 3104,
            input:
            [
                .."GW10,80,1,2,"u8.ToArray(),
                0b10101010,  // Checkerboard pattern row 0
                0b01010101,  // Checkerboard pattern row 1
                0x0A
            ],
            expectedRequestCommands:
            [
                new EplRasterImageUpload(10, 80, 8, 2, CreateExpectedRasterMedia(8, 2, [0b10101010, 0b01010101])) { LengthInBytes = 15 }
            ],
            expectedPersistedCommands:
            [
                new EplRasterImage(10, 80, 8, 2, DomainMedia.CreateDefaultPng(93)) { LengthInBytes = 15 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImage", lengthInBytes: 15),
                    CanvasImageElement(10, 80, 8, 2, DomainMedia.CreateDefaultPng(93), lengthInBytes: 15)
                ]
            ])
    ];

    public static TheoryData<EplScenario> ShapeScenarios { get; } =
    [
        new(
            id: 6001,
            input: "LO10,20,2,100\n"u8.ToArray(),
            expectedRequestCommands: [new EplDrawHorizontalLine(10, 20, 2, 100) { LengthInBytes = 14 }],
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
            expectedRequestCommands: [new EplDrawBox(5, 10, 1, 200, 50) { LengthInBytes = 15 }],
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
            expectedRequestCommands: [new EplPrint(1) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("print", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Copies"] = "1" })
                ]
            ]),
        new(
            id: 7002,
            input: "P5\n"u8.ToArray(),
            expectedRequestCommands: [new EplPrint(5) { LengthInBytes = 3 }],
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

    /// <summary>
    /// Warning scenarios for when visual elements are in buffer but never printed.
    /// In page mode (EPL), visual elements must be followed by a Print command to be rendered.
    /// If visual elements exist without a Print command, a warning is added.
    /// </summary>
    public static TheoryData<EplScenario> WarningScenarios { get; } =
    [
        // Scenario: Text command without Print command - buffer discarded
        new(
            id: 9001,
            input: "A10,20,0,2,1,1,N,\"ABC\"\n"u8.ToArray(),
            expectedRequestCommands:
            [
                CreateScalableTextCommand(10, 20, 0, 2, 1, 1, 'N', "ABC")
            ],
            expectedCanvasElements:
            [
                [
                    // Canvas with debug elements + warning, NO visual elements
                    DebugElement("scalableText", lengthInBytes: 24, parameters: new Dictionary<string, string>
                    {
                        ["X"] = "10",
                        ["Y"] = "20",
                        ["Rotation"] = "0",
                        ["Font"] = "2",
                        ["HorizontalMultiplication"] = "1",
                        ["VerticalMultiplication"] = "1",
                        ["Reverse"] = "N",
                        ["Text"] = "ABC"
                    }),
                    DebugElement("bufferDiscarded", lengthInBytes: 0, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = "3 bytes in buffer discarded"
                    })
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
        AddRange(data, WarningScenarios);
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

    private static EplScalableText CreateScalableTextCommand(
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
        return new EplScalableText(x, y, rotation, font, hMul, vMul, reverse, bytes)
        {
            //A10,20,0,2,1,1,N,"ABC"\n
            LengthInBytes = lengthInBytes ??
                            text.Length +
                            x.ToString().Length +
                            y.ToString().Length +
                            rotation.ToString().Length +
                            font.ToString().Length +
                            hMul.ToString().Length +
                            vMul.ToString().Length + 12
        };
    }

    private static bool RegisterEncodingProvider()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return true;
    }

    /// <summary>
    /// Creates expected barcode media using the EPL barcode service.
    /// The barcode content is dynamically generated, so this creates a valid placeholder.
    /// </summary>
    private static MediaUpload CreateExpectedBarcodeMedia()
    {
        // Use a minimal valid PNG as placeholder for barcode media
        // The actual content verification is skipped in DocumentAssertions for barcode uploads
        return new MediaUpload("image/png", "test-placeholder"u8.ToArray());
    }

    /// <summary>
    /// Creates expected raster image media by converting MonochromeBitmap to PNG.
    /// This generates the exact expected output for pixel verification.
    /// </summary>
    private static MediaUpload CreateExpectedRasterMedia(int width, int height, byte[] bitmapData)
    {
        var bitmap = new MonochromeBitmap(width, height, bitmapData);
        var mediaService = new MediaService();
        return mediaService.ConvertToMediaUpload(bitmap);
    }

    /// <summary>
    /// Creates a canvas image element for testing.
    /// </summary>
    private static CanvasImageElementDto CanvasImageElement(
        int x,
        int y,
        int width,
        int height,
        DomainMedia media,
        int lengthInBytes,
        Rotation rotation = Rotation.None)
    {
        return new CanvasImageElementDto(
            new CanvasMediaDto(media.ContentType, (int)media.Length, media.Url, media.Sha256Checksum),
            x,
            y,
            width,
            height,
            RotationMapper.ToDto(rotation))
        {
            LengthInBytes = lengthInBytes
        };
    }
}
