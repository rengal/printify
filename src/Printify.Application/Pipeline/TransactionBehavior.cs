using MediatR;
using Printify.Application.Interfaces;

namespace Printify.Application.Pipeline;

public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork uow) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not ITransactionalRequest)
            return await next(ct);

        await uow.BeginTransactionAsync(ct);
        try
        {
            var response = await next(ct);

            await uow.CommitAsync(ct);
            return response;
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
    }
}
