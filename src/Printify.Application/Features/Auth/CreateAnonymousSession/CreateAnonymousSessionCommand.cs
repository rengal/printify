using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.AnonymousSessions;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Auth.CreateAnonymousSession;

public sealed record CreateAnonymousSessionCommand(
    RequestContext Context)
    : IRequest<AnonymousSession>, ITransactionalRequest;
