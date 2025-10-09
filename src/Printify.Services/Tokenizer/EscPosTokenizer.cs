using Microsoft.Extensions.Options;
using Printify.Domain.Config;
using Printify.Domain.Core;
using Printify.Domain.Services;

namespace Printify.Services.Tokenizer;

// ESC/POS tokenizer stub: recognizes minimal text/newline and selected control ops.
public sealed class EscPosTokenizer : ITokenizer
{
    public const byte Esc = 0x1B;
    public const byte Gs = 0x1D;
    public const byte Lf = 0x0A;
    public const byte Bell = 0x07;

    private readonly IOptions<BufferOptions> bufferOptions;
    private readonly IClockFactory clockFactory;

    public EscPosTokenizer(IOptions<BufferOptions> bufferOptions, IClockFactory clockFactory)
    {
        this.bufferOptions = bufferOptions;
        this.clockFactory = clockFactory;
    }

    public string Protocol => "escpos";

    public ITokenizerSession CreateSession()
    {
        var clock = clockFactory.Create();
        return new EscPosTokenizerSession(bufferOptions, clock);
    }
}
