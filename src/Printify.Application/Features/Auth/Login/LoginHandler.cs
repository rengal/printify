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
        var user = await userRepository.GetByDisplayNameAsync(request.DisplayName, ct);
        if (user == null)
            throw new LoginFailedException($"User with name '{request.DisplayName}' not found");

        await sessionRepository.AttachUserAsync(request.Context.AnonymousSessionId.Value, user.Id, ct);
        return user;
    }
}
