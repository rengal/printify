using System;
using MediatR;
using Printify.Domain.Users;

namespace Printify.Application.Features.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<User?>;
