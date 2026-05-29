using System.Text;
using EscPosCommands = Printify.Infrastructure.Printing.EscPos.Commands;
using Printify.Domain.Media;
using Printify.Domain.Printing;
using Printify.Domain.Specifications;
using Printify.Infrastructure.Media;
using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;
using Xunit;

namespace Printify.Tests.Shared.EscPos;

/// <summary>
/// Provides reusable ESC/POS parser scenarios for unit and integration tests.
/// </summary>
public static class EscPosScenarioData
{
    private const byte Esc = 0x1B;
    private const byte Gs = 0x1D;
    private const byte Lf = 0x0A;
    private const byte Cr = 0x0D;

    private static readonly bool EncodingProviderRegistered = RegisterEncodingProvider();

    static EscPosScenarioData()
    {
        var codePageVectors = BuildCodePageVectors();
        CodePageScenarios = BuildCodePageScenarios(codePageVectors);
        AllScenarios = BuildAllScenarios();
    }

    public static TheoryData<EscPosScenario> BellScenarios { get; } =
    [
        new(
            id: 15001,
            input: [0x07],
            expectedRequestCommands: [new EscPosCommands.EscPosBell { LengthInBytes = 1 }],
            expectedCanvasElements:
            [
                [
                    new CanvasDebugElementDto("bell")
                    {
                        LengthInBytes = 1
                    }
                ]
            ]),
        new(
            id: 15002,
            input: Enumerable.Repeat((byte)0x07, 10).ToArray(),
            expectedRequestCommands: Enumerable.Range(0, 10).Select(_ => new EscPosCommands.EscPosBell { LengthInBytes = 1 }).ToArray(),
            expectedCanvasElements:
            [
                Enumerable.Range(0, 10)
                    .Select(_ => new CanvasDebugElementDto("bell")
                    {
                        LengthInBytes = 1
                    })
                    .ToArray()
            ])
    ];

