using System;

namespace Printify.Contracts.Service;

/// <summary>
/// Stopwatch-like clock abstraction to measure elapsed intervals.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Starts or restarts interval measurement.
    /// </summary>
    void Start();

    /// <summary>
    /// Elapsed time in milliseconds since the last <see cref="Start"/>.
    /// </summary>
    long ElapsedMs { get; }
}
