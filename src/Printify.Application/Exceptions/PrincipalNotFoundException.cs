namespace Printify.Application.Exceptions;

public sealed class PrincipalNotFoundException(Guid userId)
    : Exception($"Authenticated principal '{userId}' could not be resolved.");
