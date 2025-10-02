using System;
using System.Collections.Generic;
using Printify.Contracts.Documents.Elements;

namespace Printify.Contracts.Documents;

/// <summary>
/// A parsed document produced by a tokenizer session.
/// </summary>
/// <param name="Id">Sequential identifier assigned by storage. Use 0 when creating a new document.</param>
/// <param name="PrinterId">Identifier of the printer associated with the document.</param>
/// <param name="Timestamp">Creation timestamp (UTC) when the document was finalized or captured.</param>
/// <param name="Protocol">Protocol the bytes were parsed with.</param>
/// <param name="SourceIp">Optional source IP address string for filtering/audit.</param>
/// <param name="Elements">Ordered list of document elements.</param>
public sealed record Document(
    long Id,
    long PrinterId,
    DateTimeOffset Timestamp,
    Protocol Protocol,
    string? SourceIp,
    IReadOnlyList<Element> Elements);
