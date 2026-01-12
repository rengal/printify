namespace Printify.Tests.Shared.Epl;

public sealed record EplChunkStrategy(string Name, int[] ChunkPattern, int[] DelayPattern);

public readonly record struct EplChunkStep(ReadOnlyMemory<byte> Buffer, int DelayAfterMilliseconds);

public static class EplChunkStrategies
{
    private static readonly EplChunkStrategy[] DefaultStrategies =
    [
        new("SingleChunk", [int.MaxValue], []),
        new("SingleByte", [1], [60]),
        new("Increasing1234", [1, 2, 3, 4], [40, 70, 100, 130]),
        new("Decreasing4321", [4, 3, 2, 1], [120, 90, 60, 30]),
        new("Alternating12", [1, 2], [65, 95]),
        new("Alternating21", [2, 1], [85, 55]),
        new("Triplet3", [3], [110]),
        new("Mixed231", [2, 3, 1], [75, 115, 55]),
        new("Mixed3211", [3, 2, 1, 1], [90, 70, 50, 50]),
        new("LargeThenSmall", [5, 1], [100, 40]),
        new("SmallThenLarge", [1, 5], [45, 105]),
        new("PrimePattern", [2, 3, 5], [70, 100, 130])
    ];

    public static IReadOnlyList<EplChunkStrategy> All => DefaultStrategies;

    public static EplChunkStrategy SingleByte => DefaultStrategies[1];
}

public static class EplScenarioChunker
{
    public static IEnumerable<EplChunkStep> EnumerateChunks(byte[] payload, EplChunkStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(strategy);

        if (payload.Length == 0)
        {
            yield break;
        }

        var chunkPattern = strategy.ChunkPattern;
        var delayPattern = strategy.DelayPattern;

        var offset = 0;
        var iteration = 0;

        while (offset < payload.Length)
        {
            var desiredChunk = chunkPattern.Length == 0
                ? payload.Length
                : chunkPattern[iteration % chunkPattern.Length];
            var chunkSize = Math.Max(1, Math.Min(desiredChunk, payload.Length - offset));

            var delay = delayPattern.Length == 0
                ? 0
                : delayPattern[iteration % delayPattern.Length];

            yield return new EplChunkStep(payload.AsMemory(offset, chunkSize), delay);

            offset += chunkSize;
            iteration++;
        }
    }
}
