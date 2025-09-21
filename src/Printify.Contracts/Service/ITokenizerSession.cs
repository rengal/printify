using Printify.Contracts;
using Printify.Contracts.Elements;

namespace Printify.Contracts.Service;

/// <summary>
/// Stateful tokenizer session that accepts partial buffers and supports time-based behaviors.
/// </summary>
public interface ITokenizerSession
{
    int Sequence { get; }

    long TotalConsumed { get; }

    IReadOnlyList<Element> Elements { get; }

    /// <summary>
    /// Gets the finalized document once the session completes.
    /// Throws <see cref="InvalidOperationException"/> if accessed before completion.
    /// </summary>
    Document? Document { get; }

    bool IsCompleted { get; }

    /// <summary>
    /// Indicates whether the simulated printer buffer is currently busy processing printing commands.
    /// </summary>
    /// <remarks>
    /// Implementations track buffered bytes against the configured drain rate and update their state as
    /// the injected clock advances or other work executes, so successive reads can return different values
    /// even when no new data was fed.
    /// </remarks>
    bool IsBufferBusy { get; }

    /// <summary>
    /// Indicates whether the simulated printer buffer has overflowed during the session.
    /// </summary>
    /// <remarks>
    /// Once triggered, overflow status remains true for the lifetime of the session even if the buffer later drains.
    /// </remarks>
    bool HasOverflow { get; }

    /// <summary>
    /// Feed a chunk of bytes. The session may buffer incomplete tokens internally.
    /// </summary>
    void Feed(ReadOnlySpan<byte> data);

    /// <summary>
    /// Signals explicit completion. Implementations should flush remaining buffered content,
    /// finalize the session state, and mark it as completed.
    /// </summary>
    void Complete(CompletionReason reason);
}

