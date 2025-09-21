using System;

namespace Printify.Contracts.Service;

/// <summary>
/// Options controlling tokenizer session behavior such as simulated draining and buffer limits.
/// </summary>
public sealed record TokenizerSessionOptions(
    TimeSpan? IdleTimeout = null,
    int BusyThresholdBytes = 1,
    int? MaxBufferBytes = 128 * 1024,
    double? BytesPerSecond = 0d
);
