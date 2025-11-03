namespace Printify.Application.Printing;

// public interface IPrintJob : IAsyncDisposable
// {
//     PrintJobId Id { get; }
//     Guid PrinterId { get; }
//     PrintJobSnapshot GetSnapshot();
//     Task StopAsync(CancellationToken cancellationToken);
// }
//
// public readonly record struct PrintJobId(Guid Value);
//
// public sealed record PrintJobSnapshot(
//     PrintJobId Id,
//     Guid PrinterId,
//     long BytesReceived,
//     bool IsBusy,
//     bool HasOverflow,
//     DateTimeOffset LastActivityUtc);
