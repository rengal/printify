using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Printify.Contracts.Services;
using Printify.Services.Listener;
using Printify.TestServices;

namespace Printify.Web.Tests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRecordStorage>();
            services.RemoveAll<IBlobStorage>();
            services.RemoveAll<IClockFactory>();
            services.RemoveAll<IListenerService>();
            services.RemoveAll<ListenerService>();

            services.AddSingleton<IRecordStorage, InMemoryRecordStorage>();
            services.AddSingleton<IBlobStorage, InMemoryBlobStorage>();
            services.AddSingleton<IClockFactory, TestClockFactory>();
            services.AddSingleton<IListenerService, NoopListenerService>();
        });
    }

    private sealed class NoopListenerService : IListenerService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
