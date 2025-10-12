using Printify.Domain.Core;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Services;

namespace Printify.TestServices.Tokenizers;

using System;
using System.Collections.Generic;
using System.Threading;
using Printify.Domain.PrintJobs;

/// <summary>
/// Minimal tokenizer implementation for listener-focused tests.
/// It records created sessions and exposes synchronization primitives for assertions.
/// </summary>
public sealed class TestTokenizer : ITokenizer
{
    private readonly ManualResetEventSlim sessionCreated = new(false);

    /// <summary>
    /// The last session created by this tokenizer; useful to inspect buffer state in tests.
    /// </summary>
    public TestTokenizerSession? LastSession { get; private set; }

    /// <summary>
    /// Event signalled whenever <see cref="CreateSession"/> is invoked.
    /// </summary>
    public ManualResetEventSlim SessionCreated => sessionCreated;

    public string Protocol => "test";

    public ITokenizerSession CreateSession()
    {
        var session = new TestTokenizerSession();
        LastSession = session;
        sessionCreated.Set();
        return session;
    }
}

/// <summary>
/// Lightweight tokenizer session used by tests to capture bytes and completion semantics
/// without invoking protocol-specific logic.
/// </summary>
public sealed class TestTokenizerSession : ITokenizerSession
{
    private readonly List<byte> collectedBytes = new();

    public ManualResetEventSlim FeedReceived { get; } = new(false);

    public ManualResetEventSlim Completed { get; } = new(false);

    public IReadOnlyList<byte> ReceivedBytes => collectedBytes;

    public CompletionReason? LastCompletionReason { get; private set; }

    public int Sequence => 0;

    public long TotalConsumed => collectedBytes.Count;

    public IReadOnlyList<Element> Elements => [];

    public Document? Document => null;

    public bool IsCompleted => Completed.IsSet;

    public bool IsBufferBusy => false;

    public bool HasOverflow => false;

    public void Feed(ReadOnlySpan<byte> data)
    {
        collectedBytes.AddRange(data.ToArray());
        FeedReceived.Set();
    }

    public void Complete(CompletionReason reason)
    {
        LastCompletionReason = reason;
        Completed.Set();
    }
}
