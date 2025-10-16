using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Printify.Application.Interfaces;
using Printify.Infrastructure.Config;
using Printify.TestServices;
using Printify.Web.Contracts.Auth.Requests;
using Printify.Web.Controllers;

namespace Printify.Web.Tests;

public sealed class AuthControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory = factory;

    [Fact]
    public async Task Login_WithNullRequest_ThrowsArgumentNullException()
    {
        await using var context = TestServiceContext.CreateForAuthControllerTest();
        var controller = context.Provider.GetRequiredService<AuthController>();

        // Null-forgiving is intentional to simulate the framework passing a null body into the action.
        await Assert.ThrowsAsync<ArgumentNullException>(() => controller.Login(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Login_WithWhitespaceDisplayName_ThrowsArgumentException()
    {
        await using var context = TestServiceContext.CreateForAuthControllerTest();
        var controller = context.Provider.GetRequiredService<AuthController>();
        var request = new LoginRequestDto("   ");

        await Assert.ThrowsAsync<ArgumentException>(() => controller.Login(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        using var client = CreateClientWithStubs();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithMalformedBearerToken_ReturnsUnauthorized()
    {
        using var client = CreateClientWithStubs();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "totally-invalid-token");

        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/auth/logout", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClientWithStubs()
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.RemoveAll<IPrinterRepository>();
                services.AddSingleton<IUserRepository, NullUserRepository>();
                services.AddSingleton<IPrinterRepository, NullPrinterRepository>();
                services.PostConfigure<RepositoryOptions>(options => options.ConnectionString = "Data Source=WebTests;Mode=Memory;Cache=Shared");
            });
        });

        return customizedFactory.CreateClient();
    }
}
