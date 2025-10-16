using System.IO;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Printify.Application.Interfaces;
using Printify.Domain.AnonymousSessions;
using Printify.Infrastructure.Config;
using Printify.Infrastructure.Persistence;

namespace Printify.Infrastructure.Tests;

public sealed class AnonymousSessionRepositoryTests : IAsyncDisposable
{
    private readonly List<string> databaseFiles = new();

    [Fact]
    public async Task AddAsync_PersistsAnonymousSession()
    {
        var connectionString = CreateConnectionString();
        await using var provider = BuildServiceProvider(connectionString);

        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAnonymousSessionRepository>();

        var session = AnonymousSession.Create("127.0.0.1");
        await repository.AddAsync(session, CancellationToken.None);

        var stored = await repository.GetByIdAsync(session.Id, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(session, stored);
    }

    [Fact]
    public async Task TouchAsync_UpdatesLastActiveAt()
    {
        var connectionString = CreateConnectionString();
        await using var provider = BuildServiceProvider(connectionString);

        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAnonymousSessionRepository>();

        var session = AnonymousSession.Create("10.0.0.1");
        await repository.AddAsync(session, CancellationToken.None);

        var newLastActive = session.LastActiveAt.AddMinutes(5);
        await repository.TouchAsync(session.Id, newLastActive, CancellationToken.None);

        var stored = await repository.GetByIdAsync(session.Id, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(newLastActive, stored!.LastActiveAt);
    }

    [Fact]
    public async Task AttachUserAsync_WithUnitOfWork_CommitsLink()
    {
        var connectionString = CreateConnectionString();
        await using var provider = BuildServiceProvider(connectionString);

        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAnonymousSessionRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var session = AnonymousSession.Create("172.16.0.1");
        await repository.AddAsync(session, CancellationToken.None);

        var userId = Guid.NewGuid();
        await unitOfWork.BeginTransactionAsync(CancellationToken.None);
        await repository.AttachUserAsync(session.Id, userId, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var stored = await repository.GetByIdAsync(session.Id, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(userId, stored!.LinkedUserId);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAllFields()
    {
        var connectionString = CreateConnectionString();
        await using var provider = BuildServiceProvider(connectionString);

        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAnonymousSessionRepository>();

        var session = AnonymousSession.Create("192.168.1.10");
        await repository.AddAsync(session, CancellationToken.None);

        var updatedSession = session with
        {
            CreatedAt = session.CreatedAt.AddMinutes(-30),
            LastActiveAt = session.LastActiveAt.AddMinutes(10),
            LinkedUserId = Guid.NewGuid()
        };

        await repository.UpdateAsync(updatedSession, CancellationToken.None);

        var stored = await repository.GetByIdAsync(session.Id, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(updatedSession, stored);
    }

    public ValueTask DisposeAsync()
    {
        foreach (var file in databaseFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Tests should not fail because cleanup was unable to delete a temp file.
            }
        }

        return ValueTask.CompletedTask;
    }

    private ServiceProvider BuildServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
        services.Configure<RepositoryOptions>(options => options.ConnectionString = connectionString);
        services.AddScoped<SqliteConnectionManager>();
        services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();
        services.AddScoped<IAnonymousSessionRepository, AnonymousSessionRepository>();

        return services.BuildServiceProvider();
    }

    private string CreateConnectionString()
    {
        var fileName = Path.Combine(Path.GetTempPath(), $"printify-anon-{Guid.NewGuid():N}.db");
        databaseFiles.Add(fileName);
        return $"Data Source={fileName};Cache=Shared";
    }
}
