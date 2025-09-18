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
    /// Gets the finalized document once the session completes; returns null while the session is active.
    /// </summary>
    Document? Document { get; }
    bool IsCompleted { get; }

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

