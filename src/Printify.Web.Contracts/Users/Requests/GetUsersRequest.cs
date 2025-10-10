namespace Printify.Web.Contracts.Users.Requests;

/// <summary>
/// Cursor-based query parameters for listing users via the web API.
/// </summary>
/// <param name="Limit">Maximum number of users to return.</param>
/// <param name="BeforeId">Exclusive upper bound for the identifier cursor.</param>
public sealed record GetUsersRequest(
    int Limit,
    long? BeforeId);
