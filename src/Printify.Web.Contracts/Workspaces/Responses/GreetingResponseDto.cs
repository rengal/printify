namespace Printify.Web.Contracts.Workspaces.Responses;

/// <summary>
/// Greeting message response with time-based options.
/// Client selects the appropriate greeting based on local time.
/// </summary>
/// <param name="Morning">Greeting for morning hours (06:00 - 11:00).</param>
/// <param name="Afternoon">Greeting for afternoon hours (12:30 - 15:30).</param>
/// <param name="Evening">Greeting for evening hours (18:30 - 22:00).</param>
/// <param name="General">General greeting for any time.</param>
public sealed record GreetingResponseDto(
    string? Morning,
    string? Afternoon,
    string? Evening,
    string General);
