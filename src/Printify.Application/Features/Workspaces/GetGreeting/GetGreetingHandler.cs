using Mediator.Net;
using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Features.Workspaces.GetWorkspaceSummary;
using Printify.Application.Services;

namespace Printify.Application.Features.Workspaces.GetGreeting;

public sealed class GetGreetingHandler(
    IMediator mediator,
    IGreetingService greetingService)
    : IRequestHandler<GetGreetingQuery, GreetingResponse>
{
    public async Task<GreetingResponse> Handle(IReceiveContext<GetGreetingQuery> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var workspaceId = request.Context.WorkspaceId;

        // For anonymous users or when workspace is not available, return greeting without context
        if (!workspaceId.HasValue)
        {
            return greetingService.GetGreeting(null);
        }

        // Use mediator to fetch workspace summary (reuses existing query/handler)
        var summaryQuery = new GetWorkspaceSummaryQuery(request.Context);
        var summary = await mediator.RequestAsync<GetWorkspaceSummaryQuery, WorkspaceSummary>(summaryQuery, cancellationToken)
            .ConfigureAwait(false);

        var greetingContext = new GreetingContext(
            WorkspaceCreatedAt: summary.CreatedAt,
            TotalPrinters: summary.TotalPrinters,
            TotalDocuments: summary.TotalDocuments,
            DocumentsLast24h: summary.DocumentsLast24h,
            LastDocumentAt: summary.LastDocumentAt
        );

        return greetingService.GetGreeting(greetingContext);
    }
}
