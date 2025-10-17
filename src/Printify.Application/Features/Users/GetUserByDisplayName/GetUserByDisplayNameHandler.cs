using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Users;

namespace Printify.Application.Features.Users.GetUserByDisplayName;

public sealed class GetUserByDisplayNameHandler(IUserRepository userRepository) : IRequestHandler<GetUserByDisplayNameQuery, User?>
{
    public async Task<User?> Handle(GetUserByDisplayNameQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Display names are treated case-sensitive; repository enforces non-deleted constraint.
        var user = await userRepository.GetByDisplayNameAsync(request.DisplayName, cancellationToken).ConfigureAwait(false);

        return user;
    }
}
