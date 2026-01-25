using Printify.Domain.Layout.Primitives;

namespace Printify.Infrastructure.Mapping.Protocols.Epl;

/// <summary>
/// Converts between EPL protocol-specific rotation values (0-3) and protocol-agnostic Rotation enum.
/// EPL uses 0-3 for 90-degree increments.
/// </summary>
internal static class RotationMapper
{
    /// <summary>
    /// Converts EPL rotation value to protocol-agnostic Rotation enum.
    /// </summary>
    /// <param name="eplRotation">EPL rotation value (0-3)</param>
    /// <returns>Protocol-agnostic Rotation enum value</returns>
    public static Rotation ToDomainRotation(int eplRotation)
    {
        return eplRotation switch
        {
            0 => Rotation.None,
            1 => Rotation.Rotate90,
            2 => Rotation.Rotate180,
            3 => Rotation.Rotate270,
            _ => Rotation.None // Default to no rotation for invalid values
        };
    }

    /// <summary>
    /// Converts protocol-agnostic Rotation enum to EPL rotation value.
    /// </summary>
    /// <param name="rotation">Protocol-agnostic Rotation enum value</param>
    /// <returns>EPL rotation value (0-3)</returns>
    public static int ToEplRotation(Rotation rotation)
    {
        return rotation switch
        {
            Rotation.None => 0,
            Rotation.Rotate90 => 1,
            Rotation.Rotate180 => 2,
            Rotation.Rotate270 => 3,
            _ => 0 // Default to 0 for unknown values
        };
    }
}
