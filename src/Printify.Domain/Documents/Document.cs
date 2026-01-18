using Mediator.Net.Contracts;
using Printify.Domain.Printing;
using Printify.Domain.Printers;

namespace Printify.Domain.Documents;

/// <summary>
/// Protocol-agnostic document with parsed commands and metadata.
/// </summary>
public sealed record Document(
    Guid Id,
    Guid PrintJobId,
    Guid PrinterId,
    DateTimeOffset Timestamp,
    Protocol Protocol,
    string? ClientAddress,
    int BytesReceived,
    int BytesSent,
    IReadOnlyList<Command> Commands,
    string[]? ErrorMessages) : IResponse;
