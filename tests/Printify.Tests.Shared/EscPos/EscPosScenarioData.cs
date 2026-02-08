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
                    TextElement("ABC", x: 0, y: 0, lengthInBytes: 3)
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
                    TextElement("ABC", x: 0, y: 0, lengthInBytes: 3)
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
                    TextElement("ABC", x: 0, y: 0, lengthInBytes: 3),
                    DebugAppendText("DEF", lengthInBytes: 3),
                    DebugFlush(lengthInBytes: 1),
                    TextElement("DEF", x: 0, y: DefaultLineHeight, lengthInBytes: 3),
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
                    TextElement("ABC", x: 0, y: 0, lengthInBytes: 3),
                    TextElement("DEF", x: 36, y: 0, lengthInBytes: 3),
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
                    TextElement("DEF", x: 0, y: 0, lengthInBytes: 3)
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
                new EscPosCommands.EscPosRasterImageUpload(
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
                new EscPosCommands.EscPosRasterImage(8, 2, Media.CreateDefaultPng(85)) { LengthInBytes = 10 },
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
                    DebugElement("rasterImage", lengthInBytes: 10),
                    ViewImage(8, 2, Media.CreateDefaultPng(85), lengthInBytes: 10),
                    // "DEF" added to fresh buffer
                    DebugElement("appendToLineBuffer", parameters: new Dictionary<string, string> { ["Text"] = "DEF" }, lengthInBytes: 3),
                    // Flush - only "DEF" prints (positioned below the image)
                    DebugElement("flushLineBufferAndFeed", lengthInBytes: 1),
                    TextElement("DEF", x: 0, y: 2, lengthInBytes: 3)  // Y=2 because image (height=2) is above
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
            expectedRequestCommands: [new EscPosCommands.EscPosPrinterError("") { LengthInBytes = 1 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("printerError", lengthInBytes: 1, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = string.Empty
                    })
                ]
            ]),
        // Two consecutive null bytes produce one error (accumulated)
        new(
            id: 16002,
            input: [0x00, 0x00],
            expectedRequestCommands: [new EscPosCommands.EscPosPrinterError("") { LengthInBytes = 2 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("printerError", lengthInBytes: 2, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = string.Empty
                    })
                ]
            ]),
        // Multiple invalid bytes produce one error
        new(
            id: 16003,
            input: [0x00, 0x01, 0x02],
            expectedRequestCommands: [new EscPosCommands.EscPosPrinterError("") { LengthInBytes = 3 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("printerError", lengthInBytes: 3, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = string.Empty
                    })
                ]
            ]),
        // Invalid byte followed by text transitions correctly
        new(
            id: 160004,
            input: [0x00, .. "ABC"u8],
            expectedRequestCommands: [
                new EscPosCommands.EscPosPrinterError("") { LengthInBytes = 1 },
                CommandAppendText("ABC")],
            expectedCanvasElements:
            [
                [
                    DebugElement("printerError", lengthInBytes: 1, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = string.Empty
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
                new EscPosCommands.EscPosPrinterError("") { LengthInBytes = 1 },
                CommandAppendText("DEF")],
            expectedCanvasElements:
            [
                [
                    DebugAppendText("ABC", lengthInBytes: 3),
                    DebugElement("printerError", lengthInBytes: 1, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = string.Empty
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
                new EscPosCommands.EscPosPrinterError("") { LengthInBytes = 1 },
                new EscPosCommands.EscPosBell { LengthInBytes = 1 }],
            expectedCanvasElements:
            [
                [
                    DebugElement("printerError", lengthInBytes: 1, parameters: new Dictionary<string, string>
                    {
                        ["Message"] = string.Empty
                    }),
                    DebugElement("bell", lengthInBytes: 1)
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
                new EscPosCommands.EscPosRasterImageUpload(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0b11100000, 0b00011000]))
                { LengthInBytes = 10 }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImage(8, 2, Media.CreateDefaultPng(96)) { LengthInBytes = 10 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImage", lengthInBytes: 10),
                    ViewImage(8, 2, Media.CreateDefaultPng(96), lengthInBytes: 10)
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
                new EscPosCommands.EscPosRasterImageUpload(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0xFF, 0xFF]))
                { LengthInBytes = 10 }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImage(8, 2, Media.CreateDefaultPng(96)) { LengthInBytes = 10 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImage", lengthInBytes: 10),
                    ViewImage(8, 2, Media.CreateDefaultPng(96), lengthInBytes: 10)
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
                new EscPosCommands.EscPosRasterImageUpload(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0x00, 0x00]))
                { LengthInBytes = 10 }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImage(8, 2, Media.CreateDefaultPng(85)) { LengthInBytes = 10 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImage", lengthInBytes: 10),
                    ViewImage(8, 2, Media.CreateDefaultPng(85), lengthInBytes: 10)
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
                new EscPosCommands.EscPosRasterImageUpload(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0b10101010, 0b01010101]))
                { LengthInBytes = 10 }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImage(8, 2, Media.CreateDefaultPng(93)) { LengthInBytes = 10 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImage", lengthInBytes: 10),
                    ViewImage(8, 2, Media.CreateDefaultPng(93), lengthInBytes: 10)
                ]
            ]),
        CreateOversizeRasterScenario()
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
                new EscPosCommands.EscPosRasterImageUpload(widthInDots, heightInDots, upload) { LengthInBytes = lengthInBytes }
            ],
            expectedPersistedCommands:
            [
                new EscPosCommands.EscPosRasterImage(widthInDots, heightInDots, media) { LengthInBytes = lengthInBytes }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("rasterImage", lengthInBytes: lengthInBytes),
                    ViewImage(widthInDots, heightInDots, media, lengthInBytes)
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
                new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSelectFont(1, false, false) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSelectFont(0, true, false) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSelectFont(1, true, true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSelectFont(2, false, false) { LengthInBytes = 3 }
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setFont", lengthInBytes: 3, parameters: SetFontParameters(0, false, false)),
                    DebugElement("setFont", lengthInBytes: 3, parameters: SetFontParameters(1, false, false)),
                    DebugElement("setFont", lengthInBytes: 3, parameters: SetFontParameters(0, true, false)),
                    DebugElement("setFont", lengthInBytes: 3, parameters: SetFontParameters(1, true, true)),
                    DebugElement("setFont", lengthInBytes: 3, parameters: SetFontParameters(2, false, false))
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
                new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                CommandAppendText("AA"),
                CommandPrintAndLineFeed(),
                new EscPosCommands.EscPosSelectFont(1, true, true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetBoldMode(true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetUnderlineMode(true) { LengthInBytes = 3 },
                new EscPosCommands.EscPosSetReverseMode(true) { LengthInBytes = 3 },
                CommandAppendText("BB"),
                CommandPrintAndLineFeed()
            ],
            expectedCanvasElements:
            [
                [
                    DebugElement("setFont", lengthInBytes: 3, parameters: SetFontParameters(0, false, false)),
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
                    DebugElement("setFont", lengthInBytes: 3, parameters: SetFontParameters(1, true, true)),
                    DebugElement("setBoldMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugElement("setUnderlineMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugElement("setReverseMode", lengthInBytes: 3, parameters: ToggleParameters(true)),
                    DebugAppendText("BB", lengthInBytes: 2),
                    DebugFlush(lengthInBytes: 1),
                    TextElement(
                        "BB",
                        x: 0,
                        y: DefaultLineHeight,
                        lengthInBytes: 2,
                        charScaleX: 2,
                        charScaleY: 2,
                        fontName: EscPosSpecs.Fonts.FontB.FontName,
                        isBold: true,
                        isUnderline: true,
                        isReverse: true)
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
        AddRange(data, RasterImageScenarios);
        AddRange(data, FontStyleScenarios);
        AddRange(data, LineSpacingScenarios);
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
                expectedView.Add(TextElement(normalized, x: 0, y: currentY, lengthInBytes: bytes.Length));
                // ESC/POS advances by font height plus the configured line spacing for each feed.
                currentY += DefaultFontHeight + DefaultLineSpacing;
            }

            AppendText(vector.Uppercase);
            AppendText(vector.Lowercase);

            scenarios.Add(new EscPosScenario(id: 240001, input.ToArray(), expected, expectedCanvasElements: [expectedView.ToArray()]));
        }

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

    // Default ESC/POS font A metrics and spacing, aligned with renderer defaults.
    private const int DefaultFontWidth = EscPosSpecs.Fonts.FontA.WidthInDots;
    private const int DefaultFontHeight = EscPosSpecs.Fonts.FontA.HeightInDots;
    private const int DefaultLineSpacing = EscPosSpecs.Rendering.DefaultLineSpacing;
    private const int DefaultLineHeight = DefaultFontHeight + DefaultLineSpacing;

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
        int x,
        int y,
        int lengthInBytes,
        int charScaleX = 1,
        int charScaleY = 1,
        string fontName = EscPosSpecs.Fonts.FontA.FontName,
        bool isBold = false,
        bool isUnderline = false,
        bool isReverse = false)
    {
        var element = new CanvasTextElementDto(
            text,
            x,
            y,
            text.Length * DefaultFontWidth * charScaleX,
            DefaultFontHeight * charScaleY,
            fontName,
            0,
            isBold,
            isUnderline,
            isReverse,
            CharScaleX: charScaleX,
            CharScaleY: charScaleY);

        return element with
        {
            LengthInBytes = lengthInBytes
        };
    }

    private static CanvasImageElementDto ViewImage(int width, int height, Media media, int lengthInBytes)
    {
        var element = new CanvasImageElementDto(
            new CanvasMediaDto(
                media.ContentType,
                ToMediaSize(media.Length),
                media.Url,
                media.FileName),
            0,
            0,
            width,
            height);

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
