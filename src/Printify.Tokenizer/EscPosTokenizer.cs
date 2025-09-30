using Microsoft.Extensions.Options;
using Printify.Contracts.Config;
using Printify.Contracts.Service;

namespace Printify.Tokenizer;

// ESC/POS tokenizer stub: recognizes minimal text/newline and selected control ops.
public sealed class EscPosTokenizer : ITokenizer
{
    public const byte Esc = 0x1B;
    public const byte Gs = 0x1D;
    public const byte Lf = 0x0A;
    public const byte Bell = 0x07;

    private readonly IOptions<BufferOptions> bufferOptions;
    private readonly IClockFactory clockFactory;
    private readonly IBlobStorage blobStorage;

    public EscPosTokenizer(IOptions<BufferOptions> bufferOptions, IClockFactory clockFactory, IBlobStorage blobStorage)
    {
        this.bufferOptions = bufferOptions;
        this.clockFactory = clockFactory;
        this.blobStorage = blobStorage;
    }

    public string Protocol
    {
        get { return "escpos"; }
    }

    public ITokenizerSession CreateSession()
    {
        var clock = clockFactory.Create();
        return new EscPosTokenizerSession(bufferOptions, clock, blobStorage);
    }
}
