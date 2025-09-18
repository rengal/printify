namespace Printify.Tokenizer.Tests.EscPos;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Printify.Contracts;
using Printify.Contracts.Elements;
using Printify.Contracts.Service;
using Printify.Tokenizer.Tests;
using Xunit;

public sealed class EscPosTokenizerGoldenTests
{
    private static readonly IReadOnlyDictionary<string, Element[]> Expectations = new Dictionary<string, Element[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["case01"] = new Element[]
        {
            new TextLine(1, "Hello")
        },
        ["case02"] = new Element[]
        {
            new PageCut(1)
        }
    };

    public static IEnumerable<object[]> Cases
    {
        get
        {
            var dataDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "EscPos"));
            if (!Directory.Exists(dataDirectory))
            {
                yield break;
            }

            foreach (var path in Directory.EnumerateFiles(dataDirectory, "case*.b64").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                yield return new object[]
                {
                    Path.GetFileNameWithoutExtension(path)!,
                    File.ReadAllText(path)
                };
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ParsesGoldenCases(string caseId, string base64)
    {
        using var context = EscPosTestHelper.CreateContext();
        var session = context.Tokenizer.CreateSession();

        var bytes = Convert.FromBase64String(base64);
        session.Feed(bytes);
        session.Complete(CompletionReason.DataTimeout);

        Assert.True(Expectations.TryGetValue(caseId, out var expectedElements));

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedSourceIp: null,
            expectedElements);
    }
}
