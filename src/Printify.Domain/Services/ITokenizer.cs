using Printify.Domain.Core;

namespace Printify.Domain.Services;

/// <summary>
/// Tokenizer service that converts protocol byte streams into document elements.
/// </summary>
public interface ITokenizer
{
    /// <summary>
    /// Identifier for the protocol handled by this tokenizer (e.g., "escpos").
    /// </summary>
    string Protocol { get; }

    /// <summary>
    /// Creates a stateful tokenizer session.
    /// </summary>
    ITokenizerSession CreateSession();
}
