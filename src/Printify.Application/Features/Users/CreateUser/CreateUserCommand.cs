using MediatR;
using Printify.Domain.Requests;
using Printify.Domain.Users;

namespace Printify.Application.Features.Users.CreateUser;

public sealed record CreateUserCommand(RequestContext Context, string DisplayName) : IRequest<User>;
