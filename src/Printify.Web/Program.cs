using Printify.BlobStorage.FileSystem;
using Printify.Contracts.Config;
using Printify.Contracts.Services;
using Printify.Core.Service;
using Printify.Listener;
using Printify.Tokenizer;
using BufferOptions = Printify.Contracts.Config.BufferOptions;
using ListenerOptions = Printify.Listener.ListenerOptions;

var builder = WebApplication.CreateBuilder(args);

// Optional: load external config.json (git-ignored) to simplify ops without env vars
builder.Configuration.AddJsonFile("config.json", optional: false, reloadOnChange: true);

builder.Services.Configure<ListenerOptions>(builder.Configuration.GetSection("Listener"));
builder.Services.Configure<Page>(builder.Configuration.GetSection("Page"));
builder.Services.Configure<Storage>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<BufferOptions>(builder.Configuration.GetSection("Buffer"));

builder.Services.AddSingleton<IBlobStorage, FileSystemBlobStorage>();
builder.Services.AddSingleton<IClockFactory, StopwatchClockFactory>();
builder.Services.AddSingleton<ITokenizer, EscPosTokenizer>();

// Register the concrete ListenerService as a singleton and expose it via IListenerService so the same
// instance can be resolved and controlled from the Web app.
builder.Services.AddSingleton<ListenerService>();
builder.Services.AddSingleton<IListenerService>(sp => sp.GetRequiredService<ListenerService>());

var app = builder.Build();

// Resolve and start the listener via DI (fire-and-forget); stop it on application stopping.
var listener = app.Services.GetRequiredService<IListenerService>();
_ = listener.StartAsync(CancellationToken.None);
app.Lifetime.ApplicationStopping.Register(() => listener.StopAsync(CancellationToken.None).GetAwaiter().GetResult());

app.MapGet("/health", () => Results.Ok("OK"));

app.Run();
