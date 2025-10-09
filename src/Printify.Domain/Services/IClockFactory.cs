using Printify.Domain.Core;

namespace Printify.Domain.Services;

/// <summary>
/// Factory responsible for creating clock instances for time-sensitive components.
/// </summary>
public interface IClockFactory
{
    /// <summary>
    /// Creates a new clock instance.
    /// </summary>
    IClock Create();
}
