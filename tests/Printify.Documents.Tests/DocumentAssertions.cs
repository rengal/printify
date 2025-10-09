using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;

namespace Printify.Documents.Tests;

public static class DocumentAssertions
{
    public static void Equal(Document? actual, Document expected)
    {
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual!.Id);
        Assert.Equal(expected.PrinterId, actual.PrinterId);
        Assert.Equal(expected.Timestamp, actual.Timestamp);
        Assert.Equal(expected.Protocol, actual.Protocol);
        Assert.Equal(expected.SourceIp, actual.SourceIp);
        Assert.Equal(expected.Elements.Count, actual.Elements.Count);

        for (var index = 0; index < expected.Elements.Count; index++)
        {
            var expectedElement = expected.Elements[index];
            var actualElement = actual.Elements[index];

            if (expectedElement is RasterImageContent expectedRaster && actualElement is RasterImageContent actualRaster)
            {
                Assert.Equal(expectedRaster.Sequence, actualRaster.Sequence);
                Assert.Equal(expectedRaster.Width, actualRaster.Width);
                Assert.Equal(expectedRaster.Height, actualRaster.Height);
                Assert.Equal(expectedRaster.Media.Meta, actualRaster.Media.Meta);

                Assert.Equal(expectedRaster.Media.Content.HasValue, actualRaster.Media.Content.HasValue);
                if (expectedRaster.Media.Content.HasValue && actualRaster.Media.Content.HasValue)
                {
                    Assert.Equal(expectedRaster.Media.Content.Value.ToArray(), actualRaster.Media.Content.Value.ToArray());
                }

                continue;
            }

            Assert.Equal(expectedElement, actualElement);
        }
    }
}
