using Printify.Domain.Layout.Primitives;

namespace Printify.Web.Mapping;

/// <summary>
/// Converts between protocol-agnostic Rotation enum and string representation for public API.
/// String values: "none", "90", "180", "270"
/// </summary>
internal static class RotationMapper
{
    /// <summary>
    /// Converts Rotation enum to string representation for API responses.
    /// </summary>
    public static string ToDto(Rotation rotation)
    {
        return rotation switch
        {
            Rotation.None => "none",
            Rotation.Rotate90 => "90",
            Rotation.Rotate180 => "180",
            Rotation.Rotate270 => "270",
            _ => "none"
        };
    }

    /// <summary>
    /// Parses string representation from API request to Rotation enum.
    /// </summary>
    public static Rotation ParseRotation(string? value)
    {
        return value switch
        {
            null => Rotation.None,
            "none" => Rotation.None,
            "90" => Rotation.Rotate90,
            "180" => Rotation.Rotate180,
            "270" => Rotation.Rotate270,
            "0" => Rotation.None,
            _ => Rotation.None
        };
    }
}
