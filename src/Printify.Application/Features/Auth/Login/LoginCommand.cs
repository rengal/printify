using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Requests;
using Printify.Domain.Users;

namespace Printify.Application.Features.Auth.Login;

public record LoginCommand(
    RequestContext Context,
    string DisplayName) : IRequest<User>, ITransactionalRequest;
