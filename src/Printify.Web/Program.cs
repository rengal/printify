using System.IO;
using Printify.BlobStorage.FileSystem;
using Printify.Contracts.Service;
using Printify.Core.Service;
using Printify.Tokenizer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FileSystemBlobStorageOptions>(options =>
{
    options.RootPath = builder.Configuration.GetValue<string>("BlobStorage:RootPath")
        ?? Path.Combine(builder.Environment.ContentRootPath, "blobs");
});

builder.Services.AddSingleton<IBlobStorage, FileSystemBlobStorage>();
builder.Services.AddSingleton<IClockFactory, StopwatchClockFactory>();
builder.Services.AddSingleton<ITokenizer, EscPosTokenizer>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("OK"));

app.Run();
