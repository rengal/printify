using MediatR;
using Printify.Domain.Requests;
using Printify.Domain.Users;

namespace Printify.Application.Features.Auth.GetCurrentUser;

public record GetCurrentUserCommand(
    RequestContext Context)
    : IRequest<User>;
