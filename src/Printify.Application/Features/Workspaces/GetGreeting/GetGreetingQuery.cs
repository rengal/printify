using Mediator.Net.Contracts;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Workspaces.GetGreeting;

public sealed record GetGreetingQuery(RequestContext Context) : IRequest;
