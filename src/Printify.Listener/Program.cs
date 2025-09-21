using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Printify.Contracts.Service;
using Printify.Tokenizer;
using Printify.Listener;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        // Default configuration; can be overridden by appsettings.json / env.
        cfg.AddEnvironmentVariables(prefix: "PRINTIFY_");
    })
    .ConfigureServices((ctx, services) =>
    {
        // Bind listener options from configuration section "Listener".
        var listenerOptions = new ListenerOptions();
        ctx.Configuration.GetSection("Listener").Bind(listenerOptions);
        services.Configure<ListenerOptions>(ctx.Configuration.GetSection("Listener"));

        // Register tokenizer implementation (EscPos tokenizer).
        services.AddSingleton<ITokenizer, EscPosTokenizer>();

        // Register a real clock factory for production use.
        services.AddSingleton<IClockFactory, Printify.Core.Service.StopwatchClockFactory>();

        // Register the ListenerService as a single concrete instance and expose it
        // both as IHostedService (so Host will run it) and as IListenerService (for consumers/tests).
        services.AddSingleton<ListenerService>();
        services.AddSingleton<IListenerService>(sp => sp.GetRequiredService<ListenerService>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ListenerService>());

        // Note: do not call AddHostedService<ListenerService>() here because we've
        // already registered the concrete instance above and we want the same instance
        // to satisfy both hosted service and IListenerService.
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
