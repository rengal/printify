using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Users;

namespace Printify.Application.Features.Auth.GetCurrentUser;

public sealed class GetCurrentUserHandler(IUserRepository userRepository)
    : IRequestHandler<GetCurrentUserCommand, User>
{
    public async Task<User> Handle(GetCurrentUserCommand request, CancellationToken ct)
    {
        var userId = request.Context.UserId ?? throw new BadRequestException("UserId must not be empty");
        var user = await userRepository.GetByIdAsync(request.Context.UserId.Value, ct) ??
                   throw new AuthenticationFailedException("User not found");
        return user;
    }
}
