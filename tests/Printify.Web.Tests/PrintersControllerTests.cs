using System.Net;
using System.Net.Http.Json;
using Printify.Contracts.Resources;

namespace Printify.Web.Tests;

public sealed class PrintersControllerTests
{
    [Fact]
    public async Task Create_ReturnsNotImplemented()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new SavePrinterRequest(1, "Kitchen", "escpos", 384, null, "127.0.0.1");
        var response = await client.PostAsJsonAsync("/api/printers", request);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsNotImplemented()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/printers/1");

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }
}
