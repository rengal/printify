using System.Security.Claims;

namespace Printify.Application.Interfaces;

public interface ITokenValidator
{
    ClaimsPrincipal Validate(string token);
}
