using Printify.Contracts.Core;

namespace Printify.Contracts.Services;

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
