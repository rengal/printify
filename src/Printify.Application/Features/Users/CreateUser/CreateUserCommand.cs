using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Requests;
using Printify.Domain.Users;

namespace Printify.Application.Features.Users.CreateUser;

public sealed record CreateUserCommand(
    RequestContext Context,
    Guid UserId,
    string DisplayName)
    : IRequest<User>, ITransactionalRequest;