    public static TheoryData<EscPosScenario> TextScenarios { get; } =
    [
        new(
            id: 15003,
            input: "A"u8.ToArray(),
            expectedRequestCommands: [CommandAppendText("A")],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("A", lengthInBytes: 1),
                    DebugDiscardedError()
                ]
            ]),
        new(
            id: 15004,
            input: "ABC\n"u8.ToArray(),
            expectedRequestCommands:
            [
                CommandAppendText("ABC"),
                CommandPrintAndLineFeed(),
            ],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("ABC", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: 0, lengthInBytes: 3)
                ]
            ]),
        new(
            id : 15005,
            input: [.. "ABC"u8, Cr, Lf],
            expectedRequestCommands:
            [
                CommandAppendText("ABC"),
                new EscPosCommands.EscPosLegacyCarriageReturn { LengthInBytes = 1 },
                CommandPrintAndLineFeed(),
            ],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugElement("legacyCarriageReturn", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("ABC", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: 0, lengthInBytes: 3)
                ]
            ]),
        new(
            id: 15006,
            input: "ABC"u8.ToArray(),
            expectedRequestCommands: [CommandAppendText("ABC")],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugDiscardedError()
                ]
            ]),
        new(
            id: 15007,
            input: "ABC"u8.ToArray(),
            expectedRequestCommands: [CommandAppendText("ABC")],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugDiscardedError()
                ]
            ]),
        new(
            id: 15008,
            input: "ABC\nDEF\nG"u8.ToArray(),
            expectedRequestCommands:
            [
                CommandAppendText("ABC"),
                CommandPrintAndLineFeed(),
                CommandAppendText("DEF"),
                CommandPrintAndLineFeed(),
                CommandAppendText("G")
            ],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("ABC", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: 0, lengthInBytes: 3),
                    DebugAppendText("DEF", lengthInBytes: 3),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("DEF", 
                        fontName: EscPosSpecs.Fonts.FontA.FontName,
                        x: 0,
                        y: EscPosSpecs.Fonts.FontA.HeightInDots + DefaultLineSpacing,
                        lengthInBytes: 3),
                    DebugAppendText("G", lengthInBytes: 1),
                    DebugDiscardedError()
                ]
            ]),
        new(
            id: 15009,
            input: "ABC"u8.ToArray(),
            expectedRequestCommands: [CommandAppendText("ABC")],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugDiscardedError()
                ]
            ]),
        new(
            id: 15010,
            input: Encoding.ASCII.GetBytes(new string('A', 100)),
            expectedRequestCommands: [CommandAppendText(new string('A', 100))],
            expectedCanvasElements:
            [
                [
                    DebugAppendText(new string('A', 100), lengthInBytes: 100),
                    DebugDiscardedError()
                ]
            ]),
        new(
            id: 15011,
            input: [.. "ABC"u8, 0x07],
            expectedRequestCommands:
            [
                CommandAppendText("ABC"),
                new EscPosCommands.EscPosBell { LengthInBytes = 1 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugElement("bell", lengthInBytes: 1),
                    DebugDiscardedError()
                ]
            ]),
        new(
            id: 15012,
            input: [.. "ABC"u8, 0x07, .. "DEF"u8, 0x07],
            expectedRequestCommands:
            [
                CommandAppendText("ABC"),
                new EscPosCommands.EscPosBell { LengthInBytes = 1 },
                CommandAppendText("DEF"),
                new EscPosCommands.EscPosBell { LengthInBytes = 1 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugElement("bell", lengthInBytes: 1),
                    DebugAppendText("DEF", lengthInBytes: 3),
                    DebugElement("bell", lengthInBytes: 1),
                    DebugDiscardedError()
                ]
            ]),
        new(
            id: 15013,
            input: [.. "ABC"u8, 0x07, .. "DEF\n"u8, 0x07],
            expectedRequestCommands:
            [
                CommandAppendText("ABC"),
                new EscPosCommands.EscPosBell { LengthInBytes = 1 },
                CommandAppendText("DEF"),
                CommandPrintAndLineFeed(),
                new EscPosCommands.EscPosBell { LengthInBytes = 1 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugElement("bell", lengthInBytes: 1),
                    DebugAppendText("DEF", lengthInBytes: 3),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("ABC", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: 0, lengthInBytes: 3),
                    TextElement("DEF", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 36, y: 0, lengthInBytes: 3),
                    DebugElement("bell", lengthInBytes: 1)
                ]
            ]),
        new(
            id: 15016,
            input: [.. "ABC"u8, Esc, (byte)'i', .. "DEF\n"u8],
            expectedRequestCommands:
            [
                CommandAppendText("ABC"),
                new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.PartialOnePoint) { LengthInBytes = 2 },
                CommandAppendText("DEF"),
                CommandPrintAndLineFeed(),
            ],
            expectedCanvasElements:
            [
                // Canvas 1: ABC added to buffer (not flushed) + pagecut debug
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugDiscardedError(),
                    DebugElement(
                        "pagecut",
                        lengthInBytes: 2,
                        parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.PartialOnePoint, null))
                ],
                // Canvas 2: DEF added to buffer, then flushed. Note: ABC from canvas 1 is lost (not flushed)
                [
                    DebugAppendText("DEF", lengthInBytes: 3),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("DEF", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: 0, lengthInBytes: 3)
                ]
            ]),
        new(
            id: 15017,
            input:
            [
                .. "ABC"u8,
                Gs, (byte)'v', 0x30, 0x00,
                0x01, 0x00,
                0x02, 0x00,
                0x00, 0x00,
                .. "DEF\n"u8
            ],
            expectedRequestCommands:
            [
                CommandAppendText("ABC"),
                new EscPosCommands.EscPosRasterImageUploadGs7630(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0x00, 0x00]))
                { LengthInBytes = 10 },
                CommandAppendText("DEF"),
                CommandPrintAndLineFeed(),
            ],
            expectedPersistedCommands:
            [
                CommandAppendText("ABC"),
                new EscPosCommands.EscPosRasterImageGs7630(8, 2, Media.CreateDefaultPng(85)) { LengthInBytes = 10 },
                CommandAppendText("DEF"),
                CommandPrintAndLineFeed(),
            ],
            expectedCanvasElements:
            [
                [
                    // "ABC" added to buffer
                    DebugElement("appendToLineBuffer", parameters: new Dictionary<string, string> { ["Text"] = "ABC" }, lengthInBytes: 3),
                    // Buffer cleared by image - synthetic error for lost data
                    new CanvasDebugElementDto("printerError")
                    {
                        LengthInBytes = 0,
                        Parameters = new Dictionary<string, string>
                        {
                            ["Message"] = "Text buffer cleared by raster image command, 3 bytes lost (\"ABC\")"
                        }
                    },
                    // The image that cleared the buffer
                    DebugElement("rasterImageGs7630", lengthInBytes: 10),
                    ViewImage(0, 0, 8, 2, Media.CreateDefaultPng(85), lengthInBytes: 10),
                    // "DEF" added to fresh buffer
                    DebugElement("appendToLineBuffer", parameters: new Dictionary<string, string> { ["Text"] = "DEF" }, lengthInBytes: 3),
                    // Flush - only "DEF" prints (positioned below the image)
                    DebugElement("flushLineBufferAndFeed", lengthInBytes: 1),
                    TextElement("DEF", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: 2, lengthInBytes: 3)  // Y=2 because image (height=2) is above
                ]
            ]),
        new(
            id: 15014,
            input: "\n"u8.ToArray(),
            expectedRequestCommands: [CommandPrintAndLineFeed()],
            expectedCanvasElements:
            [
                [
                    DebugFlush(lengthInBytes: 1)
                ]
            ]),
        new(
            id: 15015,
            input: "\n\n\n"u8.ToArray(),
            expectedRequestCommands:
            [
                CommandPrintAndLineFeed(),
                CommandPrintAndLineFeed(),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugFlush(lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1)
                ]
            ])
    ];

    public static TheoryData<EscPosScenario> ErrorScenarios { get; } =
    [
        // Single null byte produces one error
        new(
            id: 16001,
            input: [0x00],
            expectedRequestCommands: [new EscPosCommands.EscPosParseError("ESCPOS_PARSER_ERROR", "Unrecognized command: 0x00") { LengthInBytes = 1 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("error", lengthInBytes: 1, parameters: new Dictionary<string, string>
                    {
                        ["Code"] = "ESCPOS_PARSER_ERROR",
                        ["Message"] = "Unrecognized command: 0x00"
                    })
                ]
            ]),
        // Two consecutive null bytes produce one error (accumulated)
        new(
            id: 16002,
            input: [0x00, 0x00],
            expectedRequestCommands: [new EscPosCommands.EscPosParseError("ESCPOS_PARSER_ERROR", "Unrecognized command: 0x00 0x00") { LengthInBytes = 2 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("error", lengthInBytes: 2, parameters: new Dictionary<string, string>
                    {
                        ["Code"] = "ESCPOS_PARSER_ERROR",
                        ["Message"] = "Unrecognized command: 0x00 0x00"
                    })
                ]
            ]),
        // Multiple invalid bytes produce one error
        new(
            id: 16003,
            input: [0x00, 0x01, 0x02],
            expectedRequestCommands: [new EscPosCommands.EscPosParseError("ESCPOS_PARSER_ERROR", "Unrecognized command: 0x00 0x01 0x02") { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("error", lengthInBytes: 3, parameters: new Dictionary<string, string>
                    {
                        ["Code"] = "ESCPOS_PARSER_ERROR",
                        ["Message"] = "Unrecognized command: 0x00 0x01 0x02"
                    })
                ]
            ]),
        // Invalid byte followed by text transitions correctly
        new(
            id: 160004,
            input: [0x00, .. "ABC"u8],
            expectedRequestCommands: [
                new EscPosCommands.EscPosParseError("ESCPOS_PARSER_ERROR", "Unrecognized command: 0x00") { LengthInBytes = 1 },
                CommandAppendText("ABC")],
            expectedCanvasElements:
            [
                [
                    DebugElement("error", lengthInBytes: 1, parameters: new Dictionary<string, string>
                    {
                        ["Code"] = "ESCPOS_PARSER_ERROR",
                        ["Message"] = "Unrecognized command: 0x00"
                    }),
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugDiscardedError()
                ]
            ]),
        // Text followed by invalid byte followed by text
        new(
            id: 160005,
            input: [.. "ABC"u8, 0x00, .. "DEF"u8],
            expectedRequestCommands: [
                CommandAppendText("ABC"),
                new EscPosCommands.EscPosParseError("ESCPOS_PARSER_ERROR", "Unrecognized command: 0x00") { LengthInBytes = 1 },
                CommandAppendText("DEF")],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugElement("error", lengthInBytes: 1, parameters: new Dictionary<string, string>
                    {
                        ["Code"] = "ESCPOS_PARSER_ERROR",
                        ["Message"] = "Unrecognized command: 0x00"
                    }),
                    DebugAppendText("DEF", lengthInBytes: 3),
                    DebugDiscardedError()
                ]
            ]),
        // Invalid byte followed by command
        new(
            id: 160006,
            input: [0x00, 0x07],
            expectedRequestCommands: [
                new EscPosCommands.EscPosParseError("ESCPOS_PARSER_ERROR", "Unrecognized command: 0x00") { LengthInBytes = 1 },
                new EscPosCommands.EscPosBell { LengthInBytes = 1 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("error", lengthInBytes: 1, parameters: new Dictionary<string, string>
                    {
                        ["Code"] = "ESCPOS_PARSER_ERROR",
                        ["Message"] = "Unrecognized command: 0x00"
                    }),
                    DebugElement("bell", lengthInBytes: 1)
                ]
            ]),
        // Reset followed by unknown ESC command, then pagecut
        // Sequence: ESC @ (reset), ESC 0xFF (unknown), ESC i (partial cut)
        // Bug expected: ESC 0xFF may not properly emit error before pagecut
        new(
            id: 160007,
            input: [Esc, 0x40, Esc, 0xFF, Esc, (byte)'i'],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosInitialize { LengthInBytes = 2 },
                new EscPosCommands.EscPosParseError("ESCPOS_PARSER_ERROR", "Unrecognized command: ESC 0xFF") { LengthInBytes = 2 },
                new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.PartialOnePoint) { LengthInBytes = 2 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("reset", lengthInBytes: 2),
                    DebugElement("error", lengthInBytes: 2, parameters: new Dictionary<string, string>
                    {
                        ["Code"] = "ESCPOS_PARSER_ERROR",
                        ["Message"] = "Unrecognized command: ESC 0xFF"
                    }),
                    DebugElement("pagecut", lengthInBytes: 2, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.PartialOnePoint, null))
                ]
            ])
    ];

    public static TheoryData<EscPosScenario> PagecutScenarios { get; } =
    [
        new(
            id: 170001,
            input: [Esc, (byte)'i'],
            expectedRequestCommands: [new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.PartialOnePoint) { LengthInBytes = 2 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pagecut", lengthInBytes: 2, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.PartialOnePoint, null))
                ]
            ]),
        new(
            id: 170002,
            input: [Gs, 0x56, 0x00],
            expectedRequestCommands: [new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Full) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pagecut", lengthInBytes: 3, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.Full, null))
                ]
            ]),
        new(
            id: 170003,
            input: [Gs, 0x56, 0x30],
            expectedRequestCommands: [new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Full) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pagecut", lengthInBytes: 3, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.Full, null))
                ]
            ]),
        new(
            id: 170004,
            input: [Gs, 0x56, 0x01],
            expectedRequestCommands: [new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pagecut", lengthInBytes: 3, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.Partial, null))
                ]
            ]),
        new(
            id: 170005,
            input: [Gs, 0x56, 0x31],
            expectedRequestCommands: [new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pagecut", lengthInBytes: 3, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.Partial, null))
                ]
            ]),
        new(
            id: 170006,
            input: [Gs, 0x56, 0x41, 0x05],
            expectedRequestCommands: [new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Full, 0x05) { LengthInBytes = 4 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pagecut", lengthInBytes: 4, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.Full, 0x05))
                ]
            ]),
        new(
            id: 170007,
            input: [Gs, 0x56, 0x42, 0x20],
            expectedRequestCommands: [new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial, 0x20) { LengthInBytes = 4 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pagecut", lengthInBytes: 4, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.Partial, 0x20))
                ]
            ]),
        new(
            id: 170008,
            input: [Gs, 0x56, 0x61, 0x05],
            expectedRequestCommands: [new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Full, 0x05) { LengthInBytes = 4 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pagecut", lengthInBytes: 4, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.Full, 0x05))
                ]
            ]),
        new(
            id: 170009,
            input: [Gs, 0x56, 0x62, 0x20],
            expectedRequestCommands: [new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial, 0x20) { LengthInBytes = 4 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pagecut", lengthInBytes: 4, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.Partial, 0x20))
                ]
            ]),
        new(
            id: 170010,
            input: [Gs, 0x56, 0x67, 0x05],
            expectedRequestCommands: [new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Full, 0x05) { LengthInBytes = 4 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pagecut", lengthInBytes: 4, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.Full, 0x05))
                ]
            ]),
        new(
            id: 170011,
            input: [Gs, 0x56, 0x68, 0x20],
            expectedRequestCommands: [new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial, 0x20) { LengthInBytes = 4 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pagecut", lengthInBytes: 4, parameters: PagecutParameters(EscPosCommands.EscPosPagecutMode.Partial, 0x20))
                ]
            ])
    ];

    public static TheoryData<EscPosScenario> PulseScenarios { get; } =
    [
        new(
            id: 180001,
            input: [Esc, (byte)'p', 0x01, 0x05, 0x0A],
            expectedRequestCommands: [new EscPosCommands.EscPosPulse(1, 0x05, 0x0A) { LengthInBytes = 5 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pulse", lengthInBytes: 5, parameters: new Dictionary<string, string>
                    {
                        ["Pin"] = "1",
                        ["OnTimeMs"] = "5",
                        ["OffTimeMs"] = "10"
                    })
                ]
            ]),
        new(
            id: 180002,
            input: [Esc, (byte)'p', 0x00, 0x7D, 0x7F],
            expectedRequestCommands: [new EscPosCommands.EscPosPulse(0, 0x7D, 0x7F) { LengthInBytes = 5 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("pulse", lengthInBytes: 5, parameters: new Dictionary<string, string>
                    {
                        ["Pin"] = "0",
                        ["OnTimeMs"] = "125",
                        ["OffTimeMs"] = "127"
                    })
                ]
            ]),
        new(
            id: 180003,
            input:
            [
                Esc, (byte)'p', 0x00, 0x08, 0x16,
                Esc, (byte)'p', 0x01, 0x02, 0x03
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosPulse(0, 0x08, 0x16) { LengthInBytes = 5 },
                new EscPosCommands.EscPosPulse(1, 0x02, 0x03) { LengthInBytes = 5 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("pulse", lengthInBytes: 5, parameters: new Dictionary<string, string>
                    {
                        ["Pin"] = "0",
                        ["OnTimeMs"] = "8",
                        ["OffTimeMs"] = "22"
                    }),
                    DebugElement("pulse", lengthInBytes: 5, parameters: new Dictionary<string, string>
                    {
                        ["Pin"] = "1",
                        ["OnTimeMs"] = "2",
                        ["OffTimeMs"] = "3"
                    })
                ]
            ])
    ];

    public static TheoryData<EscPosScenario> BarcodeScenarios { get; } =
    [
        new(
            id: 185001,
            input:
            [
                Gs, 0x48, 0x02,
                Gs, 0x6B, 0x08, 0x7B,
                (byte)'B', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7',
                (byte)'8', (byte)'9', (byte)'0', (byte)'1', (byte)'2',
                0x00,
                Lf
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetBarcodeLabelPosition(EscPosCommands.EscPosBarcodeLabelPosition.Below)
                {
                    LengthInBytes = 3
                },
                new EscPosCommands.EscPosPrintBarcodeUpload(EscPosCommands.EscPosBarcodeSymbology.Code128, "123456789012")
                {
                    LengthInBytes = 18
                },
                new EscPosCommands.EscPosPrintAndLineFeed
                {
                    LengthInBytes = 1
                }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosSetBarcodeLabelPosition(EscPosCommands.EscPosBarcodeLabelPosition.Below)
                {
                    LengthInBytes = 3
                },
                new EscPosCommands.EscPosPrintBarcode(
                    EscPosCommands.EscPosBarcodeSymbology.Code128,
                    "123456789012",
                    0,
                    0,
                    Media.CreateDefaultPng(1))
                {
                    LengthInBytes = 18
                },
                new EscPosCommands.EscPosPrintAndLineFeed
                {
                    LengthInBytes = 1
                }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement(
                        "setBarcodeLabelPosition",
                        lengthInBytes: 3,
                        parameters: new Dictionary<string, string>
                        {
                            ["Position"] = EscPosCommands.EscPosBarcodeLabelPosition.Below.ToString()
                        }),
                    DebugElement("printBarcode", lengthInBytes: 18),
                    ViewImage(0, 0, 0, 0, Media.CreateDefaultPng(1), lengthInBytes: 18),
                    DebugFlush(lengthInBytes: 1)
                ]
            ]),
        new(
            id: 185002,
            input:
            [
                Gs, 0x6B, 0x43, 0x0D,
                (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7',
                (byte)'8', (byte)'9', (byte)'0', (byte)'1', (byte)'2', (byte)'8',
                Lf,
                Gs, 0x6B, 0x08, 0x7B,
                (byte)'B', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7',
                (byte)'8', (byte)'9', (byte)'0', (byte)'1', (byte)'2',
                0x00,
                Lf
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosPrintBarcodeUpload(EscPosCommands.EscPosBarcodeSymbology.Ean13, "1234567890128")
                {
                    LengthInBytes = 17
                },
                new EscPosCommands.EscPosPrintAndLineFeed
                {
                    LengthInBytes = 1
                },
                new EscPosCommands.EscPosPrintBarcodeUpload(EscPosCommands.EscPosBarcodeSymbology.Code128, "123456789012")
                {
                    LengthInBytes = 18
                },
                new EscPosCommands.EscPosPrintAndLineFeed
                {
                    LengthInBytes = 1
                }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosPrintBarcode(
                    EscPosCommands.EscPosBarcodeSymbology.Ean13,
                    "1234567890128",
                    0,
                    0,
                    Media.CreateDefaultPng(1))
                {
                    LengthInBytes = 17
                },
                new EscPosCommands.EscPosPrintAndLineFeed
                {
                    LengthInBytes = 1
                },
                new EscPosCommands.EscPosPrintBarcode(
                    EscPosCommands.EscPosBarcodeSymbology.Code128,
                    "123456789012",
                    0,
                    0,
                    Media.CreateDefaultPng(1))
                {
                    LengthInBytes = 18
                },
                new EscPosCommands.EscPosPrintAndLineFeed
                {
                    LengthInBytes = 1
                }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("printBarcode", lengthInBytes: 17),
                    ViewImage(0, 0, 0, 0, Media.CreateDefaultPng(1), lengthInBytes: 17),
                    DebugFlush(lengthInBytes: 1),
                    DebugElement("printBarcode", lengthInBytes: 18),
                    ViewImage(0, 0, 0, 0, Media.CreateDefaultPng(1), lengthInBytes: 18),
                    DebugFlush(lengthInBytes: 1)
                ]
            ])
    ];

    public static TheoryData<EscPosScenario> RasterImageScenarios { get; } =
    [
        // GS v 0: Print raster bit image - 8x2 partially set (with pixel verification)
        // Row 0: 11100000 (3 colored, 5 transparent)
        // Row 1: 00011000 (2 colored at positions 3-4)
        new(
            id: 190001,
            input:
            [
                Gs, (byte)'v', 0x30, 0x00, // GS v 0 m: Print raster, m=0 (normal mode)
                0x01, 0x00, // xL xH: width in bytes (1 byte = 8 dots)
                0x02, 0x00, // yL yH: height in dots (2 rows)
                0b11100000, // Row 0: XXX_____ (X=colored/set, _=transparent/unset)
                0b00011000  // Row 1: ___XX___ (X=colored/set, _=transparent/unset)
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosRasterImageUploadGs7630(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0b11100000, 0b00011000]))
                { LengthInBytes = 10 }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImageGs7630(8, 2, Media.CreateDefaultPng(96)) { LengthInBytes = 10 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImageGs7630", lengthInBytes: 10),
                    ViewImage(0, 0, 8, 2, Media.CreateDefaultPng(96), lengthInBytes: 10)
                ]
            ]),

        // GS v 0: All bits set (8x2, all colored pixels)
        new(
            id: 190002,
            input:
            [
                Gs, (byte)'v', 0x30, 0x00,
                0x01, 0x00, // width: 1 byte = 8 pixels
                0x02, 0x00, // height: 2 rows
                0xFF,       // Row 0: all colored
                0xFF        // Row 1: all colored
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosRasterImageUploadGs7630(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0xFF, 0xFF]))
                { LengthInBytes = 10 }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImageGs7630(8, 2, Media.CreateDefaultPng(96)) { LengthInBytes = 10 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImageGs7630", lengthInBytes: 10),
                    ViewImage(0, 0, 8, 2, Media.CreateDefaultPng(96), lengthInBytes: 10)
                ]
            ]),

        // GS v 0: All bits unset (8x2, all transparent pixels)
        new(
            id: 190003,
            input:
            [
                Gs, (byte)'v', 0x30, 0x00,
                0x01, 0x00, // width: 1 byte = 8 pixels
                0x02, 0x00, // height: 2 rows
                0x00,       // Row 0: all transparent
                0x00        // Row 1: all transparent
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosRasterImageUploadGs7630(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0x00, 0x00]))
                { LengthInBytes = 10 }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImageGs7630(8, 2, Media.CreateDefaultPng(85)) { LengthInBytes = 10 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImageGs7630", lengthInBytes: 10),
                    ViewImage(0, 0, 8, 2, Media.CreateDefaultPng(85), lengthInBytes: 10)
                ]
            ]),

        // GS v 0: Checkerboard pattern (8x2)
        new(
            id: 190004,
            input:
            [
                Gs, (byte)'v', 0x30, 0x00,
                0x01, 0x00, // width: 1 byte = 8 pixels
                0x02, 0x00, // height: 2 rows
                0b10101010, // Row 0: X_X_X_X_
                0b01010101  // Row 1: _X_X_X_X
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosRasterImageUploadGs7630(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0b10101010, 0b01010101]))
                { LengthInBytes = 10 }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImageGs7630(8, 2, Media.CreateDefaultPng(93)) { LengthInBytes = 10 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImageGs7630", lengthInBytes: 10),
                    ViewImage(0, 0, 8, 2, Media.CreateDefaultPng(93), lengthInBytes: 10)
                ]
            ]),

        // GS v 0: Three consecutive raster images (heights: 3, 2, 1 dots)
        // Image 1: 8x3 - top row colored, middle row checkerboard, bottom row transparent
        // Image 2: 8x2 - first row colored, second row transparent
        // Image 3: 8x1 - single row checkerboard
        new(
            id: 190005,
            input:
            [
                // Image 1: 8 wide, 3 tall (header: 4 bytes + 4 dim bytes + 3 data bytes = 11 bytes)
                Gs, (byte)'v', 0x30, 0x00,
                0x01, 0x00, // width: 1 byte = 8 pixels
                0x03, 0x00, // height: 3 rows
                0b11111111, // Row 0: XXXXXXXX (all colored)
                0b10101010, // Row 1: X_X_X_X_ (checkerboard)
                0b00000000, // Row 2: ________ (all transparent)

                // Image 2: 8 wide, 2 tall (header: 4 bytes + 4 dim bytes + 2 data bytes = 10 bytes)
                Gs, (byte)'v', 0x30, 0x00,
                0x01, 0x00, // width: 1 byte = 8 pixels
                0x02, 0x00, // height: 2 rows
                0b11110000, // Row 0: XXXX____ (half colored)
                0b00001111, // Row 1: ____XXXX (half colored)

                // Image 3: 8 wide, 1 tall (header: 4 bytes + 4 dim bytes + 1 data byte = 9 bytes)
                Gs, (byte)'v', 0x30, 0x00,
                0x01, 0x00, // width: 1 byte = 8 pixels
                0x01, 0x00, // height: 1 row
                0b01010101  // Row 0: _X_X_X_X (checkerboard)
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosRasterImageUploadGs7630(
                    Width: 8,
                    Height: 3,
                    Media: CreateExpectedRasterMedia(8, 3, [0b11111111, 0b10101010, 0b00000000]))
                { LengthInBytes = 11 },
                new EscPosCommands.EscPosRasterImageUploadGs7630(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0b11110000, 0b00001111]))
                { LengthInBytes = 10 },
                new EscPosCommands.EscPosRasterImageUploadGs7630(
                    Width: 8,
                    Height: 1,
                    Media: CreateExpectedRasterMedia(8, 1, [0b01010101]))
                { LengthInBytes = 9 }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImageGs7630(8, 3, Media.CreateDefaultPng(CreateExpectedRasterMedia(8, 3, [0b11111111, 0b10101010, 0b00000000]).Content.Length)) { LengthInBytes = 11 },
                new EscPosCommands.EscPosRasterImageGs7630(8, 2, Media.CreateDefaultPng(CreateExpectedRasterMedia(8, 2, [0b11110000, 0b00001111]).Content.Length)) { LengthInBytes = 10 },
                new EscPosCommands.EscPosRasterImageGs7630(8, 1, Media.CreateDefaultPng(CreateExpectedRasterMedia(8, 1, [0b01010101]).Content.Length)) { LengthInBytes = 9 }
            ],
            expectedCanvasElements:
            [
                [
                    // Image 1: placed at y=0
                    DebugElement("rasterImageGs7630", lengthInBytes: 11),
                    ViewImage(0, 0, 8, 3, Media.CreateDefaultPng(CreateExpectedRasterMedia(8, 3, [0b11111111, 0b10101010, 0b00000000]).Content.Length), lengthInBytes: 11),
                    // Image 2: placed at y=3 (below image 1 which has height=3)
                    DebugElement("rasterImageGs7630", lengthInBytes: 10),
                    ViewImage(0, 3, 8, 2, Media.CreateDefaultPng(CreateExpectedRasterMedia(8, 2, [0b11110000, 0b00001111]).Content.Length), lengthInBytes: 10),
                    // Image 3: placed at y=5 (below image 1+2 which have heights 3+2=5)
                    DebugElement("rasterImageGs7630", lengthInBytes: 9),
                    ViewImage(0, 5, 8, 1, Media.CreateDefaultPng(CreateExpectedRasterMedia(8, 1, [0b01010101]).Content.Length), lengthInBytes: 9)
                ]
           ]),

        // GS ( L: store raster graphics data, then print stored graphics data.
        new(
            id: 190006,
            input:
            [
                Gs, 0x28, 0x4C,
                0x0C, 0x00, // pL pH: 10-byte graphics header + 2 bytes of raster data
                0x30, 0x70, 0x30, // m=48, fn=112 (store raster graphics), a=48
                0x01, 0x01, // bx/by: normal horizontal and vertical scaling
                0x31, // c: single-color model
                0x08, 0x00, // width: 8 dots
                0x02, 0x00, // height: 2 dots
                0b11100000,
                0b00011000,
                Gs, 0x28, 0x4C,
                0x02, 0x00,
                0x30, 0x32 // m=48, fn=50 (print stored graphics)
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosRasterImageStoreGs284C(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0b11100000, 0b00011000]))
                { LengthInBytes = 17 },
                new EscPosCommands.EscPosRasterImagePrintUploadGs284C { LengthInBytes = 7 }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImageGs284C(8, 2, Media.CreateDefaultPng(96)) { LengthInBytes = 24 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImageGs284C", lengthInBytes: 24),
                    ViewImage(0, 0, 8, 2, Media.CreateDefaultPng(96), lengthInBytes: 24)
                ]
            ]),

        // GS 8 L: long-form store raster graphics data, then print with GS ( L.
        new(
            id: 190007,
            input:
            [
                Gs, 0x38, 0x4C,
                0x0C, 0x00, 0x00, 0x00, // p1..p4: 10-byte graphics header + 2 bytes of data
                0x30, 0x70, 0x30,
                0x01, 0x01,
                0x31,
                0x08, 0x00,
                0x02, 0x00,
                0b10101010,
                0b01010101,
                Gs, 0x28, 0x4C,
                0x02, 0x00,
                0x30, 0x32
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosRasterImageStoreGs384C(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0b10101010, 0b01010101]))
                { LengthInBytes = 19 },
                new EscPosCommands.EscPosRasterImagePrintUploadGs284C { LengthInBytes = 7 }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImageGs384C(8, 2, Media.CreateDefaultPng(93)) { LengthInBytes = 26 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImageGs384C", lengthInBytes: 26),
                    ViewImage(0, 0, 8, 2, Media.CreateDefaultPng(93), lengthInBytes: 26)
                ]
            ]),

        CreateOversizeRasterScenario(),
        CreateOversizeRasterWithBlackOverflowScenario()
    ];

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

    private static EscPosScenario CreateOversizeRasterScenario()
    {
        const int widthInDots = 576;
        const int heightInDots = 1;
        const int lengthInBytes = 80;
        var bitmap = new byte[72];
        var upload = CreateExpectedRasterMedia(widthInDots, heightInDots, bitmap);
        var media = Media.CreateDefaultPng(upload.Content.Length);

        // The byte-aligned width exceeds the printer, but all overflow pixels are transparent padding.
        // The parser must not emit a dimension error when no black dots cross the printable boundary.

        return new EscPosScenario(
            id: 210001,
            input:
            [
                Gs, (byte)'v', 0x30, 0x00,
                0x48, 0x00, // width: 72 bytes = 576 dots
                0x01, 0x00, // height: 1 row
                .. bitmap
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosRasterImageUploadGs7630(widthInDots, heightInDots, upload)
                {
                    LengthInBytes = lengthInBytes
                }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImageGs7630(widthInDots, heightInDots, media) { LengthInBytes = lengthInBytes }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImageGs7630", lengthInBytes: lengthInBytes),
                    ViewImage(0, 0, widthInDots, heightInDots, media, lengthInBytes)
                ]
            ]);
    }

    private static EscPosScenario CreateOversizeRasterWithBlackOverflowScenario()
    {
        const int widthInDots = 520;
        const int heightInDots = 1;
        const int lengthInBytes = 73;
        var bitmap = new byte[65];
        bitmap[64] = 0x80;
        var upload = CreateExpectedRasterMedia(widthInDots, heightInDots, bitmap);
        var media = Media.CreateDefaultPng(upload.Content.Length);
        var rightEdge = EscPosSpecs.DefaultCanvasWidth + 1;

        // The first bit in the byte after the 512-dot boundary is black, so this is real overflow.
        var dimensionError = new EscPosCommands.EscPosPrinterError(
            $"Image exceeds printer width: right edge at {rightEdge} px exceeds {EscPosSpecs.DefaultCanvasWidth} dots")
        {
            LengthInBytes = 0
        };

        return new EscPosScenario(
            id: 210002,
            input:
            [
                Gs, (byte)'v', 0x30, 0x00,
                0x41, 0x00, // width: 65 bytes = 520 dots
                0x01, 0x00, // height: 1 row
                .. bitmap
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosRasterImageUploadGs7630(widthInDots, heightInDots, upload)
                {
                    LengthInBytes = lengthInBytes
                }
            ],
            expectedPersistedCommands:
            [
                dimensionError,
                new EscPosCommands.EscPosRasterImageGs7630(widthInDots, heightInDots, media)
                {
                    LengthInBytes = lengthInBytes
                }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("printerError", lengthInBytes: 0, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = dimensionError.Message!
                    }),
                    DebugElement("rasterImageGs7630", lengthInBytes: lengthInBytes),
                    ViewImage(0, 0, widthInDots, heightInDots, media, lengthInBytes)
                ]
            ]);
    }

    public static TheoryData<EscPosScenario> FontStyleScenarios { get; } =
    [
        new(
            id: 220001,
            input:
            [
                Esc, 0x21, 0x00,
                Esc, 0x21, 0x01,
                Esc, 0x21, 0x20,
                Esc, 0x21, 0x31,
                Esc, 0x21, 0x02
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetPrintMode(0, false, false) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetPrintMode(1, false, false) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetPrintMode(0, true, false) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetPrintMode(1, true, true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetPrintMode(2, false, false) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setPrintMode", lengthInBytes: 3, parameters: SetFontParameters(0, false, false)),
                    DebugElement("setPrintMode", lengthInBytes: 3, parameters: SetFontParameters(1, false, false)),
                    DebugElement("setPrintMode", lengthInBytes: 3, parameters: SetFontParameters(0, true, false)),
                    DebugElement("setPrintMode", lengthInBytes: 3, parameters: SetFontParameters(1, true, true)),
                    DebugElement("setPrintMode", lengthInBytes: 3, parameters: SetFontParameters(2, false, false))
                ]
            ]),
        new(
            id: 220002,
            input:
            [
                Esc, (byte)'E', 0x01,
                Esc, (byte)'E', 0x00,
                Esc, (byte)'E', 0x01,
                Esc, (byte)'E', 0x00
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetBoldMode(true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetBoldMode(false) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetBoldMode(true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetBoldMode(false) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setBoldMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugElement("setBoldMode", lengthInBytes: 3, parameters: ToggleParameters(false)),
                    DebugElement("setBoldMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugElement("setBoldMode", lengthInBytes: 3, parameters: ToggleParameters(false))
                ]
            ]),
        new(
            id: 220003,
            input:
            [
                Esc, 0x2D, 0x01,
                Esc, 0x2D, 0x02,
                Esc, 0x2D, 0x00,
                Gs, 0x42, 0x01,
                Gs, 0x42, 0x00,
                Gs, 0x42, 0x01
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetUnderlineMode(true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetUnderlineMode(true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetUnderlineMode(false) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetReverseMode(true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetReverseMode(false) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetReverseMode(true) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setUnderlineMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugElement("setUnderlineMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugElement("setUnderlineMode", lengthInBytes: 3, parameters: ToggleParameters(false)),
                    DebugElement("setReverseMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugElement("setReverseMode", lengthInBytes: 3, parameters: ToggleParameters(false)),
                    DebugElement("setReverseMode", lengthInBytes: 3, parameters: ToggleParameters(true))
                ]
            ]),
        new(
            id: 220004,
            input:
            [
                Esc, 0x21, 0x00,
                (byte)'A', (byte)'A', Lf,
                Esc, 0x21, 0x31,
                Esc, (byte)'E', 0x01,
                Esc, 0x2D, 0x01,
                Gs, 0x42, 0x01,
                (byte)'B', (byte)'B', Lf
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetPrintMode(0, false, false) { LengthInBytes = 3 },
                CommandAppendText("AA"),
                CommandPrintAndLineFeed(),
                new EscPosCommands.EscPosSetPrintMode(1, true, true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetBoldMode(true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetUnderlineMode(true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetReverseMode(true) { LengthInBytes = 3 },
                CommandAppendText("BB"),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setPrintMode", lengthInBytes: 3, parameters: SetFontParameters(0, false, false)),
                    DebugAppendText("AA", lengthInBytes: 2),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        "AA",
                        x: 0,
                        y: 0,
                        lengthInBytes: 2,
                        charScaleX: 1,
                        charScaleY: 1,
                        fontName: EscPosSpecs.Fonts.FontA.FontName,
                        isBold: false,
                        isUnderline: false,
                        isReverse: false),
                    DebugElement("setPrintMode", lengthInBytes: 3, parameters: SetFontParameters(1, true, true)),
                    DebugElement("setBoldMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugElement("setUnderlineMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugElement("setReverseMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugAppendText("BB", lengthInBytes: 2),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        "BB",
                        x: 0,
                        y: EscPosSpecs.Fonts.FontA.HeightInDots + DefaultLineSpacing,
                        lengthInBytes: 2,
                        charScaleX: 2,
                        charScaleY: 2,
                        fontName: EscPosSpecs.Fonts.FontB.FontName,
                        isBold: true,
                        isUnderline: true,
                        isReverse: true)
                ]
            ]),
        new(
            id: 220005,
            input:
            [
                Esc, (byte)'E', 0x01,
                Esc, (byte)'F',
                Esc, (byte)'E', 0x01
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetBoldMode(true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosCancelBoldMode { LengthInBytes = 2 },
                new EscPosCommands.EscPosSetBoldMode(true) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setBoldMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugElement("cancelBoldMode", lengthInBytes: 2),
                    DebugElement("setBoldMode", lengthInBytes: 3, parameters: ToggleParameters(true))
                ]
            ]),
        new(
            id: 220006,
            input:
            [
                Esc, 0x34,
                (byte)'I', (byte)'T', Lf,
                Esc, 0x35,
                (byte)'N', (byte)'O', Lf
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosEnableItalicMode { LengthInBytes = 2 },
                CommandAppendText("IT"),
                CommandPrintAndLineFeed(),
                new EscPosCommands.EscPosDisableItalicMode { LengthInBytes = 2 },
                CommandAppendText("NO"),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("enableItalicMode", lengthInBytes: 2),
                    DebugAppendText("IT", lengthInBytes: 2),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        "IT",
                        x: 0,
                        y: 0,
                        lengthInBytes: 2,
                        fontName: EscPosSpecs.Fonts.FontA.FontName,
                        isItalic: true),
                    DebugElement("disableItalicMode", lengthInBytes: 2),
                    DebugAppendText("NO", lengthInBytes: 2),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        "NO",
                        x: 0,
                        y: EscPosSpecs.Fonts.FontA.HeightInDots + DefaultLineSpacing,
                        lengthInBytes: 2,
                        fontName: EscPosSpecs.Fonts.FontA.FontName)
                ]
            ]),
        new(
            id: 220007,
            input:
            [
                Gs, 0x21, 0x00,
                Gs, 0x21, 0x12,
                Gs, 0x21, 0x70
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetCharacterSize(1, 1) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetCharacterSize(2, 3) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetCharacterSize(8, 1) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setCharacterSize", lengthInBytes: 3, parameters: CharacterSizeParameters(1, 1)),
                    DebugElement("setCharacterSize", lengthInBytes: 3, parameters: CharacterSizeParameters(2, 3)),
                    DebugElement("setCharacterSize", lengthInBytes: 3, parameters: CharacterSizeParameters(8, 1))
                ]
            ]),
        new(
            id: 220008,
            input:
            [
                Gs, 0x21, 0x12,
                (byte)'A', Lf,
                Gs, 0x21, 0x00,
                (byte)'B', Lf
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetCharacterSize(2, 3) { LengthInBytes = 3 },
                CommandAppendText("A"),
                CommandPrintAndLineFeed(),
                new EscPosCommands.EscPosSetCharacterSize(1, 1) { LengthInBytes = 3 },
                CommandAppendText("B"),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setCharacterSize", lengthInBytes: 3, parameters: CharacterSizeParameters(2, 3)),
                    DebugAppendText("A", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        "A",
                        x: 0,
                        y: 0,
                        lengthInBytes: 1,
                        charScaleX: 2,
                        charScaleY: 3,
                        fontName: EscPosSpecs.Fonts.FontA.FontName),
                    DebugElement("setCharacterSize", lengthInBytes: 3, parameters: CharacterSizeParameters(1, 1)),
                    DebugAppendText("B", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        "B",
                        x: 0,
                        y: (EscPosSpecs.Fonts.FontA.HeightInDots * 3) + DefaultLineSpacing,
                        lengthInBytes: 1,
                        fontName: EscPosSpecs.Fonts.FontA.FontName)
                ]
            ]),
        new(
            id: 220009,
            input:
            [
                Esc, (byte)'G', 0x01,
                (byte)'D', (byte)'S', Lf,
                Esc, (byte)'G', 0x00
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetDoubleStrikeMode(true) { LengthInBytes = 3 },
                CommandAppendText("DS"),
                CommandPrintAndLineFeed(),
                new EscPosCommands.EscPosSetDoubleStrikeMode(false) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setDoubleStrikeMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugAppendText("DS", lengthInBytes: 2),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        "DS",
                        x: 0,
                        y: 0,
                        lengthInBytes: 2,
                        fontName: EscPosSpecs.Fonts.FontA.FontName,
                        isDoubleStrike: true),
                    DebugElement("setDoubleStrikeMode", lengthInBytes: 3, parameters: ToggleParameters(false))
                ]
            ]),
        new(
            id: 220010,
            input:
            [
                Esc, 0x20, 0x03,
                (byte)'A', (byte)'B', Lf
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetRightCharacterSpacing(3) { LengthInBytes = 3 },
                CommandAppendText("AB"),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setRightCharacterSpacing", lengthInBytes: 3, parameters: LineSpacingParameters(3)),
                    DebugAppendText("AB", lengthInBytes: 2),
                    DebugFlush(lengthInBytes: 1),
                    new CanvasTextElementDto(
                        "AB",
                        0,
                        0,
                        (2 * EscPosSpecs.Fonts.FontA.WidthInDots) + 3,
                        EscPosSpecs.Fonts.FontA.HeightInDots,
                        EscPosSpecs.Fonts.FontA.FontName,
                        3,
                        false,
                        false,
                        false)
                    {
                        LengthInBytes = 2
                    }
                ]
            ]),
        new(
            id: 220011,
            input:
            [
                Esc, 0x7B, 0x01,
                (byte)'U', (byte)'D', Lf,
                Esc, 0x7B, 0x00
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetUpsideDownMode(true) { LengthInBytes = 3 },
                CommandAppendText("UD"),
                CommandPrintAndLineFeed(),
                new EscPosCommands.EscPosSetUpsideDownMode(false) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setUpsideDownMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugAppendText("UD", lengthInBytes: 2),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        "UD",
                        x: 0,
                        y: 0,
                        lengthInBytes: 2,
                        fontName: EscPosSpecs.Fonts.FontA.FontName,
                        rotation: "180"),
                    DebugElement("setUpsideDownMode", lengthInBytes: 3, parameters: ToggleParameters(false))
                ]
            ])
    ];

    public static TheoryData<EscPosScenario> LineSpacingScenarios { get; } =
    [
        new(
            id: 230001,
            input: [Esc, 0x33, 0x40],
            expectedRequestCommands: [new EscPosCommands.EscPosSetLineSpacing(0x40) { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("setLineSpacing", lengthInBytes: 3, parameters: LineSpacingParameters(0x40))
                ]
            ]),
        new(
            id: 230002,
            input: [Esc, 0x32],
            expectedRequestCommands: [new EscPosCommands.EscPosResetLineSpacing() { LengthInBytes = 2 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("resetLineSpacing", lengthInBytes: 2)
                ]
            ])
    ];

    public static TheoryData<EscPosScenario> FeedScenarios { get; } =
    [
        new(
            id: 260005,
            input:
            [
                (byte)'A', Lf,
                Esc, 0x4A, 0x05,
                (byte)'B', Lf
            ],
            expectedRequestCommands:
            [
                CommandAppendText("A"),
                CommandPrintAndLineFeed(),
                new EscPosCommands.EscPosPrintAndFeedDots(5) { LengthInBytes = 3 },
                CommandAppendText("B"),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("A", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("A", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: 0, lengthInBytes: 1),
                    DebugElement("printAndFeedDots", lengthInBytes: 3, parameters: FeedDotsParameters(5)),
                    DebugAppendText("B", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        "B",
                        fontName: EscPosSpecs.Fonts.FontA.FontName,
                        x: 0,
                        y: EscPosSpecs.Fonts.FontA.HeightInDots + DefaultLineSpacing + 5,
                        lengthInBytes: 1)
                ]
            ])
    ];

    public static TheoryData<EscPosScenario> TabScenarios { get; } =
    [
        new(
            id: 270001,
            input:
            [
                (byte)'A', 0x09, (byte)'B', Lf
            ],
            expectedRequestCommands:
            [
                CommandAppendText("A"),
                new EscPosCommands.EscPosHorizontalTab { LengthInBytes = 1 },
                CommandAppendText("B"),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("A", lengthInBytes: 1),
                    DebugElement("horizontalTab", lengthInBytes: 1),
                    DebugAppendText("B", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("A", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: 0, lengthInBytes: 1),
                    TextElement("B", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 96, y: 0, lengthInBytes: 1)
                ]
            ]),
        new(
            id: 270002,
            input:
            [
                Esc, 0x44, 0x04, 0x08, 0x00,
                (byte)'A', 0x09, (byte)'B', Lf
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetHorizontalTabStops([4, 8]) { LengthInBytes = 5 },
                CommandAppendText("A"),
                new EscPosCommands.EscPosHorizontalTab { LengthInBytes = 1 },
                CommandAppendText("B"),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setHorizontalTabStops", lengthInBytes: 5, parameters: TabStopsParameters(4, 8)),
                    DebugAppendText("A", lengthInBytes: 1),
                    DebugElement("horizontalTab", lengthInBytes: 1),
                    DebugAppendText("B", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("A", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: 0, lengthInBytes: 1),
                    TextElement("B", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 48, y: 0, lengthInBytes: 1)
                ]
            ])
    ];

    // ESC M n (1B 4D n) — select character font (0=Font A, 1=Font B)
    public static TheoryData<EscPosScenario> SetFontScenarios { get; } =
    [
        // Font A (n=0)
        new(
            id: 250001,
            input: [Esc, 0x4D, 0x00],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetFont(0) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setFont", lengthInBytes: 3,
                        parameters: new Dictionary<string, string> { ["FontNumber"] = "0" })
                ]
            ]),
        // Font B (n=1)
        new(
            id: 250002,
            input: [Esc, 0x4D, 0x01],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetFont(1) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setFont", lengthInBytes: 3,
                        parameters: new Dictionary<string, string> { ["FontNumber"] = "1" })
                ]
            ]),
        // n=2 maps to Font A (only bit0 used)
        new(
            id: 250003,
            input: [Esc, 0x4D, 0x02],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetFont(0) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setFont", lengthInBytes: 3,
                        parameters: new Dictionary<string, string> { ["FontNumber"] = "0" })
                ]
            ]),
        // ESC M switches font; text renders with correct font name
        new(
            id: 250004,
            input:
            [
                Esc, 0x4D, 0x00,
                (byte)'A', Lf,
                Esc, 0x4D, 0x01,
                (byte)'B', Lf
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetFont(0) { LengthInBytes = 3 },
                CommandAppendText("A"),
                CommandPrintAndLineFeed(),
                new EscPosCommands.EscPosSetFont(1) { LengthInBytes = 3 },
                CommandAppendText("B"),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setFont", lengthInBytes: 3,
                        parameters: new Dictionary<string, string> { ["FontNumber"] = "0" }),
                    DebugAppendText("A", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("A", x: 0, y: 0, lengthInBytes: 1, fontName: EscPosSpecs.Fonts.FontA.FontName),
                    DebugElement("setFont", lengthInBytes: 3,
                        parameters: new Dictionary<string, string> { ["FontNumber"] = "1" }),
                    DebugAppendText("B", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("B",
                        x: 0,
                        y: EscPosSpecs.Fonts.FontA.HeightInDots + DefaultLineSpacing,
                        lengthInBytes: 1,
                        fontName: EscPosSpecs.Fonts.FontB.FontName)
                ]
            ])
    ];

    // ESC d n (1B 64 n) — print and feed n lines
    public static TheoryData<EscPosScenario> PrintAndFeedLinesScenarios { get; } =
    [
        // Feed 0 lines — no Y advance
        new(
            id: 260001,
            input: [Esc, 0x64, 0x00],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosPrintAndFeedLines(0) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("printAndFeedLines", lengthInBytes: 3,
                        parameters: new Dictionary<string, string> { ["Lines"] = "0" })
                ]
            ]),
        // Feed 1 line — advances Y by one line height
        new(
            id: 260002,
            input: [Esc, 0x64, 0x01],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosPrintAndFeedLines(1) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("printAndFeedLines", lengthInBytes: 3,
                        parameters: new Dictionary<string, string> { ["Lines"] = "1" })
                ]
            ]),
        // Feed 3 lines — text after feed starts at correct Y position
        new(
            id: 260003,
            input:
            [
                (byte)'A', Lf,
                Esc, 0x64, 0x03,
                (byte)'B', Lf
            ],
            expectedRequestCommands:
            [
                CommandAppendText("A"),
                CommandPrintAndLineFeed(),
                new EscPosCommands.EscPosPrintAndFeedLines(3) { LengthInBytes = 3 },
                CommandAppendText("B"),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("A", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("A", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: 0, lengthInBytes: 1),
                    DebugElement("printAndFeedLines", lengthInBytes: 3,
                        parameters: new Dictionary<string, string> { ["Lines"] = "3" }),
                    DebugAppendText("B", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    // LF advances by one line, then ESC d 3 feeds three more lines before "B" is flushed.
                    TextElement("B",
                        fontName: EscPosSpecs.Fonts.FontA.FontName,
                        x: 0,
                        y: 4 * (EscPosSpecs.Fonts.FontA.HeightInDots + DefaultLineSpacing),
                        lengthInBytes: 1)
                ]
            ]),
        // ESC d flushes pending text before feeding
        new(
            id: 260004,
            input:
            [
                (byte)'H', (byte)'i',
                Esc, 0x64, 0x02,
                (byte)'B', Lf
            ],
            expectedRequestCommands:
            [
                CommandAppendText("Hi"),
                new EscPosCommands.EscPosPrintAndFeedLines(2) { LengthInBytes = 3 },
                CommandAppendText("B"),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("Hi", lengthInBytes: 2),
                    DebugElement("printAndFeedLines", lengthInBytes: 3,
                        parameters: new Dictionary<string, string> { ["Lines"] = "2" }),
                    // The renderer records the feed command, then flushes the pending text buffer.
                    TextElement("Hi", fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: 0, lengthInBytes: 2),
                    DebugAppendText("B", lengthInBytes: 1),
                    DebugFlush(lengthInBytes: 1),
                    // "Hi" flushes one line, then ESC d 2 feeds two more lines before "B".
                    TextElement("B",
                        fontName: EscPosSpecs.Fonts.FontA.FontName,
                        x: 0,
                        y: 3 * (EscPosSpecs.Fonts.FontA.HeightInDots + DefaultLineSpacing),
                        lengthInBytes: 1)
                ]
            ])
    ];

    public static TheoryData<EscPosScenario> CodePageScenarios { get; }

    public static TheoryData<EscPosScenario> AllScenarios { get; }

    private static TheoryData<EscPosScenario> BuildAllScenarios()
    {
        var data = new TheoryData<EscPosScenario>();
        AddRange(data, BellScenarios);
        AddRange(data, TextScenarios);
        AddRange(data, ErrorScenarios);
        AddRange(data, PagecutScenarios);
        AddRange(data, PulseScenarios);
        AddRange(data, BarcodeScenarios);
        AddRange(data, RasterImageScenarios);
        AddRange(data, FontStyleScenarios);
        AddRange(data, LineSpacingScenarios);
        AddRange(data, FeedScenarios);
        AddRange(data, TabScenarios);
        AddRange(data, SetFontScenarios);
        AddRange(data, PrintAndFeedLinesScenarios);
        AddRange(data, CodePageScenarios);
        return data;
    }

    private static void AddRange(TheoryData<EscPosScenario> target, TheoryData<EscPosScenario> source)
    {
        foreach (var scenario in source)
        {
            target.Add(scenario);
        }
    }

    private static TheoryData<EscPosScenario> BuildCodePageScenarios(IReadOnlyList<CodePageVector> codePages)
    {
        var scenarios = new TheoryData<EscPosScenario>();
        var defaultEncoding = ResolveEncoding("437");

        foreach (var vector in codePages)
        {
            var input = new List<byte>();
            var expected = new List<Command>();
            var expectedView = new List<CanvasElementDto>();
            var currentY = 0;

            if (vector.Command.Length > 0)
            {
                input.AddRange(vector.Command);
                expected.Add(new EscPosCommands.EscPosSetCodePage(vector.CodePage) { LengthInBytes = vector.Command.Length });
                expectedView.Add(DebugElement(
                    "setCodePage",
                    lengthInBytes: vector.Command.Length,
                    parameters: CodePageParameters(vector.CodePage)));
            }

            void AppendText(string text)
            {
                var bytes = vector.Encoding.GetBytes(text);
                input.AddRange(bytes);
                input.Add(Lf);

                var normalized = vector.Encoding.GetString(bytes);
                expected.Add(new EscPosCommands.EscPosAppendText(bytes) { LengthInBytes = bytes.Length });
                expected.Add(new EscPosCommands.EscPosPrintAndLineFeed { LengthInBytes = 1 });

                expectedView.Add(DebugAppendText(normalized, lengthInBytes: bytes.Length));
                expectedView.Add(DebugFlush(lengthInBytes: 1));
                expectedView.Add(TextElement(normalized, fontName: EscPosSpecs.Fonts.FontA.FontName, x: 0, y: currentY, lengthInBytes: bytes.Length));
                // ESC/POS advances by font height plus the configured line spacing for each feed.
                currentY += EscPosSpecs.Fonts.FontA.HeightInDots + DefaultLineSpacing;
            }

            AppendText(vector.Uppercase);
            AppendText(vector.Lowercase);

            scenarios.Add(new EscPosScenario(id: 240001, input.ToArray(), expected, expectedCanvasElements: [expectedView.ToArray()]));
        }

        var suspiciousBytes = new byte[] { 0x87, 0xA0, 0xAA };
        var suspiciousText = defaultEncoding.GetString(suspiciousBytes);

        scenarios.Add(new EscPosScenario(
            id: 240101,
            input: [.. suspiciousBytes, Lf],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosParseError("ESCPOS_PARSER_ERROR", "Text contains non-ASCII bytes, but no code page was set.")
                {
                    LengthInBytes = 0
                },
                new EscPosCommands.EscPosAppendText(suspiciousBytes)
                {
                    LengthInBytes = suspiciousBytes.Length
                },
                new EscPosCommands.EscPosPrintAndLineFeed
                {
                    LengthInBytes = 1
                }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosParseError("ESCPOS_PARSER_ERROR", "Text contains non-ASCII bytes, but no code page was set.")
                {
                    LengthInBytes = 0
                },
                new EscPosCommands.EscPosAppendText(suspiciousBytes)
                {
                    LengthInBytes = suspiciousBytes.Length
                },
                new EscPosCommands.EscPosPrintAndLineFeed
                {
                    LengthInBytes = 1
                }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement(
                        "error",
                        lengthInBytes: 0,
                        parameters: new Dictionary<string, string>
                        {
                            ["Code"] = "ESCPOS_PARSER_ERROR",
                            ["Message"] = "Text contains non-ASCII bytes, but no code page was set."
                        }),
                    DebugAppendText(suspiciousText, lengthInBytes: suspiciousBytes.Length),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        suspiciousText,
                        fontName: EscPosSpecs.Fonts.FontA.FontName,
                        x: 0,
                        y: 0,
                        lengthInBytes: suspiciousBytes.Length)
                ]
            ]));

        scenarios.Add(new EscPosScenario(
            id: 240102,
            input:
            [
                Esc, (byte)'t', 0x11,
                Esc, (byte)'@',
                .. suspiciousBytes,
                Lf
            ],
            expectedRequestCommands:
            [
                new EscPosCommands.EscPosSetCodePage("866")
                {
                    LengthInBytes = 3
                },
                new EscPosCommands.EscPosInitialize
                {
                    LengthInBytes = 2
                },
                new EscPosCommands.EscPosParseError("ESCPOS_PARSER_ERROR", "Text contains non-ASCII bytes, but no code page was set.")
                {
                    LengthInBytes = 0
                },
                new EscPosCommands.EscPosAppendText(suspiciousBytes)
                {
                    LengthInBytes = suspiciousBytes.Length
                },
                new EscPosCommands.EscPosPrintAndLineFeed
                {
                    LengthInBytes = 1
                }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosSetCodePage("866")
                {
                    LengthInBytes = 3
                },
                new EscPosCommands.EscPosInitialize
                {
                    LengthInBytes = 2
                },
                new EscPosCommands.EscPosParseError("ESCPOS_PARSER_ERROR", "Text contains non-ASCII bytes, but no code page was set.")
                {
                    LengthInBytes = 0
                },
                new EscPosCommands.EscPosAppendText(suspiciousBytes)
                {
                    LengthInBytes = suspiciousBytes.Length
                },
                new EscPosCommands.EscPosPrintAndLineFeed
                {
                    LengthInBytes = 1
                }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement(
                        "setCodePage",
                        lengthInBytes: 3,
                        parameters: CodePageParameters("866")),
                    DebugElement("reset", lengthInBytes: 2),
                    DebugElement(
                        "error",
                        lengthInBytes: 0,
                        parameters: new Dictionary<string, string>
                        {
                            ["Code"] = "ESCPOS_PARSER_ERROR",
                            ["Message"] = "Text contains non-ASCII bytes, but no code page was set."
                        }),
                    DebugAppendText(suspiciousText, lengthInBytes: suspiciousBytes.Length),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        suspiciousText,
                        fontName: EscPosSpecs.Fonts.FontA.FontName,
                        x: 0,
                        y: 0,
                        lengthInBytes: suspiciousBytes.Length)
                ]
            ]));

        return scenarios;
    }

    private static IReadOnlyList<CodePageVector> BuildCodePageVectors()
    {
        return new List<CodePageVector>
        {
            CreateEsc("437", 0x00, LatinUpper, LatinLower),
            CreateEsc("720", 0x20, ArabicLetters, ArabicLetters),
            CreateEsc("737", 0x0E, GreekUpper, GreekLower),
            CreateEsc("775", 0x21, LatinUpper, LatinLower),
            CreateEsc("850", 0x02, LatinUpper, LatinLower),
            CreateEsc("852", 0x12, LatinUpper, LatinLower),
            CreateEsc("855", 0x22, CyrillicUpper, CyrillicLower),
            CreateEsc("857", 0x0D, TurkishUpper, TurkishLower),
            CreateEsc("858", 0x13, LatinUpper, LatinLower),
            CreateEsc("860", 0x03, LatinUpper, LatinLower),
            CreateEsc("861", 0x23, LatinUpper, LatinLower),
            CreateEsc("862", 0x24, HebrewLetters, HebrewLetters),
            CreateEsc("863", 0x04, LatinUpper, LatinLower),
            CreateEsc("864", 0x25, ArabicLetters, ArabicLetters),
            CreateEsc("865", 0x05, LatinUpper, LatinLower),
            CreateEsc("866", 0x11, CyrillicUpper, CyrillicLower),
            CreateEsc("869", 0x26, GreekUpper, GreekLower),
            CreateEsc("1250", 0x2D, LatinUpper, LatinLower),
            CreateEsc("1251", 0x2E, CyrillicUpper, CyrillicLower),
            CreateEsc("1252", 0x10, LatinUpper, LatinLower),
            CreateEsc("1253", 0x2F, GreekUpper, GreekLower),
            CreateEsc("1254", 0x30, TurkishUpper, TurkishLower),
            CreateEsc("1255", 0x31, HebrewLetters, HebrewLetters),
            CreateEsc("1256", 0x32, ArabicLetters, ArabicLetters),
            CreateEsc("1257", 0x33, LatinUpper, LatinLower),
            CreateEsc("1258", 0x34, LatinUpper, LatinLower)
        };
    }

    private static CodePageVector CreateEsc(string codePage, byte parameter, string uppercase, string lowercase)
    {
        var command = new[] { Esc, (byte)'t', parameter };
        return Create(codePage, command, uppercase, lowercase);
    }

    private static CodePageVector Create(string codePage, byte[] command, string uppercase, string lowercase)
    {
        try
        {
            var encoding = ResolveEncoding(codePage);
            return new CodePageVector(codePage, command, uppercase, lowercase, encoding);
        }
        catch (InvalidOperationException)
        {
            var fallback = Encoding.GetEncoding(437);
            return new CodePageVector(codePage, command, LatinUpper, LatinLower, fallback);
        }
    }

    private static Encoding ResolveEncoding(string codePage)
    {
        if (int.TryParse(codePage, out var numeric))
        {
            return Encoding.GetEncoding(numeric);
        }

        return Encoding.GetEncoding(codePage);
    }

    private static bool RegisterEncodingProvider()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return true;
    }

    private sealed record CodePageVector(
        string CodePage,
        byte[] Command,
        string Uppercase,
        string Lowercase,
        Encoding Encoding);

    // Default ESC/POS font metrics and spacing, aligned with renderer defaults.
    //private const int DefaultFontHeight = EscPosSpecs.Fonts.FontA.HeightInDots;
    private const int DefaultLineSpacing = EscPosSpecs.Rendering.DefaultLineSpacing;
    //private const int DefaultLineHeight = DefaultFontHeight + DefaultLineSpacing;

    private static CanvasDebugElementDto DebugAppendText(string text, int lengthInBytes)
    {
        return DebugElement(
            "appendToLineBuffer",
            lengthInBytes,
            new Dictionary<string, string>
            {
                ["Text"] = text
            });
    }

    private static CanvasDebugElementDto DebugDiscardedError()
    {
        return new CanvasDebugElementDto("printerError") { LengthInBytes = 0, };
    }

    private static CanvasTextElementDto TextElement(
        string text,
        string fontName,
        int x,
        int y,
        int lengthInBytes,
        int charScaleX = 1,
        int charScaleY = 1,
        bool isBold = false,
        bool isUnderline = false,
        bool isReverse = false,
        bool isItalic = false,
        bool isDoubleStrike = false,
        string rotation = "none")
    {
        var charWidth = fontName == EscPosSpecs.Fonts.FontA.FontName
            ? EscPosSpecs.Fonts.FontA.WidthInDots
            : EscPosSpecs.Fonts.FontB.WidthInDots;
        var charHeight = fontName == EscPosSpecs.Fonts.FontA.FontName
            ? EscPosSpecs.Fonts.FontA.HeightInDots
            : EscPosSpecs.Fonts.FontB.HeightInDots;
        var element = new CanvasTextElementDto(
            text,
            x,
            y,
            text.Length * charWidth * charScaleX,
            charHeight * charScaleY,
            fontName,
            0,
            isBold,
            isUnderline,
            isReverse,
            CharScaleX: charScaleX,
            CharScaleY: charScaleY,
            Rotation: rotation,
            IsItalic: isItalic,
            IsDoubleStrike: isDoubleStrike);

        return element with
        {
            LengthInBytes = lengthInBytes
        };
    }

    private static CanvasImageElementDto ViewImage(int x, int y, int width, int height, Media media, int lengthInBytes)
    {
        var element = new CanvasImageElementDto(
            new CanvasMediaDto(
                media.ContentType,
                ToMediaSize(media.Length),
                media.Url,
                media.FileName),
            x, y, width, height);

        return element with
        {
            LengthInBytes = lengthInBytes
        };
    }

    private static int ToMediaSize(long length)
    {
        return length > int.MaxValue ? int.MaxValue : (int)length;
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

    private static CanvasDebugElementDto DebugFlush(int lengthInBytes)
    {
        return DebugElement("flushLineBufferAndFeed", lengthInBytes);
    }

    private static IReadOnlyDictionary<string, string> PagecutParameters(EscPosCommands.EscPosPagecutMode mode, int? feedUnits)
    {
        return new Dictionary<string, string>
        {
            ["Mode"] = mode.ToString(),
            ["FeedMotionUnits"] = feedUnits?.ToString() ?? string.Empty
        };
    }

    private static IReadOnlyDictionary<string, string> SetFontParameters(
        int fontNumber,
        bool isDoubleWidth,
        bool isDoubleHeight)
    {
        return new Dictionary<string, string>
        {
            ["FontNumber"] = fontNumber.ToString(),
            ["IsDoubleWidth"] = isDoubleWidth.ToString(),
            ["IsDoubleHeight"] = isDoubleHeight.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> ToggleParameters(bool isEnabled)
    {
        return new Dictionary<string, string>
        {
            ["IsEnabled"] = isEnabled.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> LineSpacingParameters(int spacing)
    {
        return new Dictionary<string, string>
        {
            ["Spacing"] = spacing.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> CharacterSizeParameters(int widthMultiplier, int heightMultiplier)
    {
        return new Dictionary<string, string>
        {
            ["WidthMultiplier"] = widthMultiplier.ToString(),
            ["HeightMultiplier"] = heightMultiplier.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> FeedDotsParameters(int dots)
    {
        return new Dictionary<string, string>
        {
            ["Dots"] = dots.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> TabStopsParameters(params int[] columns)
    {
        return new Dictionary<string, string>
        {
            ["Columns"] = string.Join(",", columns)
        };
    }

    private static IReadOnlyDictionary<string, string> CodePageParameters(string codePage)
    {
        return new Dictionary<string, string>
        {
            ["CodePage"] = codePage
        };
    }

    private const string LatinUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LatinLower = "abcdefghijklmnopqrstuvwxyz";
    private const string GreekUpper = "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ";
    private const string GreekLower = "αβγδεζηθικλμνξοπρστυφχψω";
    private const string CyrillicUpper = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
    private const string CyrillicLower = "абвгдежзийклмнопрстуфхцчшщъыьэюя";
    private const string TurkishUpper = "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";
    private const string TurkishLower = "abcçdefgğhıijklmnoöprsştuüvyz";
    private const string HebrewLetters = "אבגדהוזחטיךכלםמןנסעףפץצקרשת";
    private const string ArabicLetters = "ابتثجحخدذرزسشصضطظعغفقكلمنهوي";

    private static EscPosCommands.EscPosAppendText CommandAppendText(string text, Encoding? encoding = null)
    {
        encoding ??= Encoding.GetEncoding(437);
        var bytes = encoding.GetBytes(text);
        return new EscPosCommands.EscPosAppendText(bytes) { LengthInBytes = bytes.Length };
    }

    private static EscPosCommands.EscPosPrintAndLineFeed CommandPrintAndLineFeed()
    {
        return new EscPosCommands.EscPosPrintAndLineFeed { LengthInBytes = 1 };
    }
}
