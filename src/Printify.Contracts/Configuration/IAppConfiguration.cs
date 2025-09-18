namespace Printify.Contracts.Configuration;

/// <summary>
/// Root application configuration exposed to components via dependency injection.
/// </summary>
public interface IAppConfiguration
{
    ListenerConfiguration Listener { get; }

    PageConfiguration Page { get; }

    StorageConfiguration Storage { get; }
}
