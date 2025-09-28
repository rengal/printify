namespace Printify.Listener.Tests;

using Printify.Listener;

public sealed class ListenerOptionsTests
{
    [Fact]
    public void Defaults_MatchSpecification()
    {
        var options = new ListenerOptions();

        Assert.Equal(9100, options.Port);
        Assert.Equal(30, options.IdleTimeoutSeconds);
        Assert.NotNull(options.SessionOptions);
    }
}
