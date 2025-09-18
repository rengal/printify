using Printify.Contracts.Service;

namespace Printify.Tokenizer;

// ESC/POS tokenizer stub: recognizes minimal text/newline and selected control ops.
public sealed class EscPosTokenizer : ITokenizer
{
    public const byte Esc = 0x1B;
    public const byte Gs = 0x1D;
    public const byte Lf = 0x0A;
    public const byte Bell = 0x07;

    private readonly IClockFactory clockFactory;
    private readonly IBlobStorage blobStorage;

    public EscPosTokenizer(IClockFactory clockFactory, IBlobStorage blobStorage)
    {
        ArgumentNullException.ThrowIfNull(clockFactory);
        ArgumentNullException.ThrowIfNull(blobStorage);
        this.clockFactory = clockFactory;
        this.blobStorage = blobStorage;
    }

    public string Protocol
    {
        get { return "escpos"; }
    }

    public ITokenizerSession CreateSession(TokenizerSessionOptions? options = null, IClock? clock = null)
    {
        var resolvedClock = clock ?? clockFactory.Create();
        return new EscPosTokenizerSession(options ?? new TokenizerSessionOptions(), resolvedClock, blobStorage);
    }
}
