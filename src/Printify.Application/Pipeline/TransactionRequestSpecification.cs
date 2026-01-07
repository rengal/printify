using System.Runtime.ExceptionServices;
using Mediator.Net;
using Mediator.Net.Context;
using Mediator.Net.Contracts;
using Mediator.Net.Pipeline;
using Printify.Application.Interfaces;

namespace Printify.Application.Pipeline;

public sealed class TransactionRequestSpecification(IDependencyScope dependencyScope)
    : IPipeSpecification<IReceiveContext<IRequest>>
{
    private readonly IDependencyScope dependencyScope = dependencyScope
        ?? throw new ArgumentNullException(nameof(dependencyScope));

    public bool ShouldExecute(IReceiveContext<IRequest> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Only wrap requests explicitly marked as transactional.
        return context.Message is ITransactionalRequest;
    }

    public async Task BeforeExecute(IReceiveContext<IRequest> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Message is not ITransactionalRequest)
        {
            return;
        }

        // Start a database transaction before the handler runs.
        var uow = dependencyScope.Resolve<IUnitOfWork>();
        await uow.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task Execute(IReceiveContext<IRequest> context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task AfterExecute(IReceiveContext<IRequest> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Message is not ITransactionalRequest)
        {
            return;
        }

        // Commit only after the handler completes successfully.
        var uow = dependencyScope.Resolve<IUnitOfWork>();
        await uow.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task OnException(Exception exception, IReceiveContext<IRequest> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Message is not ITransactionalRequest)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        // Cancellation token is unavailable here; rollback must not be skipped.
        var uow = dependencyScope.Resolve<IUnitOfWork>();
        await uow.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        ExceptionDispatchInfo.Capture(exception).Throw();
    }
}
