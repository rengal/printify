using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Users;

namespace Printify.Application.Features.Auth.Login;

public sealed class LoginHandler(IUserRepository userRepository, IAnonymousSessionRepository sessionRepository)
    : IRequestHandler<LoginCommand, User>
{
    public async Task<User> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
            throw new AuthenticationFailedException($"User with id '{request.UserId}' not found");

        if (request.Context.AnonymousSessionId.HasValue)
            await sessionRepository.AttachUserAsync(request.Context.AnonymousSessionId.Value, user.Id, ct);
        return user;
    }
}
