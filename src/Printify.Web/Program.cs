using Printify.BlobStorage.FileSystem;
using Printify.Contracts.Config;
using Printify.Contracts.Services;
using Printify.Core.Service;
using Printify.Application.Documents.Commands;
using Printify.Application.Documents.Queries;
using Printify.Contracts.Documents.Services;
using Printify.Listener;
using Printify.RecordStorage;
using Printify.Tokenizer;
using BufferOptions = Printify.Contracts.Config.BufferOptions;
using ListenerOptions = Printify.Listener.ListenerOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("config.json", optional: false, reloadOnChange: true);

builder.Services.Configure<ListenerOptions>(builder.Configuration.GetSection("Listener"));
builder.Services.Configure<Page>(builder.Configuration.GetSection("Page"));
builder.Services.Configure<Storage>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<BufferOptions>(builder.Configuration.GetSection("Buffer"));

builder.Services.AddSingleton<IBlobStorage, FileSystemBlobStorage>();
builder.Services.AddSingleton<IRecordStorage, InMemoryRecordStorage>();
builder.Services.AddSingleton<IClockFactory, StopwatchClockFactory>();
builder.Services.AddSingleton<ITokenizer, EscPosTokenizer>();
builder.Services.AddSingleton<IDocumentCommandService, DocumentCommandService>();
// Query service materializes descriptors and optional raster content for the API surface.
builder.Services.AddSingleton<IDocumentQueryService, DocumentQueryService>();

builder.Services.AddSingleton<ListenerService>();
builder.Services.AddSingleton<IListenerService>(sp => sp.GetRequiredService<ListenerService>());

builder.Services.AddControllers();

var app = builder.Build();

var listener = app.Services.GetRequiredService<IListenerService>();
_ = listener.StartAsync(CancellationToken.None);
app.Lifetime.ApplicationStopping.Register(() => listener.StopAsync(CancellationToken.None).GetAwaiter().GetResult());

app.MapGet("/health", () => Results.Ok("OK"));
app.MapControllers();

app.Run();

public partial class Program;

