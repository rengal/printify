using Printify.Domain.AnonymousSessions;
using Printify.Web.Contracts.Auth.AnonymousSession.Response;

namespace Printify.Web.Mapping;

internal static class AnonymousSessionMapper
{
    internal static AnonymousSessionDto ToDto(this AnonymousSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new AnonymousSessionDto(
            session.Id,
            session.CreatedAt);
    }
}
