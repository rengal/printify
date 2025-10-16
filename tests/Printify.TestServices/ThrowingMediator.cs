using MediatR;
using Printify.Application.Interfaces;

namespace Printify.TestServices;

internal sealed class ThrowingMediator : IMediator
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

internal sealed class ThrowingJwtGenerator : IJwtTokenGenerator
{
    public string GenerateToken(Guid? userId, Guid? sessionId)
        => throw new InvalidOperationException("JWT generation must not be invoked in this scenario.");
}
