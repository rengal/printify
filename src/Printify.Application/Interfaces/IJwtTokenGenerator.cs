namespace Printify.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid? userId, Guid? sessionId);
}
