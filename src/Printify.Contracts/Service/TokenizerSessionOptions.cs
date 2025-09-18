using System;

namespace Printify.Contracts.Service;

/// <summary>
/// Options controlling tokenizer session behavior (timeouts and buffer simulation).
/// </summary>
public sealed record TokenizerSessionOptions(
    TimeSpan? IdleTimeout = null,
    int BusyThresholdBytes = 64 * 1024,
    int OverflowThresholdBytes = 128 * 1024,
    int SimulatedDrainBytesPerSecond = 32 * 1024
);

