using System.Collections.Generic;
using MediatR;
using Printify.Domain.Users;

namespace Printify.Application.Features.Users.ListUsers;

public sealed record ListUsersQuery : IRequest<IReadOnlyList<User>>;
