namespace Printify.Listener.Tests;

public sealed class ListenerOptionsTests
{
    [Fact]
    public void Defaults_MatchSpecification()
    {
        var context = TestServices.TestServiceContext.Create();

        Assert.NotNull(context);
        Assert.NotNull(context.ListenerOptions);
        Assert.True(context.ListenerOptions.Port > 0);
    }
}
