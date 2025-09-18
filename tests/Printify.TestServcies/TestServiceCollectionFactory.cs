namespace Printify.TestServcies;

using Microsoft.Extensions.DependencyInjection;
using Printify.Contracts.Service;
using Printify.TestServcies.Storage;
using Printify.TestServcies.Timing;

public static class TestServiceCollectionFactory
{
    public static IServiceCollection Create()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBlobStorage, InMemoryBlobStorage>();
        services.AddSingleton<IRecordStorage, InMemoryRecordStorage>();
        services.AddSingleton<IClockFactory, TestClockFactory>();
        return services;
    }
}
