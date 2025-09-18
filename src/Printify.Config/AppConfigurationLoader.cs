using System;
using Microsoft.Extensions.Configuration;
using Printify.Contracts.Configuration;

namespace Printify.Config;

/// <summary>
/// Helper for loading <see cref="AppConfiguration"/> from files and environment variables.
/// </summary>
public static class AppConfigurationLoader
{
    private const string EnvironmentPrefix = "PRINTIFY_";

    public static AppConfiguration Load(string path, bool optional = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var configuration = BuildConfiguration(builder => builder.AddJsonFile(path, optional, reloadOnChange: false));
        return Bind(configuration);
    }

    public static AppConfiguration LoadFromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return Bind(configuration);
    }

    private static IConfigurationRoot BuildConfiguration(Action<IConfigurationBuilder> configure)
    {
        var builder = new ConfigurationBuilder();
        configure(builder);
        builder.AddEnvironmentVariables(EnvironmentPrefix);
        return builder.Build();
    }

    private static AppConfiguration Bind(IConfiguration configuration)
    {
        var configurationModel = new AppConfiguration();
        configuration.Bind(configurationModel);
        return configurationModel;
    }
}
