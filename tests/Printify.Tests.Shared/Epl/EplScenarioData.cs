using System.Text;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.Epl;
using Printify.Web.Contracts.Documents.Responses.View.Elements;
using Xunit;

namespace Printify.Tests.Shared.Epl;

/// <summary>
/// Provides reusable EPL parser scenarios for unit and integration tests.
/// </summary>
public static class EplScenarioData
{
    static EplScenarioData()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static TheoryData<EplScenario> ClearBufferScenarios { get; } =
    [
        new(
            id: 1001,
            input: "N\n"u8.ToArray(),
            expectedRequestElements: [new ClearBuffer { LengthInBytes = 2 }],
            expectedViewElements:
            [
                ViewDebug("clearBuffer", lengthInBytes: 2)
            ])
    ];

    public static TheoryData<EplScenario> LabelConfigScenarios { get; } =
    [
        new(
            id: 2001,
            input: "q500\n"u8.ToArray(),
            expectedRequestElements: [new SetLabelWidth(500) { LengthInBytes = 5 }],
            expectedViewElements:
            [
                ViewDebug("setLabelWidth", lengthInBytes: 5, parameters: new Dictionary<string, string> { ["Width"] = "500" })
            ]),
        new(
            id: 2002,
            input: "Q300,26\n"u8.ToArray(),
            expectedRequestElements: [new SetLabelHeight(300, 26) { LengthInBytes = 8 }],
            expectedViewElements:
            [
                ViewDebug("setLabelHeight", lengthInBytes: 8, parameters: new Dictionary<string, string> { ["Height"] = "300", ["SecondParameter"] = "26" })
            ]),
        new(
            id: 2003,
            input: "R3\n"u8.ToArray(),
            expectedRequestElements: [new SetPrintSpeed(3) { LengthInBytes = 3 }],
            expectedViewElements:
            [
                ViewDebug("setPrintSpeed", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Speed"] = "3" })
            ]),
        new(
            id: 2004,
            input: "S10\n"u8.ToArray(),
            expectedRequestElements: [new SetPrintDarkness(10) { LengthInBytes = 4 }],
            expectedViewElements:
            [
                ViewDebug("setPrintDarkness", lengthInBytes: 4, parameters: new Dictionary<string, string> { ["Darkness"] = "10" })
            ]),
        new(
            id: 2005,
            input: "ZT\n"u8.ToArray(),
            expectedRequestElements: [new SetPrintDirection('T') { LengthInBytes = 3 }],
            expectedViewElements:
            [
                ViewDebug("setPrintDirection", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Direction"] = "T" })
            ]),
        new(
            id: 2006,
            input: "ZB\n"u8.ToArray(),
            expectedRequestElements: [new SetPrintDirection('B') { LengthInBytes = 3 }],
            expectedViewElements:
            [
                ViewDebug("setPrintDirection", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Direction"] = "B" })
            ]),
        new(
            id: 2007,
            input: "I8\n"u8.ToArray(),
            expectedRequestElements: [new SetInternationalCharacter(8) { LengthInBytes = 3 }],
            expectedViewElements:
            [
                ViewDebug("setInternationalCharacter", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Code"] = "8" })
            ]),
        new(
            id: 2008,
            input: "i8,0\n"u8.ToArray(),
            expectedRequestElements: [new SetCodePage(8, 0) { LengthInBytes = 5 }],
            expectedViewElements:
            [
                ViewDebug("setCodePage", lengthInBytes: 5, parameters: new Dictionary<string, string> { ["Code"] = "8", ["Scaling"] = "0" })
            ])
    ];

    public static TheoryData<EplScenario> TextScenarios { get; } =
    [
        new(
            id: 3001,
            input: "A10,20,0,2,1,1,N,\"Hello\"\n"u8.ToArray(),
            expectedRequestElements: [new ScalableText(10, 20, 0, 2, 1, 1, 'N', "Hello") { LengthInBytes = 25 }],
            expectedViewElements:
            [
                ViewDebug("scalableText", lengthInBytes: 25, parameters: new Dictionary<string, string>
                {
                    ["X"] = "10",
                    ["Y"] = "20",
                    ["Rotation"] = "0",
                    ["Font"] = "2",
                    ["HorizontalMultiplication"] = "1",
                    ["VerticalMultiplication"] = "1",
                    ["Reverse"] = "N",
                    ["Text"] = "Hello"
                })
            ]),
        new(
            id: 3002,
            input: "A50,100,1,3,2,2,R,\"World\"\n"u8.ToArray(),
            expectedRequestElements: [new ScalableText(50, 100, 1, 3, 2, 2, 'R', "World") { LengthInBytes = 26 }],
            expectedViewElements:
            [
                ViewDebug("scalableText", lengthInBytes: 26, parameters: new Dictionary<string, string>
                {
                    ["X"] = "50",
                    ["Y"] = "100",
                    ["Rotation"] = "1",
                    ["Font"] = "3",
                    ["HorizontalMultiplication"] = "2",
                    ["VerticalMultiplication"] = "2",
                    ["Reverse"] = "R",
                    ["Text"] = "World"
                })
            ]),
        new(
            id: 3003,
            input: "A0,0,0,4,3,3,N,\"Test123\"\n"u8.ToArray(),
            expectedRequestElements: [new ScalableText(0, 0, 0, 4, 3, 3, 'N', "Test123") { LengthInBytes = 25 }],
            expectedViewElements:
            [
                ViewDebug("scalableText", lengthInBytes: 25, parameters: new Dictionary<string, string>
                {
                    ["X"] = "0",
                    ["Y"] = "0",
                    ["Rotation"] = "0",
                    ["Font"] = "4",
                    ["HorizontalMultiplication"] = "3",
                    ["VerticalMultiplication"] = "3",
                    ["Reverse"] = "N",
                    ["Text"] = "Test123"
                })
            ])
    ];

    public static TheoryData<EplScenario> BarcodeScenarios { get; } =
    [
        new(
            id: 4001,
            input: "B10,50,0,E30,2,100,B,\"123456789012\"\n"u8.ToArray(),
            expectedRequestElements: [new PrintBarcode(10, 50, 0, "E30", 2, 100, 'B', "123456789012") { LengthInBytes = 36 }],
            expectedViewElements:
            [
                ViewDebug("printBarcode", lengthInBytes: 36, parameters: new Dictionary<string, string>
                {
                    ["X"] = "10",
                    ["Y"] = "50",
                    ["Rotation"] = "0",
                    ["Type"] = "E30",
                    ["Width"] = "2",
                    ["Height"] = "100",
                    ["Hri"] = "B",
                    ["Data"] = "123456789012"
                })
            ]),
        new(
            id: 4002,
            input: "B20,80,1,2A,3,120,N,\"ABC123\"\n"u8.ToArray(),
            expectedRequestElements: [new PrintBarcode(20, 80, 1, "2A", 3, 120, 'N', "ABC123") { LengthInBytes = 29 }],
            expectedViewElements:
            [
                ViewDebug("printBarcode", lengthInBytes: 29, parameters: new Dictionary<string, string>
                {
                    ["X"] = "20",
                    ["Y"] = "80",
                    ["Rotation"] = "1",
                    ["Type"] = "2A",
                    ["Width"] = "3",
                    ["Height"] = "120",
                    ["Hri"] = "N",
                    ["Data"] = "ABC123"
                })
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
            expectedRequestElements: [new DrawHorizontalLine(10, 20, 2, 100) { LengthInBytes = 14 }],
            expectedViewElements:
            [
                ViewDebug("drawHorizontalLine", lengthInBytes: 14, parameters: new Dictionary<string, string>
                {
                    ["X"] = "10",
                    ["Y"] = "20",
                    ["Thickness"] = "2",
                    ["Length"] = "100"
                })
            ]),
        new(
            id: 6002,
            input: "X5,10,1,200,50\n"u8.ToArray(),
            expectedRequestElements: [new DrawLine(5, 10, 1, 200, 50) { LengthInBytes = 15 }],
            expectedViewElements:
            [
                ViewDebug("drawLine", lengthInBytes: 15, parameters: new Dictionary<string, string>
                {
                    ["X1"] = "5",
                    ["Y1"] = "10",
                    ["Thickness"] = "1",
                    ["X2"] = "200",
                    ["Y2"] = "50"
                })
            ])
    ];

    public static TheoryData<EplScenario> PrintScenarios { get; } =
    [
        new(
            id: 7001,
            input: "P1\n"u8.ToArray(),
            expectedRequestElements: [new Print(1) { LengthInBytes = 3 }],
            expectedViewElements:
            [
                ViewDebug("print", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Copies"] = "1" })
            ]),
        new(
            id: 7002,
            input: "P5\n"u8.ToArray(),
            expectedRequestElements: [new Print(5) { LengthInBytes = 3 }],
            expectedViewElements:
            [
                ViewDebug("print", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Copies"] = "5" })
            ])
    ];

    public static TheoryData<EplScenario> ErrorScenarios { get; } =
    [
        new(
            id: 8001,
            input: "\x00\x01\x02"u8.ToArray(),
            expectedRequestElements: [new PrinterError("") { LengthInBytes = 3 }],
            expectedViewElements:
            [
                ViewDebug("printerError", lengthInBytes: 3, parameters: new Dictionary<string, string> { ["Message"] = "" })
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

    private static ViewDebugElementDto ViewDebug(
        string name,
        int lengthInBytes,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        return new ViewDebugElementDto(name, parameters ?? new Dictionary<string, string>())
        {
            LengthInBytes = lengthInBytes
        };
    }
}
