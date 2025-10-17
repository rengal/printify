using MediatR;
using Printify.Domain.Users;

namespace Printify.Application.Features.Users.GetUserByDisplayName;

public sealed record GetUserByDisplayNameQuery(string DisplayName) : IRequest<User?>;
