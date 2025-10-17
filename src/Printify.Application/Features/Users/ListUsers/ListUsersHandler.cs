using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Users;

namespace Printify.Application.Features.Users.ListUsers;

public sealed class ListUsersHandler(IUserRepository userRepository) : IRequestHandler<ListUsersQuery, IReadOnlyList<User>>
{
    public async Task<IReadOnlyList<User>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Repository delivers an ordered snapshot so callers get a deterministic list.
        var users = await userRepository.ListActiveAsync(cancellationToken).ConfigureAwait(false);

        return users;
    }
}
