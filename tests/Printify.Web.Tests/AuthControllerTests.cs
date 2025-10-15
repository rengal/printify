using System.Net;
using System.Net.Http.Headers;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Printify.Application.Interfaces;
using Printify.Infrastructure.Config;
using Printify.Web.Controllers;

namespace Printify.Web.Tests;

public sealed class AuthControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory = factory;

    [Fact]
    public async Task Login_WithNullRequest_ThrowsArgumentNullException()
    {
        var controller = CreateController();

        // Null-forgiving is intentional to simulate the framework passing a null body into the action.
        await Assert.ThrowsAsync<ArgumentNullException>(() => controller.Login(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Login_WithWhitespaceDisplayName_ThrowsArgumentException()
    {
        var controller = CreateController();
        var request = new Printify.Web.Contracts.Auth.Requests.LoginRequestDto("   ");

        await Assert.ThrowsAsync<ArgumentException>(() => controller.Login(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithMalformedBearerToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "totally-invalid-token");

        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/auth/logout", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static AuthController CreateController()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = new string('a', 32),
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiresInSeconds = 60
        });

        return new AuthController(jwtOptions, new ThrowingMediator(), new ThrowingJwtGenerator());
    }

    private sealed class ThrowingMediator : IMediator
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.FromException(new InvalidOperationException("Publish must not be invoked in this scenario."));

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.FromException(new InvalidOperationException("Publish must not be invoked in this scenario."));

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromException<TResponse>(new InvalidOperationException("Mediator must not be invoked in this scenario."));

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => Task.FromException(new InvalidOperationException("Mediator must not be invoked in this scenario."));

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromException<object?>(new InvalidOperationException("Mediator must not be invoked in this scenario."));

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => ThrowingStream<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => ThrowingStream<object?>();

        private static async IAsyncEnumerable<T> ThrowingStream<T>()
        {
            await Task.FromException(new InvalidOperationException("Mediator streaming must not be invoked in this scenario."));
            yield break;
        }
    }

    private sealed class ThrowingJwtGenerator : IJwtTokenGenerator
    {
        public string GenerateToken(Guid? userId, Guid? sessionId)
            => throw new InvalidOperationException("JWT generation must not be invoked in this scenario.");
    }
}
